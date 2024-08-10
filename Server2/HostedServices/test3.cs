using Common.Net;
using Common.Net.Virtuals;
using Common.Net.Virtuals.Servers;

namespace TestNS.ns2;

internal sealed class Program_test3_server(ILogger<Program_test3_server> log, 
    IHostApplicationLifetime lifetime, 
    IConnectionListener connectionListener,
    IServiceProvider services)
{
    public void OnRun()
    {
        TasksHolder.Add(RunListener(lifetime.ApplicationStopping));
    }

    async Task RunListener(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                var connection = await connectionListener.WaitForConnectionAsync(cancellation);
                log.LogInformation("On phy conn id='{id}'.", connection.Id);

                var virtualConnectionServer = new VirtualConnectionServer(connection, services.GetService<BytesPipelineOption>(), services);
                TasksHolder.Add(async () =>
                {
                    try { await virtualConnectionServer.RunAsync(VirtualConnection.Merge(connection.Closing, cancellation)); }
                    catch when (cancellation.IsCancellationRequested)
                    {
                        log.LogWarning("vcs run cancel");
                    }
					catch (Exception ex) when (!connection.IsOpen) 
					{
						log.LogWarning("vcs run error, connection is closed. id={id}, errmsg={errmsg}", connection.Id, ex.Message);
					}
                    catch (Exception ex)
                    {
                        log.LogError(ex, "vcs run error, id={id}", connection.Id);
                    }
                    finally
                    {
                        log.LogDebug("Off phy conn id='{id}'.", connection.Id);

                        virtualConnectionServer.Dispose();
                        connection.Dispose();
                    }
                }, out _);
            }
            catch when (cancellation.IsCancellationRequested)
            {
                // ignore
            }
            catch (Exception ex)
            {
                log.LogError(ex, "error on WaitForConnection");
            }
        }
    }
}
