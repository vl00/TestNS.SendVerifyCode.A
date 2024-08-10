using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common;

public static class PipelineUtils
{
    public static ValueTask<ReadResult> ReadAtLeastAsync(PipeReader pipeReader, int minimumSize, bool throwIfResultHasFlag = true, CancellationToken cancellationToken = default)
    {
        return InternalReadAsync(pipeReader, true, minimumSize, throwIfResultHasFlag, cancellationToken);
    }

    static async ValueTask<ReadResult> InternalReadAsync(PipeReader pipeReader, bool mustAtLeast, int minimumSize, bool throwIfResultHasFlag, CancellationToken cancellationToken)
    {
        var result = mustAtLeast || minimumSize >= 0 ? await pipeReader.ReadAtLeastAsync(minimumSize, cancellationToken) : await pipeReader.ReadAsync(cancellationToken);
        var buffer = result.Buffer;
        var consumed = buffer.Start;
        var examined = buffer.End;
        if (result.IsCompleted || result.IsCanceled)
        {
            pipeReader.AdvanceTo(consumed, examined);
            if (throwIfResultHasFlag) throw new InvalidOperationException("reader is closed");
        }
        else if (buffer.IsEmpty)
        {
            pipeReader.AdvanceTo(consumed, examined);
        }
        return result;
    }

    public static ReadOnlySequence<byte> GetBufferByLength(ref ReadResult result, int len)
    {
        if (len < 0) return ReadOnlySequence<byte>.Empty;
        var buffer = result.Buffer;
        return buffer.Length <= len ? buffer : buffer.Slice(0, len);
    }

    public static long Advance(PipeReader pipeReader, ref ReadResult result, long c)
    {
        var buffer = result.Buffer;
        var consumed = buffer.Start;
        var examined = buffer.End;
        if (c > 0) consumed = examined = buffer.GetPosition(c, consumed);
        pipeReader.AdvanceTo(consumed, examined);
        return c;
    }

    public static int Advance(PipeWriter pipeWriter, int c)
    {
        pipeWriter.Advance(c);
        return c;
    }

    public static void WriteBytes(PipeWriter writer, int size, byte[] source)
    {
        var s = new Span<byte>(source);
        WriteBytes(writer, size, ref s);
    }
    public static void WriteBytes(PipeWriter writer, int size, ref Memory<byte> source)
    {
        var s = source.Span;
        WriteBytes(writer, size, ref s);
    }
    public static void WriteBytes(PipeWriter writer, int size, ref Span<byte> source)
    {
        int i = 0, len0 = source.Length;
        for (int c, len = len0; i < len0;)
        {
            var sp = writer.GetSpan(size);
            if (sp.Length < len)
            {
                source.Slice(i, sp.Length).CopyTo(sp);
                c = sp.Length;
            }
            else
            {
                (i == 0 ? source : source[i..]).CopyTo(sp);
                c = source.Length - i;
            }
            i += Advance(writer, c);
            len -= c;
        }
    }

    public static unsafe int WriteString(PipeWriter writer, int size, in ReadOnlySpan<char> cs, Encoding encoding = null)
    { 
        encoding ??= Encoding.UTF8;
        var bc = encoding.GetMaxByteCount(1) + 2;
        var j = 0;
        fixed (char* chars = &MemoryMarshal.GetReference(cs)) 
        {
            for (int i = 0, clen = cs.Length; i < clen;)
            {
                var bys = writer.GetSpan(size);
                var blen = bys.Length;
                if (blen < bc) throw new Exception("blen is too small"); 
                fixed (byte* bytes = &MemoryMarshal.GetReference(bys))
                {
                    for (var bp = bytes; ;)
                    {
                        var c = encoding.GetBytes(chars + i, 1, bp, blen);
                        blen -= c;
                        bp += c;
                        i++;
                        if (blen < bc || i >= clen)
                        {
                            j += Advance(writer, bys.Length - blen);
                            //if (j > writeable.MaxLength)
                            //    throw new Exception($"content bytes must less than {writeable.MaxLength}");
                            break;
                        }
                    }
                }
            }
        }
        return j;
    }

    public static bool WriteStruct<T>(PipeWriter writer, int size, ref T value) where T : struct
    {
        var bys = writer.GetSpan(size);
        var c = Unsafe.SizeOf<T>(); 
        var b = c <= bys.Length;
        if (b)
        {
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(bys), value);
            writer.Advance(c);
        }
        return b;
    }
    public static bool WriteStruct<T>(PipeWriter writer, int size, ref T value, int c) where T : struct
    {
        var bys = writer.GetSpan(size);
        var b = MemoryMarshal.TryWrite(bys, value); 
        if (b) writer.Advance(c);
        return b;
    }
}
