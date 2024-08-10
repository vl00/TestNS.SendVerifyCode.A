using Common;
using Common.SimpleJobs;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TestNS;

namespace TestNs.PuppeteerSharp.Jobs;

public partial class SendVeCodeToPhoneNumJobHandler
{
    (string Name, Func<Func<SendVeCodeToPhoneNumJob, JobContext, CancellationToken, Task>> Func) GetFunc(string svc)
    {
        var jarr = Globals.LoadFileJson<JArray>(Path.Combine(Globals.Args.ExeDir, "jobs.json"));
        if (jarr == null) return default;
        var jo = jarr.OfType<JObject>().FirstOrDefault(_ => _["name"]?.ToString() == svc) ?? throw new NullReferenceException($"not found with name='{svc}'");
        var type = jo["type"]?.ToString();
        var method = jo["method"]?.ToString();
        if (type == null || method == null) return default;
        var ty = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()).FirstOrDefault(_ => _.Name == type);
        if (ty == null) return default;
        var o = NewObj(ty);
        var f = Delegate.CreateDelegate(typeof(Func<SendVeCodeToPhoneNumJob, JobContext, CancellationToken, Task>), o, method, true) as Func<SendVeCodeToPhoneNumJob, JobContext, CancellationToken, Task>;
        if (f == null) return default;
        return (svc, () => f); 
    }

    internal static T NewObj<T>(params object[] args)
    {
        var obj = _serviceProvider.GetService<T>();
        if (obj is not null) return obj;
        obj = ActivatorUtilities.CreateInstance<T>(_serviceProvider, args);
        return obj;
    }

    internal static object NewObj(Type type, params object[] args)
    {
        var obj = _serviceProvider.GetService(type);
        if (obj is not null) return obj;
        obj = ActivatorUtilities.CreateInstance(_serviceProvider, type, args);
        return obj;
    }
}
