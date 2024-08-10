using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Common;

/// <summary>
/// JToken解析版
/// </summary>
public class Fn2ResultNewtJsonConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Fn2Result<>);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var j = JToken.Load(reader) as JObject;
        if (j == null) return null;

        var r = (existingValue ?? Activator.CreateInstance(objectType)) as Fn2Result;
        var isok = (bool?)null;
        foreach (var p in j.Properties())
        {
            var propertyName = p.Name;
            switch (1)
            {
                case 1 when (isok == null && string.Equals(propertyName, "isok", StringComparison.OrdinalIgnoreCase)):
                case 1 when (isok == null && string.Equals(propertyName, "succeed", StringComparison.OrdinalIgnoreCase)):
                case 1 when (isok == null && string.Equals(propertyName, "success", StringComparison.OrdinalIgnoreCase)):
                    {
                        isok = (bool?)p.Value == true;
                        r.SetIsSucceeded(isok.Value);
                    }
                    break;
                case 1 when (string.Equals(propertyName, "status", StringComparison.OrdinalIgnoreCase)):
                    {
                        var c = (long?)p.Value ?? 0;
                        r.Code = c;
                        if (isok == null && (c == 200)) r.SetIsSucceeded(true);
                    }
                    break;
                case 1 when (string.Equals(propertyName, "code", StringComparison.OrdinalIgnoreCase)):
                    {
                        var c = (long?)p.Value ?? 0;
                        r.Code = c;
                        if (isok == null && (c == 200 || c == 0)) r.SetIsSucceeded(true);
                    }
                    break;
                case 1 when (isok != true && string.Equals(propertyName, "errcode", StringComparison.OrdinalIgnoreCase)):
                case 1 when (isok != true && string.Equals(propertyName, "errno", StringComparison.OrdinalIgnoreCase)):
                    {
                        var c = (long?)p.Value ?? 0;
                        r.Code = c;
                        if (isok == null && (c == 0)) r.SetIsSucceeded(true);
                    }
                    break;
                case 1 when (string.Equals(propertyName, "msg", StringComparison.OrdinalIgnoreCase)):
                case 1 when (string.Equals(propertyName, "errormsg", StringComparison.OrdinalIgnoreCase)):
                case 1 when (string.Equals(propertyName, "errmsg", StringComparison.OrdinalIgnoreCase)):
                case 1 when (string.Equals(propertyName, "errormessage", StringComparison.OrdinalIgnoreCase)):
                    {
                        r.Msg = (string)p.Value;
                    }
                    break;
                case 1 when (string.Equals(propertyName, "data", StringComparison.CurrentCultureIgnoreCase)):
                    {
                        var d = p.Value.ToObject(objectType.GetGenericArguments()[0], serializer);
                        r.SetData(d);
                    }
                    break;
            }
        }

        return r;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        var contractResolver = serializer.ContractResolver as DefaultContractResolver;

        writer.WriteStartObject();
        {
            var r = (Fn2Result)value;
            if (r.IsSucceeded())
            {
                writer.WritePropertyName(GetPropertyName(contractResolver, "succeed"));
                writer.WriteValue(r.IsSucceeded());
                writer.WritePropertyName(GetPropertyName(contractResolver, "code"));
                writer.WriteValue(r.Code);
            }
            else
            {
                //writer.WritePropertyName(GetPropertyName(contractResolver, "succeed"));
                //writer.WriteValue(r.IsSucceeded());
                writer.WritePropertyName(GetPropertyName(contractResolver, "code"));
                writer.WriteValue(r.Code);
            }
            writer.WritePropertyName(GetPropertyName(contractResolver, "msg"));
            writer.WriteValue(r.Msg);
            if (r.IsSucceeded() || r.GetData() != null)
            {
                writer.WritePropertyName(GetPropertyName(contractResolver, "data"));
                serializer.Serialize(writer, r.GetData());
            }
            if (!r.IsSucceeded() && !string.IsNullOrEmpty(r.StackTrace))
            {
#if DEBUG
                writer.WritePropertyName(GetPropertyName(contractResolver, "stackTrace"));
                writer.WriteValue(r.StackTrace);
#endif
            }
        }
        writer.WriteEndObject();
    }

    static string GetPropertyName(DefaultContractResolver contractResolver, string propertyName)
    {
        return contractResolver?.GetResolvedPropertyName(propertyName) ?? propertyName;
    }
}
