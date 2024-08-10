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
}
