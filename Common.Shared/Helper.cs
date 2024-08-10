using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static partial class Program_ext
{
    [DebuggerStepThrough, DebuggerNonUserCode]
    public static void Done(params object[] args) { }

    [DebuggerStepThrough]
    public static void DisposeEx<T>(this T inst)
    {
        if (inst is IAsyncDisposable ad) _ = ad.DisposeAsync();
        else if (inst is IDisposable d) d.Dispose();
    }


    [DebuggerStepThrough]
    public static T Exchange<T>(ref T inst, T value = default)
    {
        var o = inst;
        inst = value;
        return o;
    }

    public static T InterlockedExchange<T>(ref T inst, T value = default) where T : class
    {
        return Interlocked.Exchange(ref inst, value);
    }

    [DebuggerStepThrough]
    public static int CompareExchange(ref int location, int value, int comparand, ref bool success)
    {
        var result = Interlocked.CompareExchange(ref location, value, comparand);
        success = (result == comparand);
        return result;
    }

    [DebuggerStepThrough]
    public static object[] Pargs0(params object[] arr) => arr;
    [DebuggerStepThrough]
    public static object[] PArray0(params object[] arr) => arr;
    [DebuggerStepThrough]
    public static T[] PArray<T>(params T[] arr) => arr;

    [DebuggerStepThrough]
    public static Dictionary<string, object> PDict(params (string, object)[] kvs) => kvs.ToDictionary(_ => _.Item1, _ => _.Item2);

    [DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode, StackTraceHidden, DebuggerStepperBoundary]
    public static T Tryv<T>(Func<T> func, T defv = default)
    {
        try { return func(); }
        catch { return defv; }
    }
    [DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode, StackTraceHidden, DebuggerStepperBoundary]
    public static T Tryv0<T>(Func<T> func, T defv = default)
    {
        try
        {
            var x = func();
            if (x == null) return defv;
            return x;
        }
        catch { return defv; }
    }

    [DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode, StackTraceHidden, DebuggerStepperBoundary]
    internal static T Tryv<T>(Func<T> func, Func<Exception, Exception> ex)
	{
		try { return func(); }
        catch (Exception ex0) 
        { 
			if (ex != null) ex0 = ex(ex0);
			if (ex0 != null) throw ex0;
			else throw;
        }
	}

    [DebuggerStepThrough, DebuggerHidden, DebuggerNonUserCode, StackTraceHidden, DebuggerStepperBoundary]
    internal static T Tryv<T>(Func<T> func, Exception ex)
	{
		try { return func(); }
        catch
        { 
			if (ex != null) throw ex;
			else throw;
        }
	}

    [DebuggerStepThrough]
    public static IEnumerable<int> FromRange(int start, int end) => Enumerable.Range(start, end + 1 - start);

    internal static dynamic AsDynamic<T>(this T obj) => (dynamic)obj;

    internal static T IsTo<T>(this object obj)
    {
        return obj is T t ? t : throw new InvalidCastException("Invalid cast or obj may be null .");
    }

    internal static T IsTo<T>(this object obj, T defv)
    {
        return obj is T t ? t : defv;
    }

    public static string ExEnvv(string str)
    {
        return Environment.ExpandEnvironmentVariables(str);
    }

    public static byte[] Utf8GetBytes(string str) => Encoding.UTF8.GetBytes(str);
    public static string Utf8GetString(byte[] bys) => Encoding.UTF8.GetString(bys);
    public static string Utf8GetString(ReadOnlySpan<byte> bys) => Encoding.UTF8.GetString(bys);

    [DebuggerStepThrough, DebuggerNonUserCode]
    internal static async Task Pause(string msg = null)
    {
        if (!string.IsNullOrEmpty(msg)) Console.Write(msg);
        await Task.Delay(100).ConfigureAwait(false);
        Console.ReadLine();
    }

    /// <summary>
    /// 用于链式调用. eg:
    /// <code>
    /// obj.GetType().GetMethod("...", ...)
    ///   .Self(out var mi)
    ///   .Invoke(mi.IsStatic ? null : obj, new[] { ... });
    /// </code>
    /// </summary>
    public static T Self<T>(this T obj, out T self)
    {
        self = obj;
        return obj;
    }

    public static T Self<T>(this T obj, Action<T> action) => Self(obj, out _, action);
    public static T Self<T>(this T obj, out T self, Action<T> action)
    {
        self = obj;
        action?.Invoke(obj);
        return obj;
    }
	
	public static bool ForOnceIf(ref bool b)
	{
	    var b0 = b;
	    b = false;
	    return b0;
	}
}
