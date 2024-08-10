using Common.SimpleJobs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestNs.PuppeteerSharp.Jobs;

public abstract class BaseJobHandler2<T> : IJobHandler
    where T : Job
{
    protected JobContext JobContext { get; private set; }

    protected IDictionary<object, object> Items => JobContext!.Items;
    protected IJobLogger Log => JobContext!.Logger;

    Task<JobResult> IJobHandler.OnHandleAsync(JobContext ctx, CancellationToken cancellation)
    {
        JobContext = ctx;
        return OnHandleAsync((T)ctx.Job, cancellation);
    }

    protected abstract Task<JobResult> OnHandleAsync(T job, CancellationToken cancellation);
}
