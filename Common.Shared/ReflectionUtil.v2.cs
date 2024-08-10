using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Common;

public static class ReflectionUtil
{
    public static Type TryGetType(string assemblyQualifiedName)
    {
        return Type.GetType(assemblyQualifiedName, false);
    }

    public static object GetFieldValue(this object obj, string fieldName) => GetFieldValue(obj, obj.GetType(), fieldName);

    public static object GetFieldValue(Type type, string fieldName) => GetFieldValue(null, type, fieldName);

    public static object GetFieldValue(object obj, Type type, string fieldName)
        => type.GetRuntimeFields().Single(f => f.Name == fieldName).GetValue(obj);

    public static object GetPropertyValue(this object obj, string propertyName) => GetPropertyValue(obj, obj.GetType(), propertyName);

    public static object GetPropertyValue(Type type, string propertyName) => GetPropertyValue(null, type, propertyName);

    public static object GetPropertyValue(object obj, Type type, string propertyName)
        => type.GetRuntimeProperties().Single(f => f.Name == propertyName).GetValue(obj);

    public static object GetValue(this object obj, string propertyOrFieldName) => GetValue(obj, obj.GetType(), propertyOrFieldName);

    public static object GetValue(Type type, string propertyOrFieldName) => GetValue(null, type, propertyOrFieldName);

    public static object GetValue(object obj, Type type, string propertyOrFieldName)
    {
        var pi = type.GetRuntimeProperties().SingleOrDefault(f => f.Name == propertyOrFieldName);
        if (pi != null) return pi.GetValue(obj);

        var fi = type.GetRuntimeFields().SingleOrDefault(f => f.Name == propertyOrFieldName);
        if (fi != null) return fi.GetValue(obj);

        return null;
    }

    public static object SetValue(this object obj, string propertyOrFieldName, object value)
    {
        SetValue(obj, obj.GetType(), propertyOrFieldName, value);
        return obj;
    }

    public static void SetValue(Type type, string propertyOrFieldName, object value) => SetValue(null, type, propertyOrFieldName, value);

    public static void SetValue(object obj, Type type, string propertyOrFieldName, object value)
    {
        var pi = type.GetRuntimeProperties().SingleOrDefault(f => f.Name == propertyOrFieldName);
        if (pi != null)
        {
            pi.SetValue(obj, value);
            return;
        }

        var fi = type.GetRuntimeFields().SingleOrDefault(f => f.Name == propertyOrFieldName);
        if (fi != null)
        {
            fi.SetValue(obj, value);
            return;
        }            
    }

    public static IEnumerable<MethodInfo> GetMethods(this object obj, string name) => GetMethods(obj.GetType(), name);
    public static IEnumerable<MethodInfo> GetMethods(Type type, string name) => type.GetRuntimeMethods().Where(_mi => _mi.Name == name);

    public static object InvokeMethod(this object obj, string name, Func<MethodInfo, bool> filter, params object[] parms)
        => GetMethods(obj, name).Where(_mi => !_mi.IsStatic).Single(_mi => filter?.Invoke(_mi) != false).Invoke(obj, parms);

    public static object InvokeMethod(Type type, string name, Func<MethodInfo, bool> filter, params object[] parms)
        => GetMethods(type, name).Where(_mi => _mi.IsStatic).Single(_mi => filter?.Invoke(_mi) != false).Invoke(null, parms);

    public static object InvokeMethod(object obj, Type type, string name, Func<MethodInfo, bool> filter, params object[] parms)
    {
        return GetMethods(type, name).Single(_mi => filter?.Invoke(_mi) != false).Invoke(obj, parms);
    }

    public static object InvokeMethod(this object obj, string name, params object[] parms) => InvokeMethod(obj, name, null, parms);
    public static object InvokeMethod(Type type, string name, params object[] parms) => InvokeMethod(type, name, null, parms);
    public static object InvokeMethod(object obj, Type type, string name, params object[] parms) => InvokeMethod(obj, type, name, null, parms);
}
