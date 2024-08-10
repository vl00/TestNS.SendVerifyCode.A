using Common;
using Common.Net;
using Common.Net.Virtuals;
using Common.Net.Virtuals.Clients;
using Common.SimpleJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestNs.PuppeteerSharp;
using TestNs.PuppeteerSharp.Jobs;

/**
 * for test1

 */

namespace TestNS;

public sealed class Args
{
    public string Xid;
    public string Address;
    public int ThreadCount = 1;
    public int Ms = 5000;
    public int I = -1;
    public string ExeDir;
}

internal sealed class Program_test1(ILogger log, IHostApplicationLifetime lifetime, 
    BrowserService _browserService,
    IServiceProvider services)
{
    public static void OnPreInit(string[] args, ProgramCtx ctx)
    {
        ctx["RUN_CONTINUE"] = 1;
        Globals.Args = new();
        From(Globals.Args, (args as IEnumerable<string>).GetEnumerator());
    }

    public static void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddTransient<IConnectionFactory>(sp =>
        {
            return new Common.Net.Clients.NamedPipeConnectionProxyFactory(Args.Address);
        });

        services.AddTransient(sp => new BytesPipelineOption
        {
            Options1 = new(minimumSegmentSize: 16, pauseWriterThreshold: 1024, resumeWriterThreshold: 512, useSynchronizationContext: false),
            Options2 = new(minimumSegmentSize: 16, pauseWriterThreshold: 1024, resumeWriterThreshold: 512, useSynchronizationContext: false),
        });

        services.AddVirtualConnection();
        services.AddTransient<IResolveHelper, ResolveHelper>();

        services.AddSingleton<BrowserService>();

        services.AddHttpClient();
        {
            services.AddHttpClient(string.Empty, http =>
            {
                http.Timeout = TimeSpan.Parse("00:01:00");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var ch = new HttpClientHandler();
                ch.ServerCertificateCustomValidationCallback = delegate { return true; };
                return ch;
            });
        }
    }

    static Args Args => Globals.Args;
    string _class = "Aaa";

    public async Task OnRunAsync()
    {
        Globals.ServiceProvider = services;
        JobManager.Instance = new CustomJobManager(services);

        if (Args.I > -1 && !string.IsNullOrEmpty(Args.ExeDir))
        {
            var jsfiles = Directory.EnumerateFiles(Args.ExeDir, "*.js");
            foreach (var jsfile in jsfiles)
                File.Copy(jsfile, Path.Combine(Directory.GetCurrentDirectory(), Path.GetFileName(jsfile)), true);
        }
        await _browserService.InitBrowser();

        VirtualConnectionClient vic = null;
        if (Args.I > -1)
        {
            var client = services.GetService<IConnectionFactory>();
            vic = new VirtualConnectionClient(services, client);
            lifetime.ApplicationStopping.UnsafeRegister(_ => vic.Dispose(), null);
        }

        async Task RunLoop(CancellationToken cancellation)
        {
            while (!cancellation.IsCancellationRequested)
            {
                QueuedItem item = null;
                VirtualConnection vc = null;
                if (Args.I > -1)
                {
                    vc = await vic.OpenAsync(cancellation);
                    var mo = await vc.CallMethodAsync(_class, "Pull", Args.Ms);
                    if (mo.Length <= 0)
                    {
                        mo.Dispose();
                        log.LogInformation("pull and get no item, i={i}", Args.I);
                        continue;
                    }
                    var res = Encoding.UTF8.GetString(mo.GetSpan());
                    mo.Dispose();

                    item = Newtonsoft.Json.JsonConvert.DeserializeObject<QueuedItem>(res);
                }
                else
                {
                    Console.WriteLine("输入手机号:");
                    var pnum = Console.ReadLine();
                    if (string.IsNullOrEmpty(pnum?.Trim())) pnum = "13800138003";
                    log.LogInformation("pnum={pnum}", pnum);
                    Console.WriteLine("svc=");
                    var svc = Console.ReadLine();
                    item = new(Guid.NewGuid().ToString("n"), pnum, svc);
                }
                log.LogInformation("pull and get item: {i} {phoneNum} {svc} {jobId}", Args.I, item.PhoneNum, item.Svc, item.JobId);

                await DoJobSvc(vc, item, cancellation);
            }
        }

    
        if (Args.ThreadCount == 1) await RunLoop(lifetime.ApplicationStopping);
        else
        {
            for (var i = 0; i < Args.ThreadCount; i++)
            {
                //var _i = i;
                TasksHolder.Add(() => RunLoop(lifetime.ApplicationStopping), out _);
            }
        }

        await ApplicationStoppedToTask(lifetime);
    }

    async Task DoJobSvc(VirtualConnection vc, QueuedItem item, CancellationToken cancellation)
    {
        var jlog = new ConsoleJobLogger(log, vc);
        var job = new SendVeCodeToPhoneNumJob { GroupJobId = item.JobId, PhoneNum = item.PhoneNum, Svc = item.Svc };
        (bool? b, JobResult res) = (default, default);
        try
        {
            var cjId = JobManager.Instance.Start(job, jlog);
            var d = cancellation.UnsafeRegister(_ => JobManager.Instance.Cancel(cjId), null);
            (b, res) = await JobManager.Instance.TryGetResult(cjId, 30);
            while (b != true)
            {
                if (cancellation.IsCancellationRequested) break;
                log.LogDebug("cjId={cjId} not completed. groupId={groupId}", cjId, job.GroupJobId);
                (b, res) = await JobManager.Instance.TryGetResult(cjId, 30);
            }
            d.Dispose();
        }
        catch (Exception ex)
        {
            res = new() { Success = false, Msg = ex.Message };
            log.LogError(ex, "do job error, {phoneNum} {svc} {jobId}", item.PhoneNum, item.Svc, item.JobId);
        }
        if (cancellation.IsCancellationRequested) return;
        if (vc == null)
        {
            log.LogInformation($"Run svc={item.Svc} {(res.Success ? "ok." : $"fail.\n{res.Msg}")}");
            return;
        }
        await vc.WriteCallMethod(_class, "Report", item.JobId, new 
        {
            item.Svc,
            xid = Args.Xid,
            status = res?.Success == true ? 1 : 2,
            LogLevel = nameof(LogLevel.Information),
            StrMsg = $"Run svc={item.Svc} {(res.Success ? "ok." : $"fail.\n{res.Msg}")}",
        });
    }

    static Task ApplicationStoppedToTask(IHostApplicationLifetime lifetime)
    {
        var tcs = new TaskCompletionSource<bool>();
        lifetime.ApplicationStopped.UnsafeRegister(static o => ((TaskCompletionSource<bool>)o).TrySetResult(true), tcs);
        return tcs.Task;
    }

    static void From(Args args, IEnumerator<string> a)
    {
        using var __ = a;
        while (a.MoveNext())
        {
            switch (a.Current?.ToLower())
            {
                case "--address":
                    {
                        a.MoveNext();
                        args.Address = a.Current.Trim('\"');
                    }
                    break;
                case "--tc":
                    {
                        a.MoveNext();
                        args.ThreadCount = int.Parse(a.Current.Trim('\"'));
                    }
                    break;
                case "--ms":
                    {
                        a.MoveNext();
                        args.Ms = int.Parse(a.Current.Trim('\"'));
                    }
                    break;
                case "-i":
                case "--i":
                    {
                        a.MoveNext();
                        args.I = int.Parse(a.Current.Trim('\"'));
                    }
                    break;
            }
        }
        args.Xid = Environment.GetEnvironmentVariable("xid");
        args.ExeDir = Environment.GetEnvironmentVariable("exe-dir");
        if (string.IsNullOrEmpty(args.ExeDir)) args.ExeDir = Directory.GetCurrentDirectory();
    }
}

