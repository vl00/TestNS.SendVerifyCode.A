using Microsoft.Extensions.DependencyInjection;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Net.Virtuals;

public class VirtualConnectionCollection : BytesPipeline, IVirtualConnectionCollection, IDisposable
{
    SemaphoreSlim _writerSlim = new(1);
    readonly ConcurrentDictionary<long, VirtualConnection> _virtualConnections = new();
    readonly IResolveHelper _resolveHelper;
    readonly IServiceProvider _serviceProvider;

    public new IConnection Connection => base.Connection as IConnection;

    public int AllocBufferSize => base.Option.AllocBufferSize;

    public IServiceProvider ServiceProvider => _serviceProvider;

    public VirtualConnectionCollection(IConnection connection, BytesPipelineOption option, IServiceProvider serviceProvider)
        : base(connection, option)
    {
        _serviceProvider = serviceProvider;
        _resolveHelper = serviceProvider.GetService<IResolveHelper>();
    }

    public new void Dispose()
    {
        base.Dispose();

        var ws = _writerSlim;
        if (ws == null) return;
        _writerSlim = null;

        ws.Dispose();

        foreach (var (_, v) in _virtualConnections)
            v.Dispose();
    }


    protected override async Task OnRunningOnEach(CancellationToken cancellation)
    {
        try
        {
            var result = await PipelineUtils.ReadAtLeastAsync(Reader, 8 + 1 + 1 + 1, true, cancellation);
            var c = VirtualConnectionExtension.ResolveFrameId(result.Buffer, out var frameId, out var op);
            PipelineUtils.Advance(Reader, ref result, c);

            if (op == 0) await OnRunningOnEach_op0(frameId, cancellation);
            else if (op == 1) await OnRunningOnEach_op1(frameId, cancellation);
            //else if (op == 2) await OnRunningOnEach_op2(frameId, cancellation);
        }
        catch when (!cancellation.IsCancellationRequested)
        {
            await Connection.CloseAsync();
            throw;
        }
    }

    private async Task OnRunningOnEach_op0(long frameId, CancellationToken cancellation)
    {
        ReadResult result;

        var ps = new List<(int, object)>(6);
        while (true)
        {
            result = await PipelineUtils.ReadAtLeastAsync(Reader, 5, true, cancellation);
            VirtualConnectionExtension.ResolveLength(result.Buffer, out var blen, out var b);
            PipelineUtils.Advance(Reader, ref result, 5);
            ps.Add((blen, default));
            if (b) break;
        }
        if (ps.Count < 1) throw new Exception("resolve fail");

        result = await PipelineUtils.ReadAtLeastAsync(Reader, ps[0].Item1, true, cancellation);
        VirtualConnectionExtension.ResolveClassMethod(result.Buffer, ps[0].Item1, out var _class, out var _method);
        PipelineUtils.Advance(Reader, ref result, ps[0].Item1);
        if (string.IsNullOrEmpty(_class) || string.IsNullOrEmpty(_method)) throw new Exception("resolve fail class and method");
		
        for (var i = 0; ;)
        {
            var hasBuffer = (i + 1) < ps.Count;
            var (blen, _) = hasBuffer ? ps[i + 1] : (-2, null);
			result = blen <= 0 ? default : await PipelineUtils.ReadAtLeastAsync(Reader, blen, true, cancellation);
            var buffer = blen <= 0 ? default : PipelineUtils.GetBufferByLength(ref result, blen);
            //
LB_ResolveMethodArgs:
            var idx = i;
            object data;
            try { _resolveHelper.ResolveMethodArgs(_class, _method, ref idx, ref blen, ref buffer, out data); }
            catch 
            {
                if (blen > 0) PipelineUtils.Advance(Reader, ref result, blen);
                throw;
            }
            //
            if (idx == -1) break;
            else if (idx == i)
            {
                if (blen > 0) PipelineUtils.Advance(Reader, ref result, blen);
                ps[++i] = (blen, data);
            }
            else
            {
                Debug.Assert(idx == i + 1);
                ps.Insert(idx, (-1, data)); 
                i = idx++;
                goto LB_ResolveMethodArgs;
            }
        }

        ps.RemoveAt(0);

        OnReceivedCallMethod(frameId, _class, _method, ps);
    }

    protected virtual void OnReceivedCallMethod(long frameId, string @class, string method, List<(int, object)> ps) 
    { 
        // need override
    }

    private async Task OnRunningOnEach_op1(long frameId, CancellationToken cancellation)
    {
        var result = await PipelineUtils.ReadAtLeastAsync(Reader, 5, true, cancellation);
        VirtualConnectionExtension.ResolveLength(result.Buffer, out var blen, out var b);
        PipelineUtils.Advance(Reader, ref result, 5);
        if (!b) throw new Exception("resolve fail");

        var isNull = blen <= 0;
        result = isNull ? default : await PipelineUtils.ReadAtLeastAsync(Reader, blen, true, cancellation);
        var buffer = isNull ? default : PipelineUtils.GetBufferByLength(ref result, blen);

        var memoryOwner = isNull ? _resolveHelper.ResolveNull(blen) : MemoryPool<byte>.Shared.Rent(blen);
        if (!isNull)
        {
            var m0 = memoryOwner!.Memory;
            VirtualConnectionExtension.CopyTo(ref buffer, ref m0);
            PipelineUtils.Advance(Reader, ref result, blen);
        }

        OnReceivedData(frameId, memoryOwner, blen);
    }

