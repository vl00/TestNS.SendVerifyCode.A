namespace Common;

public static class Fn2ResultExtensions
{
    public static Fn2Result<T> SetSucceeded<T>(this Fn2Result<T> r, bool value)
    {
        r.SetIsSucceeded(value);
        return r;
    }

    public static Fn2Result<T> SetData<T>(this Fn2Result<T> r, T data)
    {
        r.Data = data;
        return r;
    }

    public static Fn2Result<T> SetMsg<T>(this Fn2Result<T> r, string msg)
    {
        r.Msg = msg;
        return r;
    }

    public static void ThrowIfResultIsFailed(this Fn2Result r)
    {
        if (r.IsSucceeded()) return;
        throw new Fn2ResultException(r);
    }

    public static Fn2ResultException SetExtraData(this Fn2ResultException err, object data)
    {
        err.ExtraData = data;
        return err;
    }

    public static Fn2Result ToFn2Result(this Fn2ResultException err)
    {
        return new Fn2Result<object> { Code = err.Code, Msg = err.Message, Data = err.ExtraData, StackTrace = err.StackTrace }.SetSucceeded(false);
    }

    public static Fn2Result<T> ToFn2Result<T>(this Fn2ResultException err)
    {
        return new Fn2Result<T> { Code = err.Code, Msg = err.Message, Data = err.ExtraData is T d ? d : default, StackTrace = err.StackTrace }.SetSucceeded(false);
    }
}