sealed class CustomJobManager(IServiceProvider serviceProvider) : JobManager
{
    public override IJobHandler ResolveJobHandler(Type type)
    {
        if (type == null) return null;
        return (ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type) as IJobHandler) ?? base.ResolveJobHandler(type);
    }
}

sealed class ConsoleJobLogger(ILogger log, VirtualConnection vc) : IJobLogger
{
    public void LogDebug(string msg, params object[] args)
    {
        log.LogDebug(msg, args);
    }

    public void LogError(string msg, params object[] args)
    {
        log.LogError(msg, args);
    }

    public void LogInfo(string msg, params object[] args)
    {
        log.LogInformation(msg, args);
    }

    public void LogWarn(string msg, params object[] args)
    {
        log.LogWarning(msg, args);
    }

    public void LogError(Exception ex, string msg, params object[] args)
    {
        log.LogError(ex, msg ?? "", args);
    }
}

class ResolveHelper : DefaultResolveHelper
{
    public override bool WriteArgs(PipeWriter writer, int size, object o, out int blen, List<Action> actions)
    {
        var b = base.WriteArgs(writer, size, o, out blen, actions);
        if (b) return b;


        // 空字符串的byte-len为2
        //
        var s = Newtonsoft.Json.JsonConvert.SerializeObject(o);
        blen = Encoding.UTF8.GetByteCount(s);
        actions.Add(() => PipelineUtils.WriteString(writer, size, s, Encoding.UTF8));
        return true;
    }
}
