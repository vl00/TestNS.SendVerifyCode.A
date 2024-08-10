using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Common.Net.Virtuals;

public class VirtualConnection : IDisposable
{
    readonly Channel<IReceivedData> _receivedQueue; 
    IVirtualConnectionCollection _service;
    internal IServiceProvider _serviceProvider;
    bool _isMethodCallingOnServerSide;

    public readonly long Id;

    public int AllocBufferSize => _service!.AllocBufferSize;

    public IServiceProvider ServiceProvider => _serviceProvider ?? _service!.ServiceProvider!;

    internal VirtualConnection(long id, IVirtualConnectionCollection service)
    {
        Id = id;
        _service = service;

        _receivedQueue = Channel.CreateUnbounded<IReceivedData>(new()
        {
            SingleWriter = true,
            SingleReader = false,
            AllowSynchronousContinuations = true
        });
    }

    internal void MakeIsMethodCallingOnServerSide(out bool is1st)
    {
        if (_isMethodCallingOnServerSide) is1st = false;
        else is1st = _isMethodCallingOnServerSide = true;
    }

    public async Task<BytesData> ReadAsync(CancellationToken cancellationToken = default)
    {
        var o = await _receivedQueue.Reader.ReadAsync(Merge(_service.Closing, cancellationToken));
        return o is null ? default : o is ReceivedData<BytesData> mo ? mo.Value : throw new InvalidOperationException("");
    }

    public async Task<CallMethodInfo> ReadCallMethodInfoAsync(CancellationToken cancellationToken = default)
    {
        var o = await _receivedQueue.Reader.ReadAsync(Merge(_service.Closing, cancellationToken));
        return o is ReceivedData<CallMethodInfo> mo ? mo.Value : throw new InvalidOperationException("");
    }

    internal Task WriteAsync(Func<PipeWriter, CancellationToken, Task> doWrite, CancellationToken cancellationToken = default)
    {
        return _service.WriteAsync(Id, doWrite, Merge(_service.Closing, cancellationToken));
    }

    public ValueTask WriteAsync(Memory<byte> data, CancellationToken cancellationToken = default)
    {
        return new(_service.WriteAsync(Id, data, Merge(_service.Closing, cancellationToken)));
    }

    public ValueTask WriteAsync(string utf8String, CancellationToken cancellationToken = default)
    {
        return new(_service.WriteAsync(Id, utf8String, Merge(_service.Closing, cancellationToken)));
    }

    public void Dispose()
    {
        var f = _service;
        if (f == null) return;
        _service = null;
        _serviceProvider = null;

        f.RemoveVirtualConnection(this);

        _receivedQueue.Writer.TryComplete();

        while (_receivedQueue.Reader.TryRead(out var d))
        {
            switch (d)
            {
                case ReceivedData<BytesData> b:
                    b.Value.Dispose();
                    break;
                case IDisposable d2:
                    d2.Dispose();
                    break;
            }
        }
    }

    internal void AddDataToQueue(IReceivedData data)
    {
        _receivedQueue.Writer.TryWrite(data);
    }

    public static CancellationToken Merge(CancellationToken c1, CancellationToken c2)
    {
        if (!c1.CanBeCanceled || c2.IsCancellationRequested) return c2;
        if (!c2.CanBeCanceled || c1.IsCancellationRequested) return c1;
        if (c1 == c2) return c1;
        return CancellationTokenSource.CreateLinkedTokenSource(c1, c2).Token;
    }
}
