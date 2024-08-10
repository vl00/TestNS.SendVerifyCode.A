using Common.Net.Virtuals;
using System.IO.Pipelines;
using System.Reflection;

/**
 * for test2_server

 */

namespace TestNS.ns2;

class Aaa(ILogger<Aaa> log, WrapItems wrapItems, JobWorking jobWorking)
{
    public async Task<object> Pull(int ms, VirtualConnection vconn)
    {
        var cancel = new CancellationTokenSource(ms);
        var (item, has) = (default(object), false);
        try
        {
            item = await wrapItems.Reader.ReadAsync(cancel.Token);
            has = true;
        }
        catch when (cancel.IsCancellationRequested)
        {
            // ignore
        }
        catch (Exception ex)
        {
            log.LogError(ex, "");
        }
        if (!has)
        {
            log.LogDebug("Has no item on pull, vc id={id}", vconn.Id);
            return null;
        }
        return item;
    }

    public async Task Report(string jobId, ReportMsg reportMsg)
    {
        if (reportMsg.Status == 0)
        {
            var msg = """
                svc={svc}, xid={xid}, jobid={jobId} report:
                {strMsg}
                """;
            log.Log((LogLevel)Enum.Parse(typeof(LogLevel), reportMsg.LogLevel), 
                null, msg, 
                reportMsg.Svc, reportMsg.Xid, jobId, reportMsg.StrMsg);

            //throw new NotImplementedException();
        }
        if (reportMsg.Status > 0)
        {
            var msg = """
                 svc={svc}, xid={xid}, jobid={jobId} {r}.
                 {strMsg}
                 """;
            log.LogInformation(msg, reportMsg.Svc, reportMsg.Xid, jobId, (reportMsg.Status == 1 ? "ok" : "fail"), reportMsg.StrMsg);

            var isAllCompleted = jobWorking.Complete(jobId, reportMsg.Svc, reportMsg.Status, out var all, out var ok, out var fail);
            if (isAllCompleted)
            {
                log.LogInformation("jobid={jobId} is all completed. all={all}, ok={ok}, fail={fail}", jobId, all, ok, fail);
            }
            await default(ValueTask);
        }
    }
}

class ReportMsg
{
    public string Svc;
    public string Xid;
    public int Status = 0;
    public string LogLevel;
    public string StrMsg;
}

class ResolveHelper : DefaultResolveHelper
{
    public override void ResolveMethodArgs(string clss, string method, ref int index, ref int blen, ref ReadOnlySequence<byte> buffer, out object data)
    {
        data = null;
        clss = "Aaa"; // for test
        switch ($"{clss}:{method}:{index}")
        {
            default:
                index = -1;
                break;

            #region Aaa:B
            case "Aaa:B:0": // p1
                {
                    if (blen < -1) throw new Exception("resolve fail no buffer for args");
                    if (buffer.IsEmpty) break;
                    var s = Encoding.UTF8.GetString(buffer);
                    data = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(s);
                }
                break;
            case "Aaa:B:1":
                data = typeof(VirtualConnection);
                index++;
                break;
            case "Aaa:B:2": // p2
                {
                    if (blen < -1) throw new Exception("resolve fail no buffer for args");
					var s = Encoding.UTF8.GetString(buffer);
					data = float.Parse(s);
                }
                break;
			case "Aaa:B:3": // p3
				{
					if (blen < -1) throw new Exception("resolve fail no buffer for args");
					if (buffer.IsEmpty) break;
					using var __ = CopyTo(ref buffer, out var s);
					data = BitConverter.ToUInt64(s);
				}
				break;
            case "Aaa:B:4":
                data = typeof(IServiceScopeFactory);
                index++;
                break;
            #endregion
            
            #region Aaa:Pull
            case "Aaa:Pull:0":
                {
                    if (blen < -1) throw new Exception("resolve fail no buffer for args");
                    if (buffer.IsEmpty) break;
                    using var __ = CopyTo(ref buffer, out var s);
                    data = BitConverter.ToInt32(s);
                }
                break;
            case "Aaa:Pull:1":
                data = typeof(VirtualConnection);
                index++;
                break;
            #endregion

            #region Aaa:Report
            case "Aaa:Report:0":
                {
                    if (blen < -1) throw new Exception("resolve fail no buffer for args");
                    var s = Encoding.UTF8.GetString(buffer);
                    data = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(s);
                }
                break;
            case "Aaa:Report:1":
                {
                    if (blen < -1) throw new Exception("resolve fail no buffer for args");
                    if (buffer.IsEmpty) break;
                    var s = Encoding.UTF8.GetString(buffer);
                    data = Newtonsoft.Json.JsonConvert.DeserializeObject<ReportMsg>(s);
                }
                break;
            #endregion
        }
    }

