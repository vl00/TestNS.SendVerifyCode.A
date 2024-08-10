using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Common;

public class Fn2ResultException : Exception
{
    public Fn2ResultException(Fn2Result fnResult) : base(fnResult.Msg) 
    {
        if (fnResult.IsSucceeded()) throw new ArgumentException(nameof(fnResult));
        this.Code = fnResult.Code;
        this.ExtraData = fnResult.GetData();
    }

    public Fn2ResultException(long errcode, string msg, Exception ex = null) : this(msg, errcode, ex) { }

    public Fn2ResultException(long errcode, Exception ex) : this(ex, errcode) { }

    public Fn2ResultException(string msg, long errcode, Exception ex = null) : this(ex, msg, errcode) { }

    public Fn2ResultException(Exception ex, long errcode) : this(ex, ex.Message, errcode) { }

    public Fn2ResultException(Exception ex, string msg, long errcode) : base(msg, ex)
    {
        //if (Fn2Result.CheckIfIsOK(errcode)) throw new ArgumentException(nameof(errcode));
        //if (errcode == 0) throw new ArgumentException(nameof(errcode));
        this.Code = errcode;
    }

    public long Code { get; }

    public string Category { get; set; } // use for log msg

    public string DisplayErrMsg { get; set; }

    // show errmsg on frontUI/client
    public Fn2ResultException SetDisplayErrMsg(string displayErrMsg)
    {
        this.DisplayErrMsg = displayErrMsg;
        return this;
    }

    // ExtraData // 额外数据
    public object ExtraData { get; set; }
}
