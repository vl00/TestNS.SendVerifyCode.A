namespace Common;


public sealed class FuncHostedLifecycleService(IServiceProvider serviceProvider)
    : IHostedLifecycleService
{
    readonly IServiceProvider _serviceProvider = serviceProvider; 
    readonly ILogger _log = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<FuncHostedLifecycleService>();

    public Func<IServiceProvider, CancellationToken, Task> OnApplicationStarting;
    public Func<IServiceProvider, CancellationToken, Task> OnApplicationStarted;
    public Func<IServiceProvider, CancellationToken, Task> OnApplicationStopping;
    public Func<IServiceProvider, CancellationToken, Task> OnApplicationStopped;

    public async Task StartingAsync(CancellationToken cancellationToken)
    {
        _log?.LogDebug("host starting ...");
        try { await (OnApplicationStarting?.Invoke(_serviceProvider, cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false); }
        finally { OnApplicationStarting = null; }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _log?.LogDebug("host start ...");
        return Task.CompletedTask;
    }

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        _log?.LogDebug("host started ...");
        try { await(OnApplicationStarted?.Invoke(_serviceProvider, cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false); }
        finally { OnApplicationStarted = null; }
    }

    public async Task StoppingAsync(CancellationToken cancellationToken)
    {
        _log?.LogDebug("host stopping ...");
        try { await (OnApplicationStopping?.Invoke(_serviceProvider, cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false); }
        finally { OnApplicationStopping = null; }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _log?.LogDebug("host stop ...");
        return Task.CompletedTask;
    }

    public async Task StoppedAsync(CancellationToken cancellationToken)
    {
        _log?.LogDebug("host stopped ...");
        try { await (OnApplicationStopped?.Invoke(_serviceProvider, cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false); }
        finally { OnApplicationStopped = null; }
    }
}
