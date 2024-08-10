using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Common.SimpleJobs;

public class JobManager
{
    readonly ConcurrentDictionary<string, JobContext> _jobs = new();    

    public static JobManager Instance;

    public virtual IJobLogger Logger { get; }

    public virtual Type ResolveJobHandlerTypeByDefault(Type jobType)
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(_ => _.GetTypes()).FirstOrDefault(t => t.Name.EndsWith($"{jobType.Name}Handler"));
    }

    public virtual IJobHandler ResolveJobHandler(Type type)
    {
        return type == null ? null : (Activator.CreateInstance(type) as IJobHandler);
    }

    public T ResolveJobHandler<T>() where T : IJobHandler
    {
        return (T)ResolveJobHandler(typeof(T));
    }

    public bool Cancel(string id)
    {
        if (id == null) return false;
        if (!_jobs.TryGetValue(id, out var item)) return false;
        if (item.InternalCancel())
        {
            _jobs.TryRemove(new(id, item));
            return true;
        }
        return false;
    }

    public void Pause(string id) => GetJobContext(id)?.Pause();
    public void Resume(string id) => GetJobContext(id)?.Resume();

    public string Start(Job job, IJobLogger logger = null)
    {
        job.Id ??= Guid.NewGuid().ToString();

        var item = new JobContext { Job = job, Logger = logger ?? Logger };
        if (!_jobs.TryAdd(job.Id, item))
        {
            throw new Exception("[error] add job fail, exsits one");
        }

        item.Logger?.LogDebug($"start job {job.Id}");
        _ = Task.Run(() => ExecAsync(item));

        return job.Id;
    }

    public JobContext GetJobContext(string id)
    {
        if (id == null) return null;
        return _jobs.TryGetValue(id, out var ctx) ? ctx : null;
    }

    public async Task<(bool? IsCompleted, JobResult Result)> TryGetResult(string id, int timeoutSec = 5)
    {
        if (!_jobs.TryGetValue(id, out var item)) return default;
        var t = item.Tcs.Task;
        var isTimeout = !await t.DoOrTimeout(TimeSpan.FromSeconds(timeoutSec));
        if (isTimeout) return (false, null);
        if (_jobs.TryRemove(new(id, item))) item.InternalCancel(true);
        return (true, t.Result);
    }

    async Task ExecAsync(JobContext ctx)
    {
        var job = ctx.Job;
        var jobHandlerType = job.HandleType ?? ResolveJobHandlerTypeByDefault(job.GetType());
        var handler = ResolveJobHandler(jobHandlerType);
        if (handler == null)
        {
            throw new NullReferenceException($"Not found jobHandler for type='{job.GetType().Name}'.");
        }
        //
        try
        {
            var r = await handler.OnHandleAsync(ctx, ctx.Cts.Token);
            //r ??= new() { Success = true };
            ctx.Tcs.TrySetResult(r);
            ctx.Logger?.LogDebug($"exec job done: ({job.Id}) result is ({(r == null ? "null" : r.Success.ToString().ToLower())})");
        }
        catch (Exception ex)
        {
            ctx.Tcs.TrySetException(ex);
            if (ctx.Cts.IsCancellationRequested)
                ctx.Logger?.LogWarn($"exec job cancelled: ({job.Id})\n {ex.Message}.");
            else
                ctx.Logger?.LogError(ex, $"exec job fail: ({job.Id})\n {ex.Message}.");
        }
        finally
        {
            switch (handler)
            {
                case IAsyncDisposable ad:
                    await ad.DisposeAsync();
                    break;
                case IDisposable d:
                    d.Dispose();
                    break;
            }
        }
    }
}
