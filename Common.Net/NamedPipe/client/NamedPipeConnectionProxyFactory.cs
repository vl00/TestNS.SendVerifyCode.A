using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Net.Clients;

public class NamedPipeConnectionProxyFactory(string address) 
    : IConnectionFactory
{
    public IConnection Create()
    {
        return new NamedPipeConnectionProxy(this, address);
    }
}

sealed class NamedPipeConnectionProxy(NamedPipeConnectionProxyFactory factory, string address) 
    : IConnection, IBufferWriter, IDisposable
{
    CancellationTokenSource _close;
    NamedPipeClientStream _namedPipeClientStream;

    public string Id { get; } = Guid.NewGuid().ToString();
    public bool IsOpen => _namedPipeClientStream?.IsConnected ?? false;
    public CancellationToken Closing => _close.Token;

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        if (factory == null) throw new ObjectDisposedException(nameof(factory));

        _close ??= new CancellationTokenSource();

        if (_namedPipeClientStream == null)
        {
            _namedPipeClientStream = new NamedPipeClientStream(
                ".",
                address,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);
        }
        if (_namedPipeClientStream.IsConnected) return Task.CompletedTask;
        return _namedPipeClientStream.ConnectAsync(cancellationToken);
    }

    public void Close()
    {
        if (factory == null) return;
        if (!IsOpen) return;
        _namedPipeClientStream.Close();
        _namedPipeClientStream = null;
        _close?.Cancel();
        _close = null;
    }

    public void Dispose()
    {
        if (factory == null) return;
        factory = null;
        _namedPipeClientStream?.Dispose();
        _namedPipeClientStream = null;
        _close?.Cancel();
        _close = null;
    }

    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        => _namedPipeClientStream.ReadAsync(buffer, offset, count, CancellationTokenSource.CreateLinkedTokenSource(Closing, cancellationToken).Token);

    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        => _namedPipeClientStream.WriteAsync(buffer, offset, count, CancellationTokenSource.CreateLinkedTokenSource(Closing, cancellationToken).Token);

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => new(_namedPipeClientStream.FlushAsync(CancellationTokenSource.CreateLinkedTokenSource(Closing, cancellationToken).Token));

    public ValueTask<int> ReadAsync(Memory<byte> memory, CancellationToken cancellationToken = default)
    {
#if Net4x
        var by = memory.GetArray();
        return new ValueTask<int>(ReadAsync(by.Array, by.Offset, by.Count, cancellationToken));
#else
        return _namedPipeClientStream.ReadAsync(memory, cancellationToken);
#endif
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
    {
#if Net4x
        var by = memory.GetArray();
        return new ValueTask(WriteAsync(by.Array, by.Offset, by.Count, cancellationToken));
#else
        return _namedPipeClientStream.WriteAsync(memory, cancellationToken);
#endif
    }

    public void Write(byte[] buffer, int offset, int count)
        => _namedPipeClientStream.Write(buffer, offset, count);

    public void Write(ReadOnlySpan<byte> buffer)
    {
        _namedPipeClientStream.Write(buffer);
    }
}
