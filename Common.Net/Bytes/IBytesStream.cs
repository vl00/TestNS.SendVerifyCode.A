using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Net;

public interface IBytesStream 
{
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
    ValueTask<int> ReadAsync(Memory<byte> memory, CancellationToken cancellationToken = default);

    Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);
    ValueTask WriteAsync(ReadOnlyMemory<byte> memory, CancellationToken cancellationToken = default);

    ValueTask FlushAsync(CancellationToken cancellationToken = default);
}