    protected virtual void OnReceivedData(long frameId, IMemoryOwner<byte> data, int blen)
    {
        // need override
    }

    public async Task CallMethod(IServiceProvider serviceProvider, VirtualConnection v, string clss, string method, List<(int, object)> ps)
    {
        var sp = serviceProvider;
        var b = sp == null;
        AsyncServiceScope scope = default;
        if (b)
        {
            var f = _serviceProvider.GetService<IServiceScopeFactory>();
            scope = f.CreateAsyncScope();
            sp = scope.ServiceProvider;
        }
        sp.GetService<IVirtualConnectionWrap>()?.Set(sp, v);
        try
        {
            var resolveHelper = b ? _resolveHelper : (sp.GetService<IResolveHelper>() ?? _resolveHelper);
            var (hasResult, result) = await resolveHelper.CallMethod(sp, clss, method, ps);
            if (!hasResult) return;

            switch (result)
            {
                case null:
                    await InternalWriteAsync(v.Id, (writer, _) =>
                    {
                        resolveHelper.WriteNull(writer);
                        return Task.CompletedTask;
                    }, Connection.Closing);
                    break;

                case string str:
                    await InternalWriteAsync(v.Id, (_, c) => WriteUtf8String(str, c), Connection.Closing);
                    break;

                case byte[] bysArr:
                    await InternalWriteAsync(v.Id, (_, c) => WriteData(bysArr, c), Connection.Closing);
                    break;
                case Memory<byte> bys:
                    await InternalWriteAsync(v.Id, (_, c) => WriteData(bys, c), Connection.Closing);
                    break;

                default:
                    throw new NotImplementedException("todo serialize");
            }
        }
        finally
        {
            if (b) await scope.DisposeAsync();
        }
    }

    CancellationToken IVirtualConnectionCollection.Closing => Connection.Closing;

    VirtualConnection IVirtualConnectionCollection.GetOrAddVirtualConnection(long frameId, bool addIfNotExist) => GetOrAddVirtualConnection(frameId, addIfNotExist);
    protected internal VirtualConnection GetOrAddVirtualConnection(long frameId, bool addIfNotExist)
    {
        if (!_virtualConnections.TryGetValue(frameId, out var virtualConnection))
        {
            if (addIfNotExist) virtualConnection = _virtualConnections.GetOrAdd(frameId, new VirtualConnection(frameId, this));
            else throw new Exception($"not exist frameId={frameId}");
        }
        return virtualConnection;
    }

    void IVirtualConnectionCollection.RemoveVirtualConnection(VirtualConnection virtualConnection)
    {
        _virtualConnections.TryRemove(new(virtualConnection.Id, virtualConnection));
    }

    Task IVirtualConnectionCollection.WriteAsync(long frameId, Func<PipeWriter, CancellationToken, Task> doWrite, CancellationToken cancellationToken) => InternalWriteAsync(frameId, doWrite, cancellationToken);
    async Task InternalWriteAsync(long frameId, Func<PipeWriter, CancellationToken, Task> doWrite, CancellationToken cancellationToken)
    {
        await _writerSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            VirtualConnectionExtension.WriteFrameId(Writer, frameId);
            await Writer.FlushAsync(cancellationToken);

            var t = doWrite?.Invoke(Writer, cancellationToken);
            if (t?.IsCompletedSuccessfully == false) await t;

            await Writer.FlushAsync(cancellationToken);
        }
        finally
        {
            _writerSlim.Release();
        }
    }

    Task IVirtualConnectionCollection.WriteAsync(long frameId, Memory<byte> data, CancellationToken cancellationToken) => InternalWriteAsync(frameId, (_, c) => WriteData(data, c), cancellationToken);
    async Task WriteData(Memory<byte> data, CancellationToken cancellationToken)
    {
        VirtualConnectionExtension.WriteOp(Writer, 1);

        VirtualConnectionExtension.WriteLength(Writer, data.Length, (byte)'\n');
        await Writer.FlushAsync(cancellationToken);

        PipelineUtils.WriteBytes(Writer, this.Option.AllocBufferSize, ref data);
        await Writer.FlushAsync(cancellationToken);
    }

    Task IVirtualConnectionCollection.WriteAsync(long frameId, string utf8String, CancellationToken cancellationToken) => InternalWriteAsync(frameId, (_, c) => WriteUtf8String(utf8String, c), cancellationToken);
    async Task WriteUtf8String(string utf8String, CancellationToken cancellationToken)
    {
        VirtualConnectionExtension.WriteOp(Writer, 1);

        var l0 = Encoding.UTF8.GetByteCount(utf8String);
        VirtualConnectionExtension.WriteLength(Writer, l0, (byte)'\n');
        var j = PipelineUtils.WriteString(Writer, this.Option.AllocBufferSize, utf8String, Encoding.UTF8);
        Debug.Assert(l0 == j);

        await Writer.FlushAsync(cancellationToken);
    }
}
