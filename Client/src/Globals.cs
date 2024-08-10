using Common;
using Common.JsonNet.v2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Linq;

namespace TestNS;

public static partial class Globals
{
    public static Args Args;
    public static IServiceProvider ServiceProvider;

    public static string Exnv(string str) => Environment.ExpandEnvironmentVariables(str);

    public static void Throw(string error)
    {
        Debug.WriteLine($"[error]: {error}");
        throw new Exception(error);
    }



    public static T LoadFileJson<T>(string filepath, T defaultValue = default)
    {
        try
        {
            var json = File.ReadAllText(filepath);
            var v = JsonNetUtils.JsonStrTo<T>(json);
            return EqualityComparer<T>.Default.Equals(v, default) ? defaultValue : v;
        }
        catch { return defaultValue; }
    }

    public static void SaveFileJson(string filepath, object obj, bool ignoreNull = true)
    {
        var json = JsonNetUtils.ToJsonStr(obj, true, ignoreNull);
        File.WriteAllText(filepath, json);
    }

    //public static T IsTo<T>(this object obj)
    //{
    //    if (obj is T v) return v;
    //    throw new InvalidCastException($"obj type is not '{typeof(T).FullName}'");
    //}

    //public static T IsTo<T>(this object obj, T or)
    //{
    //    if (obj is T v) return v;
    //    return or;
    //}

    public static IEnumerable<string> GetUserAgentLs()
    {
        yield return "Mozilla/5.0 (Linux; Android 12; M2011K2C Build/SKQ1.211006.001; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/102.0.5005.99 Mobile Safari/537.36 T7/12.16 SearchCraft/3.9.1 (Baidu; P1 11)";
        yield return "Mozilla/5.0 (Linux; Android 6.0; Nexus 5 Build/MRA58N) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Mobile Safari/537.36";
        yield return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";
        yield return "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.110 Safari/537.36 Edge/18.18362";
        yield return "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:88.0) Gecko/20100101 Firefox/109.0";
    }
}
