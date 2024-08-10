using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public static class HttpClientExtension
    {
        public static HttpHeaders Set(this HttpHeaders headers, string name, params string[] values)
        {
            headers.Remove(name);
            headers.TryAddWithoutValidation(name, values);
            return headers;
        }

        public static HttpRequestMessage SetHttpHeader(this HttpRequestMessage req, string name, params string[] values)
        {
            Set(req.Headers, name, values);
            return req;
        }

        public static HttpRequestMessage SetContent(this HttpRequestMessage req, HttpContent content)
        {
            req.Content = content;
            return req;
        }

        public static async Task<HttpResponseMessage> SendAndEnsureSuccessAsync(this HttpClient http, HttpRequestMessage req, CancellationToken cancellation = default)
        {
            var res = await http.SendAsync(req, cancellation).ConfigureAwait(false);
            return res.EnsureSuccessStatusCode();
        }

        public static CookieCollection GetResCookies(this HttpResponseMessage res, CookieContainer cookieContainer = null)
        {
            cookieContainer ??= new CookieContainer();
            ProcessReceivedCookies(res, cookieContainer);
            return cookieContainer.GetCookies(res.RequestMessage.RequestUri);
        }

        // see https://github.com/dotnet/runtime/blob/01b7e73cd378145264a7cb7a09365b41ed42b240/src/libraries/System.Net.Http/src/System/Net/Http/SocketsHttpHandler/CookieHelper.cs
        static void ProcessReceivedCookies(HttpResponseMessage response, CookieContainer cookieContainer)
        {
            if (response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string> values))
            {
                // The header values are always a string[]
                var valuesArray = (string[])values;
                Debug.Assert(valuesArray.Length > 0, "No values for header??");
                Debug.Assert(response.RequestMessage != null && response.RequestMessage.RequestUri != null);

                Uri requestUri = response.RequestMessage.RequestUri;
                for (int i = 0; i < valuesArray.Length; i++)
                {
                    cookieContainer.SetCookies(requestUri, valuesArray[i]);
                }
            }
        }


        public static HttpRequestMessage SetHttpHeaderEx(this HttpRequestMessage req, string name, params string[] values)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (IsContentHeader(name))
            {
                if (req.Content != null) req.Content.Headers.Remove(name);
                else throw new NullReferenceException("req.Content is null when set content header.");
                if (values != null) req.Content.Headers.TryAddWithoutValidation(name, values);
            }
            else
            {
                req.Headers.Remove(name);
                if (values != null) req.Headers.TryAddWithoutValidation(name, values);
            }
            return req;
        }

        static bool IsContentHeader(string name)
        {
            switch (name)
            {
                // https://docs.microsoft.com/en-us/dotnet/api/system.net.http.headers.httpcontentheaders
                case string _ when (string.Equals(name, "Allow", StringComparison.OrdinalIgnoreCase)):
                case string _ when (string.Equals(name, "Content-Disposition", StringComparison.OrdinalIgnoreCase)):
                case string _ when (string.Equals(name, "Content-Encoding", StringComparison.OrdinalIgnoreCase)):
                case string _ when (string.Equals(name, "Content-Canguage", StringComparison.OrdinalIgnoreCase)):
                case string _ when (string.Equals(name, "Content-Length", StringComparison.OrdinalIgnoreCase)):
                case string _ when (string.Equals(name, "Content-Location", StringComparison.OrdinalIgnoreCase)):
                case string _ when (string.Equals(name, "Content-MD5", StringComparison.OrdinalIgnoreCase)):
                case string _ when (string.Equals(name, "Content-Range", StringComparison.OrdinalIgnoreCase)):
                case string _ when (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase)):
                case string _ when (string.Equals(name, "Expires", StringComparison.OrdinalIgnoreCase)):
                case string _ when (string.Equals(name, "Last-Modified", StringComparison.OrdinalIgnoreCase)):
                    return true;

                default:
                    return false;
            }
        }

        public static HttpRequestMessage DelHttpHeaderEx(this HttpRequestMessage req, string name)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!IsContentHeader(name)) req.Headers.Remove(name);
            else req.Content?.Headers?.Remove(name);
            return req;
        }
    }
}