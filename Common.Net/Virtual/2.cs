using System;
using System.Buffers;
using System.Collections.Generic;

namespace Common.Net.Virtuals;

public readonly struct BytesData(IMemoryOwner<byte> src, int len = 0)
{
    public readonly IMemoryOwner<byte> Owner = src;
    public readonly int Length = len; 

    public Memory<byte> GetMemory()
    {
        if (Length <= 0) return Memory<byte>.Empty;
        var mo = Owner.Memory;
        return mo.Length > Length ? mo[..Length] : mo;
    }

    public Span<byte> GetSpan()
    {
        if (Length <= 0) return Span<byte>.Empty;
        var mo = Owner.Memory.Span;
        return mo.Length > Length ? mo[..Length] : mo;
    }

    public void Dispose() => Owner?.Dispose();
}

public readonly record struct CallMethodInfo(string Class, string Method, List<(int, object)> Ps);

internal interface IReceivedData { }

internal sealed class ReceivedData<T>(T value) : IReceivedData
{
    public T Value = value;
}
