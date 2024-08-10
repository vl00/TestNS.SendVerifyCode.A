using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Common.Net.Virtuals;

public class DefaultResolveHelper : IResolveHelper
{
    public virtual void ResolveMethodArgs(string clss, string method, ref int index, ref int blen, ref ReadOnlySequence<byte> buffer, out object data)
    {
        throw new NotSupportedException("override if need call method");
    }

    public virtual Task<(bool, object)> CallMethod(IServiceProvider sp, string clss, string method, List<(int, object)> ps)
    {
        throw new NotSupportedException("override if need call method");
    }

    public virtual IMemoryOwner<byte> ResolveNull(int blen) 
    {
        Debug.Assert(blen <= 0);
        return null;
    }

    public virtual void WriteNull(PipeWriter writer)
    {
        VirtualConnectionExtension.WriteOp(writer, 1);
        VirtualConnectionExtension.WriteLength(writer, 0, (byte)'\n');
    }

    public virtual bool WriteArgs(PipeWriter writer, int size, object o, out int blen, List<Action> actions)
    {
        blen = 0;
        switch (o)
        {
            case null:
                return true;

            case byte _byte:
                blen = 1;
                actions.Add(() => _ = PipelineUtils.WriteStruct(writer, size, ref _byte) ? o : throw new Exception());
                return true;
            case sbyte _sbyte:
                blen = 1;
                actions.Add(() => _ = PipelineUtils.WriteStruct(writer, size, ref _sbyte) ? o : throw new Exception());
                return true;

            case bool _bool:
                blen = 1;
                actions.Add(() => _ = PipelineUtils.WriteStruct(writer, size, ref _bool) ? o : throw new Exception());
                return true;

            case short _short:
                blen = 2;
                actions.Add(() => _ = PipelineUtils.WriteStruct(writer, size, ref _short) ? o : throw new Exception());
                return true;
            case ushort _ushort:
                blen = 2;
                actions.Add(() => _ = PipelineUtils.WriteStruct(writer, size, ref _ushort) ? o : throw new Exception());
                return true;

            case int _int:
                blen = 4;
                actions.Add(() => _ = PipelineUtils.WriteStruct(writer, size, ref _int) ? o : throw new Exception());
                return true;
            case uint _uint:
                blen = 4;
                actions.Add(() => _ = PipelineUtils.WriteStruct(writer, size, ref _uint) ? o : throw new Exception());
                return true;

            case long _long:
                blen = 8;
                actions.Add(() => _ = PipelineUtils.WriteStruct(writer, size, ref _long, 8) ? o : throw new Exception());
                return true;
            case ulong _ulong:
                blen = 8;
                actions.Add(() => _ = PipelineUtils.WriteStruct(writer, size, ref _ulong, 8) ? o : throw new Exception());
                return true;

            

            case byte[] _byteArr:
                blen = _byteArr.Length;
                actions.Add(() => PipelineUtils.WriteBytes(writer, size, _byteArr));
                return true;
            case Memory<byte> _memory:
                blen = _memory.Length;
                actions.Add(() => PipelineUtils.WriteBytes(writer, size, ref _memory));
                return true;

            
            default:
                {
                }
                break;
        }
        return false;
    }
}
