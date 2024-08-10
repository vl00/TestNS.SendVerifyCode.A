using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Common;

[DebuggerTypeProxy(typeof(Fn2Result_DebuggerView))]
public abstract partial class Fn2Result
{
    //long? _MsgTimeStamp;
    //public long MsgTimeStamp
    //{
    //    get => _MsgTimeStamp ?? (_MsgTimeStamp = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds()).Value;
    //    set => _MsgTimeStamp = value;
    //}

    [IgnoreDataMember] bool _success;
    [Obsolete("use IsSucceeded() and SetIsSucceeded() instead")]
    public bool Succeed
    {
        get => _success;
        set => _success = value;
    }

    /// <summary>错误码|状态码</summary>
    public long Code { get; set; }
    ///// <summary>同code一样</summary>
    //public long Status
    //{
    //    get => Code;
    //    set => Code = value;
    //}

    /// <summary>(错误)消息</summary>
    public string Msg { get; set; }

    public string StackTrace { get; set; }

    /// <summary>true=成功 false=失败</summary>
    public virtual bool IsSucceeded() => _success;
    public virtual void SetIsSucceeded(bool value) => _success = value;

    public abstract object GetData();
    public abstract void SetData(object data);

    sealed class Fn2Result_DebuggerView
    {
        readonly Fn2Result _this;

        public Fn2Result_DebuggerView(Fn2Result _this) => this._this = _this;

        public bool Succeed => _this._success;
        public long Code => _this.Code;
        public string Msg => _this.Msg;
        public object Data => _this.GetData();
    }
}

public partial class Fn2Result
{
    public static Fn2Result<object> OK(long code = 0) // =200
        => new Fn2Result<object> { _success = true, Code = code, Msg = "ok" };

    public static Fn2Result<T> OK<T>(T data = default, long code = 0) // =200
        => new Fn2Result<T> { _success = true, Data = data, Code = code, Msg = "ok" };

    public static Fn2Result<object> Fail(long errcode)
        => new Fn2Result<object> { _success = false, Code = errcode };

    public static Fn2Result<object> Fail(string errmsg, long errcode = -1) // =400
        => new Fn2Result<object> { _success = false, Msg = errmsg, Code = errcode };

    public static Fn2Result<T> Fail<T>(long errcode = -1) // =400
        => new Fn2Result<T> { _success = false, Code = errcode };

    public static Fn2Result<T> Fail<T>(string errmsg, long errcode = -1) // =400
        => new Fn2Result<T> { _success = false, Msg = errmsg, Code = errcode };
}

public class Fn2Result<T> : Fn2Result
{
#pragma warning disable CS0109 // 成员不会隐藏继承的成员；不需要关键字 new
    /// <summary>数据</summary>
    public new T Data { get; set; }
#pragma warning restore CS0109 // 成员不会隐藏继承的成员；不需要关键字 new

    public override object GetData() => Data;
    public override void SetData(object data) => Data = (T)data;

    public Fn2Result<T> SetData(T data)
    {
        this.Data = data;
        return this;
    }
}
