using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Net.Servers;

public class NamedPipeConnectionListener : IConnectionListener
{
    readonly string address;
    readonly int maxFrameBodyLength;

    public NamedPipeConnectionListener(string address, int maxFrameBodyLength)
    {
        this.address = address;
        this.maxFrameBodyLength = maxFrameBodyLength;
    }

    public async Task<IConnection> WaitForConnectionAsync(CancellationToken cancellationToken = default)
    {
        var _namedPipeServerStream = new NamedPipeServerStream(
            address,
            PipeDirection.InOut,
            254,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
            maxFrameBodyLength, maxFrameBodyLength);

        var connection = new NamedPipeConnection(this, _namedPipeServerStream);
        await connection.InternalOpen(cancellationToken).ConfigureAwait(false);

        return connection;
    }
}

sealed class NamedPipeConnection : IConnection, IBufferWriter, IDisposable
{
    NamedPipeServerStream _namedPipeServerStream;
    NamedPipeConnectionListener _connectionListener;
    CancellationTokenSource _close;

    public string Id { get; }
    public bool IsOpen => _namedPipeServerStream?.IsConnected ?? false;
    public CancellationToken Closing => _close.Token;

    internal NamedPipeConnection(NamedPipeConnectionListener connectionListener, NamedPipeServerStream namedPipeServerStream)
    {
        Id = Guid.NewGuid().ToString("n");
        _connectionListener = connectionListener;
        _namedPipeServerStream = namedPipeServerStream;
    }

    internal Task InternalOpen(CancellationToken cancellationToken)
    {
        _close ??= new CancellationTokenSource();

        if (!_namedPipeServerStream.IsConnected)
            return _namedPipeServerStream.WaitForConnectionAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public void Close()
    {
        if (_connectionListener == null) return;
        if (_namedPipeServerStream?.IsConnected == true) _namedPipeServerStream.Disconnect();
        _close?.Cancel();
        _close = null;
    }

    public void Dispose()
    {
        if (_connectionListener == null) return;
        _connectionListener = null;
        try
        {
            if (_namedPipeServerStream?.IsConnected == true) 
                _namedPipeServerStream.Disconnect();
        }
        finally
        {
            _namedPipeServerStream.Dispose();
            _namedPipeServerStream = null;
        }
        _close?.Cancel();
        _close = null;
    }

    public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        => _namedPipeServerStream.ReadAsync(buffer, offset, count, CancellationTokenSource.CreateLinkedTokenSource(Closing, cancellationToken).Token);

    public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        => _namedPipeServerStream.WriteAsync(buffer, offset, count, CancellationTokenSource.CreateLinkedTokenSource(Closing, cancellationToken).Token);

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
        => new ValueTask(_namedPipeServerStream.FlushAsync(CancellationTokenSource.CreateLinkedTokenSource(Closing, cancellationToken).Token));

    public ValueTask<int> ReadAsync(Memory<byte> memory, CancellationToken cancellationToken = default)
    {
#if Net4x
        var by = memory.GetArray();
        return new ValueTask<int>(ReadAsync(by.Array, by.Offset, by.Count, cancellationToken));
#else
        return _namedPipeServerStream.ReadAsync(memory, cancellationToken);
#endif
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default)
    {
#if Net4x
        var by = memory.GetArray();
        return new ValueTask(WriteAsync(by.Array, by.Offset, by.Count, cancellationToken));
#else
        return _namedPipeServerStream.WriteAsync(memory, cancellationToken);
#endif
    }

    public void Write(byte[] buffer, int offset, int count)
        => _namedPipeServerStream.Write(buffer, offset, count);

    public void Write(ReadOnlySpan<byte> buffer)
    {
        _namedPipeServerStream.Write(buffer);
    }
}
