using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Common.Net.Virtuals.Servers;

public class VirtualConnectionServer(IConnection connection, BytesPipelineOption option, IServiceProvider serviceProvider) 
    : VirtualConnectionCollection(connection, option, serviceProvider)
{
    protected override void OnReceivedCallMethod(long frameId, string @class, string method, List<(int, object)> ps)
    {
        var virtualConnection = base.GetOrAddVirtualConnection(frameId, addIfNotExist: true);
        virtualConnection.MakeIsMethodCallingOnServerSide(out var is1st);
        if (is1st)
        {
            TasksHolder.Add(async () =>
            {
                try { await base.CallMethod(null, virtualConnection, @class, method, ps); }
                catch (Exception ex)
                {
                    var log = base.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(this.GetType());
					if (!base.Connection.IsOpen) log?.LogWarning("error on callmethod, connection is closed. c={cid}, v={vid}", base.Connection.Id, virtualConnection.Id);
                    else log?.LogError(ex, "error on callmethod, c={cid}, v={vid}", base.Connection.Id, virtualConnection.Id);
                }
                finally
                {
                    virtualConnection.Dispose();
                }
            }, out _);
        }
        else
        {
            virtualConnection.AddDataToQueue(new ReceivedData<CallMethodInfo>(new(@class, method, ps)));
        }
    }

    protected override void OnReceivedData(long frameId, IMemoryOwner<byte> data, int blen)
    {
        var virtualConnection = base.GetOrAddVirtualConnection(frameId, addIfNotExist: false);
        virtualConnection.AddDataToQueue(data == null ? null : new ReceivedData<BytesData>(new(data, blen))); 
    }
}
