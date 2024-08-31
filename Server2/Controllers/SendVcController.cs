using Common.JsonNet.v2;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace TestNS.Controllers;

[Route("/api/[controller]")]
[ApiController]
public partial class SendVcController(IServiceProvider serviceProvider) 
    : ControllerBase
{
    readonly ILogger<SendVcController> _log = serviceProvider.GetService<ILogger<SendVcController>>();
    readonly JobWorking _jobWorking = serviceProvider.GetService<JobWorking>();
    readonly WrapItems _wrapItems = serviceProvider.GetService<WrapItems>();

    [HttpPost("all/jobs")]
    [ProducesResponseType(typeof(string[]), 200)]
    public async Task<Fn2Result> GetAllJobs()
    {
        var jarr = Globals.LoadFileJson("jobs.json", new JArray());
        await default(ValueTask);
        var jobs = jarr.OfType<JObject>().Select(jo => jo["name"]?.ToString()).Where(_ => _ != null);
        return Fn2Result.OK(new 
        {
            total = jobs.Count(), 
            items = jobs
        });
    }

    /// <summary>
    /// send
    /// </summary>
    /// <returns></returns>
    [HttpPut]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<Fn2Result> Index(SendVcJob job)
    {
        //job.Id = Guid.NewGuid().ToString("n");
        var b = _jobWorking.Do(job);
        await default(ValueTask);
        return Fn2Result.OK(new 
        {
            id = b ? job.Id : null,
			count = b ? (job.Svcs?.Length ?? 0) : default(int?),
        });
    }

    [HttpGet(@"result/{id:required}")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<Fn2Result> GetResult(string id)
    {
        var r = await _jobWorking.GetResult(id);
        return Fn2Result.OK(r);
    }

    [HttpGet("items")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<Fn2Result> GetItems()
    {
        var count = _wrapItems.Reader.Count;
        await default(ValueTask);
        return Fn2Result.OK(new
        {
            count
        });
    }

    [HttpDelete("killps")]
    [ProducesResponseType(typeof(int), 200)]
    public Fn2Result Killps([FromServices] ProcessManager processManager)
    {
        processManager.KillAll();
        return Fn2Result.OK(1);
    }
}
