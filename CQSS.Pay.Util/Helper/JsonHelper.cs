using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace CQSS.Pay.Util.Helper
{
    /// <summary>
    /// JSON序列化和反序列化
    /// </summary>
    public static class JsonHelper
    {
        #region 全局设置
        /// <summary>
        /// 默认设置
        /// </summary>
        private static JsonSerializerSettings _defaultSetting
        {
            get
            {
                return new JsonSerializerSettings()
                {
                    //序列化和反序列化时类型转换方式
                    Converters = new List<JsonConverter>() { new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff" } },
                    //序列化和反序列化时是否忽略null值
                    NullValueHandling = NullValueHandling.Include,
                };
            }
        }

        /// <summary>
        /// 提供 JavaScript 序列化和反序列化功能
        /// </summary>
        private static JavaScriptSerializer _serializer = new JavaScriptSerializer();

        /// <summary>
        /// 移除某类型的转换格式
        /// </summary>
        /// <param name="converters"></param>
        /// <param name="type"></param>
        public static void RemoveType(this IList<JsonConverter> converters, Type type)
        {
            if (converters == null)
                return;

            for (int i = 0; i < converters.Count; i++)
            {
                var item = converters[i];
                if (item.GetType() == type)
                {
                    converters.Remove(item);
                    i--;
                }
            }
        }
        #endregion

        /// <summary>
        /// Json对象序列化
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string JavaScriptSerialize(object obj)
        {
            return _serializer.Serialize(obj);
        }

        /// <summary>
        /// Json对象序列化
        /// </summary>
        /// <param name="obj">被序列化的对象</param>
        /// <param name="format">是否格式化</param>
        /// <returns></returns>
        public static string Serialize(object obj, bool format = false)
        {
            LimitPropsContractResolver limitProps = null;
            return Serialize(obj, limitProps, format);
        }

        /// <summary>
        /// Json对象序列化
        /// </summary>
        /// <param name="obj">被序列化的对象</param>
        /// <param name="limitProps">属性限制设置</param>
        /// <param name="format">是否格式化</param>
        /// <returns></returns>
        public static string Serialize(object obj, LimitPropsContractResolver limitProps, bool format = false)
        {
            JsonSerializerSettings settings = _defaultSetting;
            if (limitProps != null) settings.ContractResolver = limitProps;
            return Serialize(obj, settings, format);
        }

        /// <summary>
        /// Json对象序列化
        /// </summary>
        /// <param name="obj">被序列化的对象</param>
        /// <param name="settings">序列化设置</param>
        /// <param name="format">是否格式化</param>
        /// <returns></returns>
        public static string Serialize(object obj, JsonSerializerSettings settings, bool format = false)
        {
            var formatting = Formatting.None;
            if (format) formatting = Formatting.Indented;
            return JsonConvert.SerializeObject(obj, formatting, settings);
        }

        /// <summary>
        /// Json对象反序列化
        /// </summary>
        /// <typeparam name="T">反序列化结果类型泛型</typeparam>
        /// <param name="jsonString">json字符串</param>
        /// <returns></returns>
        public static T Deserialize<T>(string jsonString)
        {
            LimitPropsContractResolver limitProps = null;
            return Deserialize<T>(jsonString, limitProps);
        }

        /// <summary>
        /// Json对象反序列化
        /// </summary>
        /// <typeparam name="T">反序列化结果类型泛型</typeparam>
        /// <param name="jsonString">json字符串</param>
        /// <param name="limitProps">属性限制设置</param>
        /// <returns></returns>
        public static T Deserialize<T>(string jsonString, LimitPropsContractResolver limitProps)
        {
            JsonSerializerSettings settings = _defaultSetting;
            settings.Converters.RemoveType(typeof(IsoDateTimeConverter));
            if (limitProps != null) settings.ContractResolver = limitProps;
            return Deserialize<T>(jsonString, settings);
        }

        /// <summary>
        /// Json对象反序列化
        /// </summary>
        /// <typeparam name="T">反序列化结果类型泛型</typeparam>
        /// <param name="jsonString">json字符串</param>
        /// <param name="settings">序列化设置</param>
        /// <returns></returns>
        public static T Deserialize<T>(string jsonString, JsonSerializerSettings settings)
        {
            settings.Converters.RemoveType(typeof(IsoDateTimeConverter));
            return JsonConvert.DeserializeObject<T>(jsonString, settings);
        }

        /// <summary>
        /// 读取json字符串节点的值
        /// </summary>
        /// <param name="jsonString">json字符串</param>
        /// <param name="nodeName">节点名称</param>
        /// <returns></returns>
        public static string GetJsonNode(string jsonString, string nodeName)
        {
            try
            {
                var json = JObject.Parse(jsonString);
                var node = json[nodeName];
                if (node != null && node.Parent.HasValues)
                    if (node.HasValues)
                        return JsonConvert.SerializeObject(node);
                    else
                        return node.ToString();
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 读取默认设置
        /// </summary>
        /// <returns></returns>
        public static JsonSerializerSettings GetDefaultSettings()
        {
            return _defaultSetting;
        }
    }

    public class LimitPropsContractResolver : DefaultContractResolver
    {
        /// <summary>
        /// 属性数组
        /// </summary>
        private string[] _props = null;

        /// <summary>
        /// props是否是保留属性  true：保留  false：排除
        /// </summary>
        private bool _retain;

        /// <summary>
        /// 限制输出属性
        /// </summary>
        /// <param name="props">传入的属性数组</param>
        /// <param name="retain">props是否是保留属性  true：保留  false：排除</param>
        public LimitPropsContractResolver(string[] props, bool retain = true)
        {
            //指定要序列化属性的清单
            this._props = props;
            this._retain = retain;
        }

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            IList<JsonProperty> list = base.CreateProperties(type, memberSerialization);
            //只保留清单有列出的属性
            return list.Where(p =>
            {
                if (_retain)
                    return _props.Contains(p.PropertyName);
                else
                    return !_props.Contains(p.PropertyName);
            }).ToList();
        }
    }
}
