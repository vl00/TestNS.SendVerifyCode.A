using Common.JsonNet.v2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public delegate void LogHttpApiInvocationOnDebugFunc(string apiDesc, string msg, object logParams);
    public delegate void LogHttpApiInvocationOnErrorFunc(string apiDesc, Exception ex, int errcode, string errmsg, object logParams);

    /// <summary>
    /// 调用http api, 并在出错或返回不成功时记录日志
    /// </summary>
    public partial class HttpApiInvocation
    {
        bool _allowSetBodyOnGet;
        Action<HttpRequestEntity, HttpRequestMessage> _onBeforeRequest;
        Func<HttpResponseMessage, Task<object>> _onAfterResponse;
        HttpRequestEntity requestEntity;
        LogHttpApiInvocationOnErrorFunc _logOnError;
        LogHttpApiInvocationOnDebugFunc _logOnDebug;

        /// <inheritdoc cref="HttpApiInvocation"/>
        [DebuggerStepThrough]
        public HttpApiInvocation() { }

        [DebuggerStepThrough]
        public HttpApiInvocation SetLogOnError(LogHttpApiInvocationOnErrorFunc func)
        {
            this._logOnError = func;
            return this;
        }

        [DebuggerStepThrough]
        public HttpApiInvocation SetLogOnDebug(LogHttpApiInvocationOnDebugFunc func)
        {
            this._logOnDebug = func;
            return this;
        }

        class ReqRes
        {
            public string address { get; set; }
            public string req_type { get; set; }
            public object Req { get; set; }
            public string res_type { get; set; }
            public object Res { get; set; } // raw

            [IgnoreDataMember]
            public Exception Err { get; set; }
        }

        [DebuggerStepThrough]
        public HttpApiInvocation SetAllowBodyOnGet(bool allowSetBodyOnGet)
        {
            this._allowSetBodyOnGet = allowSetBodyOnGet;
            return this;
        }

        [DebuggerStepThrough]
        public HttpApiInvocation SetRequestEntity(HttpRequestEntity requestEntity)
        {
            this.requestEntity = requestEntity;
            return this;
        }

        [DebuggerStepThrough]
        public HttpApiInvocation OnBeforeRequest(Action<HttpRequestMessage> onBeforeRequest)
        {
            this._onBeforeRequest = (_, req) => onBeforeRequest(req);
            return this;
        }

        /// <summary>
        /// SetHeader和SetBody在此方法之前
        /// </summary>
        [DebuggerStepThrough]
        public HttpApiInvocation OnBeforeRequest(Action<HttpRequestEntity, HttpRequestMessage> onBeforeRequest)
        {
            this._onBeforeRequest = onBeforeRequest;
            return this;
        }

        /// <summary>
        /// 'onAfterResponse'返回类型要跟'$.InvokeByAsync()'返回类型一样
        /// <code><![CDATA[
        /// .OnAfterResponse(async res => 
        /// {
        ///     var str = await res.Content.ReadAsStringAsync();
        ///     ...
        ///     return Fn2Result.Success<T>(...);
        /// })
        /// .InvokeByAsync<T>(http);
        /// ]]></code>
        /// </summary>
        [DebuggerStepThrough]
        public HttpApiInvocation OnAfterResponse(Func<HttpResponseMessage, Task<object>> onAfterResponse)
        {
            this._onAfterResponse = onAfterResponse;
            return this;
        }

        /// <summary>
        /// 'onAfterResponse'返回类型要跟'$.InvokeByAsync()'返回类型一样
        /// <code><![CDATA[
        /// .OnAfterResponse(async (res, bodyStr) => 
        /// {
        ///     var json = JToken.Parse(bodyStr);
        ///     ...
        ///     return Fn2Result.Success<T>(...);
        /// })
        /// .InvokeByAsync<T>(http);
        /// ]]></code>
        /// </summary>
        [DebuggerStepThrough]
        public HttpApiInvocation OnAfterResponse(Func<HttpResponseMessage, string, Task<object>> onAfterResponse)
        {
            this._onAfterResponse = async (res) => 
            {
                var bodyStr = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                return await onAfterResponse(res, bodyStr);
            };
            return this;
        }

        public Task InvokeIgnoreResByAsync(HttpClient http, CancellationToken cancellation = default)
        {
            var obj = new ReqRes();
            var req = new HttpRequestMessage(new HttpMethod(requestEntity.Method), requestEntity.Url);
            return InvokeByAndGetRespone(http, req, obj, requestEntity, cancellation);
        }

        static async Task<object> OnLog_GetContent(object ctn, MediaTypeHeaderValue contentType = null)
        {
            if (ctn == null || ctn == Task.CompletedTask)
            {
                return null;
            }
            switch (ctn)
            {
                case Stream stream:
                    return $"(stream len={(stream.CanSeek ? $"{stream.Length}" : "(can't seek)")})";

                // and more ...

                case HttpContent c:
                    {
                        string str = null;
                        try
                        {
                            str = await c.ReadAsStringAsync().ConfigureAwait(false);
                            contentType ??= c.Headers.ContentType;
                            return contentType?.MediaType == "application/json" || contentType?.MediaType == "text/json" || contentType?.MediaType == "application/problem+json"
                                ? (object)JToken.Parse(str) : str;
                        }
                        catch
                        {
                            if (str == null) return $"({c.GetType().FullName})";
                            return str.Length <= 100 ? str : $"{str[..100]} ...(and more {(str.Length - 100)})";
                        }
                    }
            }
            return ctn;
        }

        private async Task<HttpResponseMessage> InvokeByAndGetRespone(HttpClient http, HttpRequestMessage req, ReqRes obj, HttpRequestEntity requestEntity, CancellationToken cancellation)
        {
            obj.address = $"{requestEntity.Method} {requestEntity.Url}";
            obj.req_type = requestEntity.ContentType;
            obj.Res = Task.CompletedTask;
            if (_allowSetBodyOnGet || req.Method != HttpMethod.Get) SetHttpBody(req, requestEntity);
            if (requestEntity.Headers?.Count > 0)
            {
                foreach (var (k, v) in requestEntity.Headers)
                {
                    req.SetHttpHeaderEx(k, v);
                }
            }
            if (_onBeforeRequest != null)
            {
                _onBeforeRequest(requestEntity, req);
            }
            obj.Req ??= requestEntity.Body;
            // 请求ing
            HttpResponseMessage res = null;
            try
            {
                res = await http.SendAsync(req, cancellation).ConfigureAwait(false);
                res.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                obj.Err = ex;
            }
            obj.res_type = res?.Content?.Headers?.ContentType?.MediaType;
            if (obj.Err != null && _logOnError != null)
            {
                var (ecode, emsg) = (0, default(string));
                if (cancellation.IsCancellationRequested)
                {
                    ecode = -4;
                    emsg = "http_request_cancelled";
                }
                else if (res != null && !res.IsSuccessStatusCode)
                {
                    ecode = (int)res.StatusCode;
                    emsg = "http_response_error";
                }
                else if (obj.Err != null)
                {
                    ecode = -5;
                    emsg = "send_internal_error";
                }
                if (emsg != null)
                {
                    obj.Res = await OnLog_GetContent(res?.Content);
                    obj.Req = await OnLog_GetContent(obj.Req, req.Content?.Headers?.ContentType);
                    _logOnError(requestEntity.ApiDesc, obj.Err, ecode, emsg, obj);
                }
            }
            return res;
        }

        public async Task<Fn2Result<T>> InvokeByAsync<T>(HttpClient http, CancellationToken cancellation = default)
        {
            var obj = new ReqRes();
            var req = new HttpRequestMessage(new HttpMethod(requestEntity.Method), requestEntity.Url);
            var res = await InvokeByAndGetRespone(http, req, obj, requestEntity, cancellation);
            if (obj.Err != null || res?.IsSuccessStatusCode != true)
            {
                return Fn2Result.Fail<T>(cancellation.IsCancellationRequested ? "http request is cancelled." : ($"{(obj.Err?.Message ?? "")}{(obj.Res == null ? "" : $"\n{obj.Res}")}"),
                    (res != null ? (int)res.StatusCode : 500));
            }
            Fn2Result<T> r = default;
            try
            {
                obj.Res = res.Content;
                if (_onAfterResponse != null)
                {
                    r = (Fn2Result<T>)(await _onAfterResponse(res).ConfigureAwait(false));
                }
                else r = Fn2Result.OK<T>(default, 200);
            }
            catch (Exception ex)
            {
                if (_logOnError != null)
                {
                    obj.Req = await OnLog_GetContent(obj.Req, req.Content?.Headers?.ContentType);
                    obj.Res = await OnLog_GetContent(obj.Res, res.Content?.Headers?.ContentType);
                    _logOnError(requestEntity.ApiDesc, ex, -2, $"返回格式有问题", obj);
                }
                return Fn2Result.Fail<T>(ex.Message);
            }
            if (r?.IsSucceeded() != true && _logOnError != null)
            {
                obj.Res = await OnLog_GetContent(obj.Res, res.Content?.Headers?.ContentType);
                obj.Req = await OnLog_GetContent(obj.Req, req.Content?.Headers?.ContentType);
                _logOnError(requestEntity.ApiDesc, new Exception(r?.Msg), -3, $"返回api结果不成功", obj);
            }
			if (r?.IsSucceeded() == true && _logOnDebug != null)
            {
                obj.Res = await OnLog_GetContent(obj.Res, res.Content?.Headers?.ContentType);
                obj.Req = await OnLog_GetContent(obj.Req, req.Content?.Headers?.ContentType);                
                _logOnDebug(requestEntity.ApiDesc, "返回api结果成功", obj);
            }
            return r;
        }

        static void SetHttpBody(HttpRequestMessage req, HttpRequestEntity requestEntity)
        {
            var contentType = requestEntity.ContentType;
            switch (contentType)
            {
                // form
                case "application/x-www-form-urlencoded":
                    switch (requestEntity.Body)
                    {
                        case null:
                            break;
                        case string str:
                            req.Content = new StringContent(str, null, contentType);
                            break;
                        case IDictionary<string, string> d:
                            req.Content = new FormUrlEncodedContent(d);
                            break;
                        case FormUrlEncodedContent fuec:
                            req.Content = fuec;
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                    break;

                // json
                case "text/json":
                case "application/json":
                case "application/problem+json": // swagger
                case string _ when (contentType.StartsWith("text/json")):
                case string _ when (contentType.StartsWith("application/json")):
                    switch (requestEntity.Body)
                    {
                        case null:
                            req.Content = new StringContent(string.Empty, null, contentType);
                            break;
                        case string str:
                            req.Content = new StringContent(str, null, contentType);
                            break;
                        case HttpContent httpContent:
                            req.Content = httpContent;
                            break;
                        default:
                            req.Content = new StringContent(requestEntity.Body.ToJsonStr(true), null, contentType);
                            break;
                    }
                    break;

                default:
                    switch (requestEntity.Body)
                    {
                        case null:
                            req.Content = null;
                            break;
                        case string str:
                            req.Content = new StringContent(str, null, contentType);
                            break;
                        case Memory<byte> m:
                            req.Content = new ReadOnlyMemoryContent(m);
                            break;
                        case ReadOnlyMemory<byte> m2:
                            req.Content = new ReadOnlyMemoryContent(m2);
                            break;
                        case ArraySegment<byte> a:
                            req.Content = new ByteArrayContent(a.Array, a.Offset, a.Count);
                            break;
                        case byte[] bys:
                            req.Content = new ByteArrayContent(bys);
                            break;
                        case Stream stream:
                            req.Content = new StreamContent(stream);
                            break;
                        case HttpContent httpContent:
                            req.Content = httpContent;
                            break;
                        // and more ...
                        default:
                            throw new NotSupportedException();
                    }
                    break;
            }
        }

        public async Task InvokeIgnoreResByAsync(IHttpClientFactory httpClientFactory, string name = null, CancellationToken cancellation = default)
        {
            using var http = name != null ? httpClientFactory.CreateClient(name) : httpClientFactory.CreateClient();
            await InvokeIgnoreResByAsync(http, cancellation);
        }

        public async Task<Fn2Result<T>> InvokeByAsync<T>(IHttpClientFactory httpClientFactory, string name = null, CancellationToken cancellation = default)
        {
            using var http = name != null ? httpClientFactory.CreateClient(name) : httpClientFactory.CreateClient();
            return await InvokeByAsync<T>(http, cancellation);
        }

        [DebuggerStepThrough]
        public HttpApiInvocation Reset(bool includeLogOn = false)
        {
            _allowSetBodyOnGet = false;
            requestEntity = null;
            if (includeLogOn)
            {
                _logOnDebug = null;
                _logOnError = null;
            }
            _onBeforeRequest = default;
            _onAfterResponse = default;
            return this;
        }
    }
}
