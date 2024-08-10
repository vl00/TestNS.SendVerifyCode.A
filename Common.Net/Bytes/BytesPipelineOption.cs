using System;
using System.Buffers;
using System.IO.Pipelines;

namespace Common.Net;

public class BytesPipelineOption
{
    public MemoryPool<byte> Pool => Options1?.Pool ?? Options2?.Pool ?? MemoryPool<byte>.Shared;

    public int MaxContentLength = 4096;
    public int AllocBufferSize = 512;

    public PipeOptions Options1; // null for new(useSynchronizationContext: false, minimumSegmentSize: 8)
    public PipeOptions Options2; // null for new(useSynchronizationContext: false, minimumSegmentSize: 8)
}