    static IDisposable CopyTo(ref ReadOnlySequence<byte> source, out ReadOnlySpan<byte> target)
	{
		if (source.IsSingleSegment)
		{
			target = source.FirstSpan; 
			return null;
		}
		var slen = (int)source.Length;
		var mo = MemoryPool<byte>.Shared.Rent(slen);
		Span<byte> s1 = mo.Memory.Span; 
		if (s1.Length > slen) s1 = s1[..slen];
		target = s1;
		foreach (var m in source)
		{
			m.Span.CopyTo(s1);
			s1 = s1[m.Length..];
		}
		return mo;
	}

    public override async Task<(bool, object)> CallMethod(IServiceProvider sp, string clss, string method, List<(int, object)> ps)
    {
        var ty = Type.GetType(clss) ?? AppDomain.CurrentDomain.GetAssemblies().SelectMany(_ => _.GetTypes()).Where(_ => _.Name == clss).FirstOrDefault();
        if (ty == null || ty.IsAbstract || ty.IsGenericType || ty.IsGenericTypeDefinition) throw new Exception("not found type");

        var mi = ty.GetMethod(method, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        if (mi == null || mi.IsVirtual || mi.IsGenericMethod || mi.IsGenericMethodDefinition) throw new Exception("not found method");

        var o = mi.IsStatic ? null : ActivatorUtilities.CreateInstance(sp, ty);

        var args = ps.Select(a =>
        {
            if (a.Item1 == -1 && a.Item2 is Type ty)
            {
                if (ty == typeof(VirtualConnection)) return sp.GetService<IVirtualConnectionWrap>().Value;
                return sp.GetService(ty);
            }
            return a.Item2;
        });
        var ret = mi.Invoke(o, args.ToArray());

        var (has, obj) = await GetRetVal(mi, ret);
        if (!has) return default;
        if (obj == null) return (has, null);

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        return (has, json);
    }

    static async Task<(bool, object)> GetRetVal(MethodInfo mi, object ret)
    {
        if (mi.ReturnType == typeof(Task) || mi.ReturnType == typeof(ValueTask))
        {
            dynamic t = ret;
            await t;
            return default;
        }
        if (mi.ReturnType.IsGenericType)
        {
            var gt = mi.ReturnType.GetGenericTypeDefinition();
            if (gt == typeof(Task<>))
            {
                dynamic t = ret;
                return (true, await t);
            }
            if (gt == typeof(ValueTask<>))
            {
                dynamic t = ret;
                return (true, await t);
            }
        }
        return (mi.ReturnType != typeof(void), ret);
    }

    public override bool WriteArgs(PipeWriter writer, int size, object o, out int blen, List<Action> actions)
    {


        var b = base.WriteArgs(writer, size, o, out blen, actions);
        if (b) return b;


        // 空字符串的byte-len为2
        //
        var s = Newtonsoft.Json.JsonConvert.SerializeObject(o);
        blen = Encoding.UTF8.GetByteCount(s);
        actions.Add(() => PipelineUtils.WriteString(writer, size, s, Encoding.UTF8));
        return true;
    }
}
