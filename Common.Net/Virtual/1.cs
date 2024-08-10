using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Net.Virtuals;

public interface IResolveHelper
{
    void ResolveMethodArgs(string clss, string method, ref int index, ref int blen, ref ReadOnlySequence<byte> buffer, out object data);
    Task<(bool, object)> CallMethod(IServiceProvider sp, string clss, string method, List<(int, object)> ps);
    IMemoryOwner<byte> ResolveNull(int blen);
    void WriteNull(PipeWriter writer);
    bool WriteArgs(PipeWriter writer, int size, object o, out int blen, List<Action> actions);
}

public interface IVirtualConnectionWrap : IDisposable
{
    VirtualConnection Value { get; }

    void Set(IServiceProvider sp, VirtualConnection value);
}

internal sealed class VirtualConnectionWrap : IVirtualConnectionWrap
{
    IServiceProvider _preSp;

    public VirtualConnection Value { get; private set; }

    public void Set(IServiceProvider sp, VirtualConnection value)
    {
        if (Value != null) return;
        Value = value;
        _preSp = Value._serviceProvider;
        Value._serviceProvider = sp;
    }

    public void Dispose() 
    {
        var v = Value;
        if (v == null) return;
        Value = null;
        v._serviceProvider = _preSp;
        _preSp = null;
    }
}

public interface IVirtualConnectionCollection
{
    int AllocBufferSize { get; }
    CancellationToken Closing { get; }
    IServiceProvider ServiceProvider { get; }

    Task WriteAsync(long frameId, Func<PipeWriter, CancellationToken, Task> doWrite, CancellationToken cancellationToken);
    Task WriteAsync(long frameId, Memory<byte> data, CancellationToken cancellationToken);
    Task WriteAsync(long frameId, string utf8String, CancellationToken cancellationToken);

    VirtualConnection GetOrAddVirtualConnection(long frameId, bool addIfNotExist);
    void RemoveVirtualConnection(VirtualConnection virtualConnection);
}
