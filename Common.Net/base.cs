using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Net;

public interface IConnectionListener
{
    Task<IConnection> WaitForConnectionAsync(CancellationToken cancellationToken = default);
}

public interface IConnectionFactory
{
    IConnection Create();
}

public interface IConnection : IBytesStream, IDisposable //, IAsyncDisposable
{
    string Id { get; }
    bool IsOpen { get; }
    CancellationToken Closing { get; }

    Task OpenAsync(CancellationToken cancellationToken = default) { return Task.CompletedTask; }

    void Close();
    Task CloseAsync() { Close(); return Task.CompletedTask; }

}


public interface IBufferWriter
{
    void Write(byte[] buffer, int offset, int count);
    void Write(ReadOnlySpan<byte> buffer);
    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
