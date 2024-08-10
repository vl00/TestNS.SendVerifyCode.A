using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Common;

public class BytesSegment1 : ReadOnlySequenceSegment<byte>, IDisposable
{
    IMemoryOwner<byte> _mo;

    public new BytesSegment1 Next
    {
        get => base.Next as BytesSegment1;
        set => base.Next = value;
    }

    public bool IsDisposed
    {
        get
        {
            try
            {
                _ = _mo?.Memory; 
                return false;
            }
            catch { return true; }
        }
    }

    public void Dispose()
    {
        _mo?.Dispose();
        Next?.Dispose();
    }

    public void SetNext(BytesSegment1 next) => base.Next = next;

    public void SetRunningIndex(long runningIndex) => RunningIndex = runningIndex;

    public void SetMemory(IMemoryOwner<byte> mo)
    {
        _mo = mo;
        Memory = mo.Memory;
    }

    public void SetMemory(Memory<byte> memory) => Memory = memory;

    public static void ToReadOnlySequence(byte[] source, MemoryPool<byte> pool, int rentSize, out ReadOnlySequence<byte> readOnlySequence, out BytesSegment1 start, out BytesSegment1 end)
    {
        Span<byte> b = source;
        ToReadOnlySequence(ref b, pool, rentSize, out readOnlySequence, out start, out end);
    }

    public static void ToReadOnlySequence(Memory<byte> source, MemoryPool<byte> pool, int rentSize, out ReadOnlySequence<byte> readOnlySequence, out BytesSegment1 start, out BytesSegment1 end)
    {
        Span<byte> b = source.Span;
        ToReadOnlySequence(ref b, pool, rentSize, out readOnlySequence, out start, out end);
    }

    public static void ToReadOnlySequence(ref Span<byte> source, MemoryPool<byte> pool, int rentSize, out ReadOnlySequence<byte> readOnlySequence, out BytesSegment1 start, out BytesSegment1 end)
    {
        start = end = null;
        var len1 = source.Length;
        for (var i = 0; i < len1;)
        {
            var mo = pool.Rent(rentSize);
            var j = 0;
            for (var len2 = mo.Memory.Length; j < len2; j++)
            {
                if (i + j >= len1) break;
                mo.Memory.Span[j] = source[i + j];
            }
            if (start == null)
            {
                start = end = new BytesSegment1();
                start.SetMemory(mo);
            }
            else
            {
                var l = end.RunningIndex + end.Memory.Length;
                end.SetNext(new BytesSegment1());
                end = end.Next;
                end.SetMemory(mo);
                end.SetRunningIndex(l);
            }
            if (j != mo.Memory.Length) end.SetMemory(mo.Memory[..j]);
            i += j;
        }
        readOnlySequence = new ReadOnlySequence<byte>(start, 0, end, end.Memory.Length); 
    }

    public static void ToReadOnlySequence(IEnumerable<byte[]> sources, out ReadOnlySequence<byte> readOnlySequence, out BytesSegment1 start, out BytesSegment1 end)
    {
        var s = sources.Select(_ => new Memory<byte>(_));
        ToReadOnlySequence(s, out readOnlySequence, out start, out end);
    }

    public static void ToReadOnlySequence(IEnumerable<Memory<byte>> sources, out ReadOnlySequence<byte> readOnlySequence, out BytesSegment1 start, out BytesSegment1 end)
    {
        start = end = null;
        foreach (var bytes in sources)
        {
            if (start == null)
            {
                start = end = new BytesSegment1();
                start.SetMemory(bytes);
            }
            else
            {
                var l = end.RunningIndex + end.Memory.Length;
                end.SetNext(new BytesSegment1());
                end = end.Next;
                end.SetMemory(bytes);
                end.SetRunningIndex(l);
            }
        }
        readOnlySequence = new ReadOnlySequence<byte>(start, 0, end, end.Memory.Length);
    }
}
