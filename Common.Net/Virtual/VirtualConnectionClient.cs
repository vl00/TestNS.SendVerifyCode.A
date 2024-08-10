using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Net.Virtuals.Clients;

public class VirtualConnectionClient(IServiceProvider serviceProvider, IConnectionFactory connectionFactory, BytesPipelineOption option = null) : IDisposable
{
    ViConnClient _viClient;
    readonly SemaphoreSlim _slim = new(1);
    long _id;

    public async Task<VirtualConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var conn = _viClient?.Connection;
        if (conn?.IsOpen != true)
        {
            await _slim.WaitAsync(cancellationToken);
            try
            {
                conn = _viClient?.Connection;
                if (conn?.IsOpen != true)
                {
                    _viClient?.Dispose();

                    conn = connectionFactory.Create();
                    await conn.OpenAsync(cancellationToken);

                    _viClient = new(conn, (option ?? serviceProvider.GetService<BytesPipelineOption>()), serviceProvider);
                    TasksHolder.Add(_viClient.RunAsync(conn.Closing).ContinueWith(static (t, vi) =>
                    {
                        var _vi = (ViConnClient)vi;
                        if (t.Exception?.InnerException is Exception ex)
                        {
                            var log = _vi.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(VirtualConnectionClient));
                            log?.LogError(ex, "vcs run error, id={id}", _vi.Connection.Id);
                        }
                        _vi.Dispose();
                    }, _viClient));
                }
            }
            finally
            {
                _slim.Release();
            }
        }
        var frameId = Interlocked.Increment(ref _id);
        return _viClient.GetOrAddVirtualConnection(frameId, addIfNotExist: true);
    }

    public void Dispose()
    {
        _viClient?.Dispose();
    }
}

internal class ViConnClient(IConnection connection, BytesPipelineOption option, IServiceProvider serviceProvider) 
    : VirtualConnectionCollection(connection, option, serviceProvider)
{
    protected override void OnReceivedCallMethod(long frameId, string @class, string method, List<(int, object)> ps)
    {
        var virtualConnection = base.GetOrAddVirtualConnection(frameId, addIfNotExist: false);
        virtualConnection.AddDataToQueue(new ReceivedData<CallMethodInfo>(new(@class, method, ps)));
    }

    protected override void OnReceivedData(long frameId, IMemoryOwner<byte> data, int blen)
    {
        var virtualConnection = base.GetOrAddVirtualConnection(frameId, addIfNotExist: false);
        virtualConnection.AddDataToQueue(data == null ? null : new ReceivedData<BytesData>(new(data, blen))); 
    }
}
