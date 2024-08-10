using System.Threading;
using System.Threading.Tasks;

namespace Common.SimpleJobs;

public interface IJobHandler
{
    Task<JobResult> OnHandleAsync(JobContext ctx, CancellationToken cancellation);
}
