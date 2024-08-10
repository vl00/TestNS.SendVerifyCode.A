using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Common.Net.Virtuals;

public static class VirtualConnectionExtension
{
    public static IServiceCollection AddVirtualConnection(this IServiceCollection services)
    {
        services.TryAddScoped<IVirtualConnectionWrap, VirtualConnectionWrap>();
        return services;
    }

    internal static int ResolveFrameId(in ReadOnlySequence<byte> buffer, out long frameId, out byte op)
    {
        var seqReader = new SequenceReader<byte>(buffer);

        if (!(seqReader.TryPeek(8, out var by) && by is (byte)'/'))
            throw new Exception("resolve fail");
        if (!(seqReader.TryPeek(10, out by) && by is (byte)'/'))
            throw new Exception("resolve fail");

        op = seqReader.TryPeek(9, out by) ? by : default;
        frameId = seqReader.TryReadLittleEndian(out long l) ? l : default;
        return 11;
    }

    internal static void ResolveLength(in ReadOnlySequence<byte> buffer, out int len, out bool end)
    {
        var seqReader = new SequenceReader<byte>(buffer);

        if (!(seqReader.TryPeek(4, out var by) && (by is (byte)'/' or (byte)'\n')))
            throw new Exception("resolve fail");

        len = seqReader.TryReadLittleEndian(out int l) ? l : default;
        end = by is (byte)'\n';
    }

    internal static void ResolveClassMethod(in ReadOnlySequence<byte> buffer, int len, out string @class, out string method)
    {
        var bf = buffer.Length <= len ? buffer : buffer.Slice(0, len);
        var str = Encoding.UTF8.GetString(bf).AsSpan();
        var i = str.IndexOf(':');
        @class = i == -1 ? null : new string(str[..i]);
        method = i == -1 || i >= str.Length ? null : new string(str[(i + 1)..]);
    }

    internal static int CopyTo(ref ReadOnlySequence<byte> buffer, ref Memory<byte> m0)
    {
        var i = 0;
        foreach (var m in buffer)
        {
            m.CopyTo(m0[i..(i + m.Length)]);
            i += m.Length;
        }
        return i;
    }

    internal static void WriteFrameId(PipeWriter writer, long frameId)
    {
        var memory = writer.GetMemory(8 + 1);
        BitConverter.TryWriteBytes(memory.Span, frameId); 
        memory.Span[8] = (byte)'/';
        writer.Advance(8 + 1);
    }

    public static void WriteOp(PipeWriter writer, byte op)
    {
        var span = writer.GetSpan(2);
        span[0] = op;
        span[1] = (byte)'/';
        writer.Advance(2);
    }

    public static void WriteLength(PipeWriter writer, int len, byte s)
    {
        var span = writer.GetSpan(5);
        MemoryMarshal.Write(span, len); 
        span[4] = s;
        writer.Advance(5);
    }

    public static async Task<BytesData> CallMethodAsync(this VirtualConnection virtualConnection, string clss, string method, params object[] ps)
    {
        await virtualConnection.WriteCallMethod(clss, method, ps);
        var mo = await virtualConnection.ReadAsync();
        return mo;
    }

    public static Task WriteCallMethod(this VirtualConnection virtualConnection, string clss, string method, params object[] ps)
    {
        return virtualConnection.WriteAsync((writer, cancellationToken) => WriteCallMethod(virtualConnection.ServiceProvider.GetService<IResolveHelper>(), writer, virtualConnection.AllocBufferSize, clss, method, ps, cancellationToken));
    }

    static async Task WriteCallMethod(IResolveHelper resolveHelper, PipeWriter writer, int size, string clss, string method, object[] ps, CancellationToken cancellationToken)
    {
        WriteOp(writer, 0);

        List<Action> actions = new((ps?.Length ?? 0) + 1);
        var ca = $"{clss}:{method}";
        var len = Encoding.UTF8.GetByteCount(ca);
        var canNext = ps?.Length > 0;
        WriteLength(writer, len, canNext ? (byte)'/' : (byte)'\n');
        actions.Add(() => PipelineUtils.WriteString(writer, size, ca, Encoding.UTF8));

        for (var i = 0; canNext;)
        {
            if (resolveHelper?.WriteArgs(writer, size, ps[i], out len, actions) != true)
            {
                throw new InvalidOperationException($"can't write args {i}.");
            }
            canNext = (++i) < ps.Length;
            WriteLength(writer, len, canNext ? (byte)'/' : (byte)'\n');
        }

        foreach (var f in actions)
        {
            f!.Invoke();
            await writer.FlushAsync(cancellationToken);
        }
    }
}
