using CQSS.Pay.Util.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Caching;
using System.Xml;

namespace CQSS.Pay.Util.Helper
{
    /// <summary>
    /// 配置文件加载器
    /// </summary>
    public class ConfigLoader
    {
        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath">配置文件路径</param>
        /// <returns></returns>
        public static T LoadConfig<T>(string filePath) where T : class
        {
            if (!System.IO.File.Exists(filePath))
                return Activator.CreateInstance<T>();

            var data = DictionaryFromXmlFile(filePath);
            T t = GetInstanceT<T>(data);
            return t;
        }

        /// <summary>
        /// 通过键值对获得实例化对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T GetInstanceT<T>(Dictionary<string, string> data) where T : class
        {
            T t = Activator.CreateInstance<T>();
            if (data == null || data.Count == 0)
                return t;

            var mapping = new Dictionary<int, string>();
            var propertyInfos = typeof(T).GetProperties();
            for (int i = 0; i < propertyInfos.Length; i++)
            {
                if (propertyInfos[i].CanWrite == false)
                    continue;

                foreach (var key in data.Keys)
                {
                    if (string.Equals(propertyInfos[i].Name, key, StringComparison.CurrentCultureIgnoreCase))
                    {
                        mapping[i] = key;
                        break;
                    }
                }
            }

            if (mapping.Count == 0)
                return t;

            foreach (var pair in mapping)
                t.SetPropertyValue(propertyInfos[pair.Key], data[pair.Value]);

            return t;
        }

        /// <summary>
        /// 由xml文件转化成键值对
        /// </summary>
        /// <param name="filePath">xml文件路径</param>
        /// <returns></returns>
        public static Dictionary<string, string> DictionaryFromXmlFile(string filePath)
        {
            var data = new Dictionary<string, string>();
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return data;

                var doc = new XmlDocument();
                doc.Load(filePath);
                foreach (XmlNode child in doc.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.XmlDeclaration)
                        continue;

                    foreach (XmlNode node in child.ChildNodes)
                    {
                        if (node.NodeType != XmlNodeType.Element)
                            continue;

                        var element = (XmlElement)node;
                        var keyNode = element.GetAttributeNode("key");
                        var valueNode = element.GetAttributeNode("value");
                        if (keyNode != null && valueNode != null)
                            data[keyNode.Value] = valueNode.Value;
                        else
                            data[element.Name] = element.InnerText;
                    }
                }
            }
            catch { }
            return data;
        }
    }

    /// <summary>
    /// 配置文件加载器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConfigLoader<T> : ConfigLoader where T : class
    {
        /// <summary>
        /// 配置文件加载器
        /// 基于应用程序缓存依赖功能，在配置文件发生变更后，无需重启应用程序即可实现自动更新
        /// </summary>
        /// <param name="filePath">配置文件物理路径</param>
        /// <param name="callBack">配置文件加载完毕的回调函数</param>
        public ConfigLoader(string filePath, Action<T> callBack)
        {
            var type = typeof(T);
            this._filePath = filePath;
            this._cacheKey = string.Format("{0}_{1}", type.Name, type.GetHashCode());
            this._callBack = callBack;

            this.LoadConfig();
        }

        /// <summary>
        /// 配置文件路径
        /// </summary>
        private string _filePath;

        /// <summary>
        /// 依赖缓存的键
        /// </summary>
        private string _cacheKey;

        /// <summary>
        /// 加载配置文件完成后的回调函数
        /// </summary>
        private Action<T> _callBack;

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public void LoadConfig()
        {
            var data = LoadConfig<T>(this._filePath);
            var dependency = new CacheDependency(this._filePath);
            //警告：此处禁止使用HttpRuntime.Cache.Insert，否则可能在缓存过期通知回调函数时，形成死循环调用！！
            System.Web.HttpRuntime.Cache.Add("config_" + this._cacheKey, "", dependency, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.High, ConfigRemovedCallback);

            if (this._callBack != null)
                this._callBack(data);
        }

        /// <summary>
        /// 移除缓存项的通知回调函数
        /// </summary>
        /// <param name="key">被移除的缓存键</param>
        /// <param name="value">被移除的缓存值</param>
        /// <param name="reason">被移除的原因</param>
        private void ConfigRemovedCallback(string key, object value, CacheItemRemovedReason reason)
        {
            this.LoadConfig();
        }
    }
}
