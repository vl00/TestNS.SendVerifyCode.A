using Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TestNS;

public class JobWorking(WrapItems wrapItems, ProcessManager processManager)
{
    readonly object Sync = new();
    SendVcJob _job;
    string[] _svcs;
    int[] _res;
    int _c;

    public TaskCompletionSource<string> Tcs { get; private set; }

    public bool Do(SendVcJob job)
    {
        lock (Sync)
        {
            var b = _job == null;
            if (!b) return b;
            _job = job;
            _res = new int[_job.Svcs!.Length];
            _c = 0;
            _svcs = [.. _job.Svcs];
            Random.Shared.Shuffle(_svcs); // 随机排序数组
            Tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            TasksHolder.Add(() => OnDoing(), out _);
            return b;
        }
    }

    async Task OnDoing()
    {
        await Task.Yield();
        foreach (var s in _svcs)
        {
            var o = new QueuedItem(_job.Id, _job.PhoneNum, s);
            if (!wrapItems.Writer.TryWrite(o))
                await wrapItems.Writer.WriteAsync(o);
        } 
        
        processManager.Start(_job.Pc, _job.Tc);
    }

    public bool Complete(string jobId, string svc, int status,
        out int all, out int ok, out int fail)
    {
        ok = fail = 0;
        lock (Sync)
        {
            all = _res?.Length ?? 0;
            if (_job == null || _job.Id != jobId) return false;
            var i = Array.IndexOf(_svcs, svc);
            if (i == -1) return false;
            try
            {
                // 去重
                if (_res[i] != 0) return false;
                _res[i] = status;
                //
                // all is completed
                if (++_c < all) return false;
                Tcs.TrySetResult(_job.Id);
                _job = null;
                return true;
            }
            finally
            {
                ok = _res.Where(_ => _ == 1).Count();
                fail = _res.Where(_ => _ == 2).Count();
            }
        }
    }

    public async Task<object> GetResult(string jobId, int ms = 5000)
    {
        var t = Tcs?.Task;
        if (t == null) return null;
        var isCompleted = await t.DoOrTimeout(TimeSpan.FromMicroseconds(ms));
        lock (Sync)
        {
            return new
            {
                isCompleted, jobId, svcs = _svcs,
                all = _res.Length,
                alls = string.Join("", _res),
                ok = _res.Where(_ => _ == 1).Count(),
                fail = _res.Where(_ => _ == 2).Count(),
            };
        }
    }
}
