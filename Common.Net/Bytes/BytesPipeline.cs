using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Net;

public class BytesPipeline : IDisposable
{
    readonly BytesPipelineOption _option;
    readonly Pipe _pipe1, _pipe2;
    bool _disposed;

    protected readonly IBytesStream Connection;

    public BytesPipeline(IBytesStream bytes, BytesPipelineOption option = null)
    {
		Connection = bytes;
        _option = option ?? new();
        _pipe1 = new(option.Options1 ?? new(useSynchronizationContext: false, minimumSegmentSize: 8));
        _pipe2 = new(option.Options2 ?? new(useSynchronizationContext: false, minimumSegmentSize: 8));
    }

    protected BytesPipelineOption Option => _option;

    public int MaxContentLength => _option.MaxContentLength;

    PipeWriter _input => _pipe1.Writer;
    PipeReader _output => _pipe2.Reader;

    public PipeReader Reader => _pipe1.Reader;
    public PipeWriter Writer => _pipe2.Writer;

    public void Dispose()
    {
        if (_disposed) return; 
        _disposed = true;
        _pipe1.Reader.Complete();
        _pipe2.Writer.Complete();
        _input.CancelPendingFlush();
        _output.CancelPendingRead();
    }

    public Task RunAsync(CancellationToken cancellation = default)
    {
        return Task.WhenAll(OnReadInput(cancellation), OnWriteOutput(cancellation), OnRunning(cancellation));
    }

    protected virtual async Task OnRunning(CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
            await OnRunningOnEach(cancellation);
    }

    protected virtual Task OnRunningOnEach(CancellationToken cancellation)
    {
        return Task.Delay(0); // try {} catch {}
    }

    private async Task OnReadInput(CancellationToken cancellation)
    {
        Exception error = null;
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                var buffer = _input.GetMemory(_option.AllocBufferSize);

                var byrecd = await Connection.ReadAsync(buffer, cancellation).ConfigureAwait(false); 
                if (byrecd == 0) break;
                _input.Advance(byrecd);

                var fr = await _input.FlushAsync(cancellation); 
                if (fr.IsCanceled || fr.IsCompleted) break;
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _input.Complete(error);
        }
    }

    private async Task OnWriteOutput(CancellationToken cancellation)
    {
        Exception error = null;
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                var r = await _output.ReadAsync(cancellation); 
                if (r.IsCanceled) break;

                var buffer = r.Buffer;
                var consumed = buffer.Start;
                var examined = buffer.End;
                var isCompleted = r.IsCompleted;

                if (!buffer.IsEmpty)
                {
                    if (buffer.Length > MaxContentLength)
                        throw new InvalidOperationException($"content must be less than {MaxContentLength}");

                    if (Connection is IBufferWriter bwr)
                    {
                        foreach (var m in buffer)
                            bwr.Write(m.Span);
                    }
                    else
                    {
                        foreach (var m in buffer)
                            await Connection.WriteAsync(m, cancellation).ConfigureAwait(false);
                    }
                    await Connection.FlushAsync(cancellation).ConfigureAwait(false);

                    consumed = examined;
                }

                _output.AdvanceTo(consumed, examined);
                if (isCompleted) break;
            }
        }
        catch (Exception ex)
        {
            error = ex;
        }
        finally
        {
            _output.Complete(error);
        }
    }
}
