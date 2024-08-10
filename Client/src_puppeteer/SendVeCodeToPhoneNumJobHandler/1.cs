using Common;
using Common.JsonNet.v2;
using Common.SimpleJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TestNs.PuppeteerSharp.Jobs;

public partial class SendVeCodeToPhoneNumJobHandler(IHttpClientFactory httpClientFactory,
    //AppSettings _settings,
    //BrowserService _browserService,
    IServiceProvider serviceProvider)
    : BaseJobHandler2<SendVeCodeToPhoneNumJob>
{
    static IHttpClientFactory _httpClientFactory;
    static IServiceProvider _serviceProvider;

    protected override async Task<JobResult> OnHandleAsync(SendVeCodeToPhoneNumJob job, CancellationToken cancellation)
    {
        _httpClientFactory = httpClientFactory;
        _serviceProvider = serviceProvider;
        Exception err = null;
        await default(ValueTask);

        var (name, func) = GetFunc(job.Svc);
        if (name == null)
        {
            return new() { Success = false, Msg = $"not found svc={job.Svc}" };
        }
        (_, err) = await func()(job, JobContext, cancellation).AwaitResOrErr();
        if (err != null)
        {
            Log.LogError(err, "{name} failed", name);
            return new() { Success = false, Msg = err.Message };
        }
        else
        {
            Log.LogInfo("{name} ok", name);
            return new() { Success = true };
        }
    }

    [DebuggerStepThrough]
    internal static void ReturnThrowError(Exception ex) => throw ex;

    [DebuggerStepThrough]
    internal static void ReturnThrowError(string err) => throw new Exception(err);

    #region create Js Func string

    /// <summary>
    /// <code>(...)=>{ ...body }</code>
    /// </summary>
    public static string JsFuncStr(string args, string body)
    {
        throw new Exception("不会自己写吗？？？");
    }

    /// <summary>
    /// <code>async(...)=>{ await ...body }</code>
    /// </summary>
    public static string JsAsyncFuncStr(string args, string body)
    {
        throw new Exception("不会自己写吗？？？");
    }

    /// <inheritdoc cref="JsFuncStr"/>
    public static string JsFuncStr(string body) => JsFuncStr(string.Empty, body);
    /// <inheritdoc cref="JsAsyncFuncStr"/>
    public static string JsAsyncFuncStr(string body) => JsAsyncFuncStr(string.Empty, body);
    #endregion create Js Func string
	
	static HttpApiInvocation GetHttpApiInvocation()
	{
		var hapi = new HttpApiInvocation(); //*/
		hapi.SetLogOnError((string apiDesc, Exception ex, int errcode, string errmsg, object logParams) => 
		{
			var log = _serviceProvider.GetService<ILoggerFactory>().CreateLogger<SendVeCodeToPhoneNumJobHandler>();
			log.LogError(ex, "{apiDesc} errcode={errcode}, errmsg={errmsg}.", apiDesc, errcode, errmsg);
		}); //*/
		return hapi;
	}
	
  

    internal static async Task<string> GetWordsByd(string b64)
    {
        var r = await new HttpApiInvocation()
            .SetRequestEntity(
                new HttpRequestEntity().SetApiDesc("call d")
                .SetAddress(HttpMethod.Post, "http://localhost:9898/ocr/b64/json")
                .SetBody0(new StringContent(b64))
            )
            .OnAfterResponse(async (res, str) =>
            {
                if (!res.IsSuccessStatusCode) return Fn2Result.Fail<string>(str, (long)res.StatusCode);
                if (string.IsNullOrEmpty(str)) return Fn2Result.Fail<string>("result is empty string", 400);

                var json = JToken.Parse(str);
                if ((int)json["status"] != 200) return Fn2Result.Fail<string>((int)json["status"]);
                if (!string.IsNullOrEmpty((string)json["msg"])) return Fn2Result.Fail<string>((string)json["msg"], 400);
                str = (string)json["result"];

                await default(ValueTask);
                return Fn2Result.OK(str);
            })
            .InvokeByAsync<string>(_httpClientFactory, "");

        r.ThrowIfResultIsFailed();

        return r.Data;
    }

    internal static async Task<JObject> GetSlideMatchByd(byte[] hk, byte[] bg, int simple_hk)
    {
        var r = await new HttpApiInvocation()
            .SetRequestEntity(
                new HttpRequestEntity().SetApiDesc("call d")
                .SetAddress(HttpMethod.Post, "http://localhost:9898/slide/match/file/json?simple_target={simple_hk}")
                .SetBody0(new MultipartFormDataContent
                {
                    { new ByteArrayContent(hk), "target_img", "target_img" },
                    { new ByteArrayContent(bg), "bg_img", "bg_img" },
                })
            )
            .OnAfterResponse(async (res, str) =>
            {
                if (!res.IsSuccessStatusCode) return Fn2Result.Fail<JObject>(str, (long)res.StatusCode);
                if (string.IsNullOrEmpty(str)) return Fn2Result.Fail<JObject>("result is empty string", 400);

                var json = JObject.Parse(str);
                if ((int)json["status"] != 200) return Fn2Result.Fail<JObject>((int)json["status"]);
                if (!string.IsNullOrEmpty((string)json["msg"])) return Fn2Result.Fail<JObject>((string)json["msg"], 400);                

                await default(ValueTask);
                return Fn2Result.OK(json["result"] as JObject);
            })
            .InvokeByAsync<JObject>(_httpClientFactory, "");

        r.ThrowIfResultIsFailed();

        return r.Data;
    }


    internal static async Task<JArray> GetWeiZhiByTe(string b64, int? c1 = null, int? c2 = null)
    {
        var r = await new HttpApiInvocation()
            .SetRequestEntity(
                new HttpRequestEntity().SetApiDesc("call T")
                .SetAddress(HttpMethod.Post, "http://localhost:8003/clickOn")
                .SetBodyByJsonStr(new 
                {
                    dataType = 2, 
                    imageSource = b64, 
                    imageID = Guid.NewGuid().ToString("n"),
					method = "jy_click2",
					args = new { chars_idx1 = c1, chars_idx2 = c2 }
                }.ToJsonStr())
            )
            .OnAfterResponse(async (res, str) =>
            {
                if (!res.IsSuccessStatusCode) return Fn2Result.Fail<JArray>(str, (long)res.StatusCode);
                if (string.IsNullOrEmpty(str)) return Fn2Result.Fail<JArray>("result is empty string", 400);

                var json = JToken.Parse(str);
                if ((int)json["code"] != 200) return Fn2Result.Fail<JArray>((string)json["msg"], (int)json["code"]);
                var arr = (JArray)json["data"]["res"];

                await default(ValueTask);
                return Fn2Result.OK(arr);
            })
            .InvokeByAsync<JArray>(_httpClientFactory, "");

        r.ThrowIfResultIsFailed();

        return r.Data;
    }
}
