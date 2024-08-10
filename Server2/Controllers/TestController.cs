using Microsoft.AspNetCore.Mvc;

namespace TestNS.Controllers;

[Route("/api/[controller]")]
[ApiController]
public partial class TestController(ILogger<TestController> _log) 
    : ControllerBase
{
    /// <summary>
    /// test index
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(string), 200)]
    public async ValueTask<Fn2Result> Index()
    {
        await Task.Delay(100);
        return Fn2Result.OK("hello world");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpGet(nameof(Logs))]
    [ProducesResponseType(typeof(string), 200)]
    public async Task<Fn2Result> Logs()
    {
        _log.LogTrace("log trace");
        _log.LogDebug("log debug");
        _log.LogInformation("log info");
        _log.LogWarning("log warn");
        _log.LogError("log error");
        _log.LogCritical("log critical");
        await Task.Delay(100);

        

        await default(ValueTask);
        return Fn2Result.OK("see logs");
    }
}
