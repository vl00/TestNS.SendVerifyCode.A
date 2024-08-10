using Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common.SimpleJobs;

public class JobContext
{
    internal readonly CancellationTokenSource Cts = new();
    internal readonly TaskCompletionSource<JobResult> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource PauseTcs;

    public readonly Dictionary<object, object> Items = new();

    public IJobLogger Logger { get; init; } = null;
    public Job Job { get; init; }
    public string Id => Job?.Id;

    public Task PauseAndWaitForResumeAsync()
    {
        Task t;
        lock (Cts)
        {
            if (PauseTcs == null || PauseTcs.Task.IsCompleted)
            {
                PauseTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                Logger?.LogDebug($"Pause jobid={Job.Id} !!");
            }
            t = PauseTcs.Task;
        }
        return t;
    }

    public void Pause() => _ = PauseAndWaitForResumeAsync();

    public Task GoOnOrWaitAsync() 
    {
        Task t = null;
        lock (Cts)
        {
            if (PauseTcs != null)
            {
                t = PauseTcs.Task;
                if (!t.IsCompleted) 
                    Logger?.LogDebug($"Paused and wait for resume jobid={Job.Id} !!");
            }
        }
        return t ?? Task.CompletedTask;
    }

    public void Resume()
    {
        lock (Cts)
        {
            if (PauseTcs?.TrySetResult() == true)
                Logger?.LogDebug($"Resume jobid={Job.Id} !!");
        }
    }

    internal bool InternalCancel(bool force = false)
    {
        lock (Cts)
        {
            var b = Tcs.TrySetCanceled();
            if (force || b)
            {
                Cts.Cancel();
                PauseTcs?.TrySetCanceled(Cts.Token);
            }
            return b;
        }
    }
}
