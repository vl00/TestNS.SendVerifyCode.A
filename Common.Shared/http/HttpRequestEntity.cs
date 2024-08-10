using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;

namespace Common;

[DebuggerStepThrough]
public class HttpRequestEntity
{
    public string ApiDesc { get; set; }

    public string Url { get; set; }
    public string Method { get; set; }
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary><code><![CDATA[
    /// not null body type is :
    ///   form: string | IDictionary<string, string> | FormUrlEncodedContent
    ///   json: HttpContent | string | object
    ///   others: HttpContent | string | byte[] | ArraySegment<byte> | Memory<byte> | Stream | ReadOnlyMemory<byte>
    /// ]]></code></summary>
    public object Body { get; private set; }

    [IgnoreDataMember]
    public string ContentType 
    {
        get => Headers != null && Headers.TryGetValue("Content-Type", out var s) ? s : null;
        set => SetHeaderWithNotNull("Content-Type", value);
    }

    public HttpRequestEntity SetMethod(HttpMethod method)
    {
        this.Method = method.Method;
        return this;
    }

    public HttpRequestEntity SetUrl(string url)
    {
        this.Url = url;
        return this;
    }

    public HttpRequestEntity SetAddress(HttpMethod method, string url) 
    {
        this.Method = method.Method;
        this.Url = url;
        return this;
    }

    public HttpRequestEntity SetApiDesc(string apiDesc)
    {
        this.ApiDesc = apiDesc;
        return this;
    }

    public HttpRequestEntity SetHeader(string name, string value)
    {
        this.Headers[name] = value;
        return this;
    }

    public HttpRequestEntity SetHeaders(IDictionary<string, string> headers)
    {
        if (this.Headers != null)
        {
            foreach (var (k, v) in headers)
                this.Headers[k] = v;
        }
        return this;
    }

    /// <inheritdoc cref="Body"/>
    public HttpRequestEntity SetBody(object body, string contentType, string contentEncoding = null)
    {
        ContentType = contentType;
        if (!string.IsNullOrEmpty(contentEncoding))
        {
            Headers["Content-Encoding"] = contentEncoding; 
        }
        Body = body;
        return this;
    }

    void SetHeaderWithNotNull(string name, string value, string defaultValue = null)
    {
        if (string.IsNullOrEmpty(name)) return;
        value = string.IsNullOrEmpty(value) ? defaultValue : value;
        if (string.IsNullOrEmpty(value)) Headers.Remove(name);
        else Headers[name] = value;
    }

    /// <inheritdoc cref="Body"/>
    public HttpRequestEntity SetBody0(object obj)
    {
        Body = obj;
        return this;
    }

    public HttpRequestEntity SetBodyByJsonStr(string jsonStr)
    {
        return SetBody(jsonStr, "application/json");
    }

    public HttpRequestEntity SetBodyByForm(IDictionary<string, string> form)
    {
        form ??= new Dictionary<string, string>();
        return SetBody(form, "application/x-www-form-urlencoded");
    }

    public HttpRequestEntity SetBodyByFormStr(string formStr)
    {
        return SetBody(formStr, "application/x-www-form-urlencoded");
    }

    public HttpRequestEntity SetEmptyBody(string contentType = null)
    {
        return SetBody(string.Empty, contentType);
    }

    public HttpRequestEntity SetOn(Action<HttpRequestEntity> action)
    {
        action?.Invoke(this);
        return this;
    }
}
