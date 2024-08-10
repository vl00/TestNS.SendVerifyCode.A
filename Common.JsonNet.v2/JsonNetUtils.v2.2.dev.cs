using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace Common.JsonNet.v2
{
    public static partial class JsonNetUtils
    {
        /// <summary>add|remove|raise event-handler by yourself</summary>
        public static Action<JsonSerializerSettings> ConfigDefaultJsonSettings;

        [Obsolete("use 'ConfigDefaultJsonSettings' instead")]
        public static void ConfigToDefaultJsonSettings(Action<JsonSerializerSettings> func)
        {
            ConfigDefaultJsonSettings += func;
        }

        static JsonSerializerSettings DefaultJsonSettings(bool camelCase)
        {
            var options = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
                TypeNameHandling = TypeNameHandling.None,
                ConstructorHandling = ConstructorHandling.Default
            };
            if (camelCase) options.ContractResolver = new CamelCasePropertyNamesContractResolver();
            return options;
        }

        public static JsonSerializerSettings GetJsonSettings(bool camelCase = false)
        {
            var options = DefaultJsonSettings(camelCase);            
            ConfigDefaultJsonSettings?.Invoke(options);
            return options;
        }

        public static JsonSerializerSettings CopyTo(this JsonSerializerSettings src, JsonSerializerSettings dest)
        {
            if (dest == null) throw new ArgumentNullException(nameof(dest));
            dest.CheckAdditionalContent = src.CheckAdditionalContent;
            dest.ConstructorHandling = src.ConstructorHandling;
            dest.Context = src.Context;
            dest.ContractResolver = src.ContractResolver;
            dest.Converters = src.Converters;
            dest.Culture = src.Culture;
            dest.DateFormatHandling = src.DateFormatHandling;
            dest.DateFormatString = src.DateFormatString;
            dest.DateParseHandling = src.DateParseHandling;
            dest.DateTimeZoneHandling = src.DateTimeZoneHandling;
            dest.DefaultValueHandling = src.DefaultValueHandling;
            dest.EqualityComparer = src.EqualityComparer;
            dest.Error = src.Error;
            dest.FloatFormatHandling = src.FloatFormatHandling;
            dest.FloatParseHandling = src.FloatParseHandling;
            dest.Formatting = src.Formatting;
            dest.MaxDepth = src.MaxDepth;
            dest.MetadataPropertyHandling = src.MetadataPropertyHandling;
            dest.MissingMemberHandling = src.MissingMemberHandling;
            dest.NullValueHandling = src.NullValueHandling;
            dest.ObjectCreationHandling = src.ObjectCreationHandling;
            dest.PreserveReferencesHandling = src.PreserveReferencesHandling;
            dest.ReferenceLoopHandling = src.ReferenceLoopHandling;
            //dest.ReferenceResolver = src.ReferenceResolver; // [Obsolete]
            dest.ReferenceResolverProvider = src.ReferenceResolverProvider;
            //dest.Binder = src.Binder; // [Obsolete]
            dest.SerializationBinder = src.SerializationBinder;
            dest.StringEscapeHandling = src.StringEscapeHandling;
            dest.TraceWriter = src.TraceWriter;
            //dest.TypeNameAssemblyFormat = src.TypeNameAssemblyFormat; // [Obsolete]
            dest.TypeNameAssemblyFormatHandling = src.TypeNameAssemblyFormatHandling;
            dest.TypeNameHandling = src.TypeNameHandling;
            return dest;
        }

        public static JsonSerializerSettings AddConverter(this JsonSerializerSettings src, JsonConverter converter)
        {
            if (converter != null)
            {
                src.Converters ??= new List<JsonConverter>();
                src.Converters.Add(converter);
            }
            return src;
        }

        public static JsonSerializerSettings AddConverters(this JsonSerializerSettings src, IEnumerable<JsonConverter> converters)
        {
            if (converters != null)
            {
                src.Converters ??= new List<JsonConverter>();
                foreach (var converter in converters)
                {
                    if (converter != null)
                        src.Converters.Add(converter);
                }
            }
            return src;
        }
        
        public static string ToJsonStr(this object obj, bool camelCase = false, bool ignoreNull = false, bool indented = false, 
            IEnumerable<JsonConverter> converters = null)
        {
            var options = GetJsonSettings(camelCase);
            options.NullValueHandling = ignoreNull ? NullValueHandling.Ignore : NullValueHandling.Include;
            if (indented) options.Formatting = Formatting.Indented;
            AddConverters(options, converters);

            return ToJsonStr(obj, options);
        }

        public static string ToJsonStr(this object obj, JsonSerializerSettings jsonSerializerSettings)
        {
			if (obj == null) return null; // null is default json to "null"
            return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
        }

        public static T JsonStrTo<T>(this string json, IEnumerable<JsonConverter> converters = null)
        {
            return (T)JsonStrTo(json, typeof(T), converters);
        }

        public static object JsonStrTo(this string json, Type type = null, IEnumerable<JsonConverter> converters = null)
        {
            var options = GetJsonSettings();
            AddConverters(options, converters);
            return JsonStrTo(json, type, options);
        }

        public static T JsonStrTo<T>(this string json, JsonSerializerSettings jsonSerializerSettings)
        {
            return (T)JsonStrTo(json, typeof(T), jsonSerializerSettings);
        }

        public static object JsonStrTo(this string json, Type type, JsonSerializerSettings jsonSerializerSettings)
        {
            //if (json == null) return null; // converters maybe handle null
            try
            {
                /** 
                 * * no-need add CamelCasePropertyNamesContractResolver, it will Deserialize ok by using v12.0.2+
                 * * when type=typeof(string) then will throw error
                 */
                return JsonConvert.DeserializeObject(json, type, jsonSerializerSettings);
            }
            catch (Exception ex)
            {
                // if `json="" type=int` then be throw
                if (string.IsNullOrEmpty(json)) return null;
                throw new SerializationException($"Deserialize to type='{type}' error.\njson=' {(json.Length <= 50 ? json : json[..50] + "...")} '\n" + ex.Message, ex);
            }
        }
    }

    public static partial class JsonNetUtils
    {
        public static string ToJsonString(this object obj, bool camelCase = false, bool ignoreNull = false, bool indented = false,
            IEnumerable<JsonConverter> converters = null)
        {
            return ToJsonString(obj, (opt) =>
            {
                if (camelCase) opt.ContractResolver = new CamelCasePropertyNamesContractResolver();
                opt.NullValueHandling = ignoreNull ? NullValueHandling.Ignore : NullValueHandling.Include;
                opt.Formatting = indented ? Formatting.Indented : Formatting.None;
                AddConverters(opt, converters);
            });
        }

        public static string ToJsonString(this object obj, Action<JsonSerializerSettings> config)
        {
            var opt = new JsonSerializerSettings();
            config?.Invoke(opt);
            return JsonConvert.SerializeObject(obj, opt);
        }
    }
}