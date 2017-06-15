using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;

namespace CQSS.Pay.Util.Helper
{
    public class CacheHelper
    {
        private static readonly Cache _cache = HttpRuntime.Cache;
        private static readonly TimeSpan _expireTime = TimeSpan.FromDays(1);
        /// <summary>
        /// 清空Cache对象
        /// </summary>
        public static void Clear()
        {
            IDictionaryEnumerator CacheEnum = _cache.GetEnumerator();
            var keyList = new List<string>();
            while (CacheEnum.MoveNext())
            {
                keyList.Add(CacheEnum.Key.ToString());
            }
            foreach (string key in keyList)
            {
                _cache.Remove(key);
            }
        }
        /// <summary>
        /// 根据正则表达式的模式移除Cache
        /// </summary>
        /// <param name="pattern">正则表达式</param>
        public static void RemoveByPattern(string pattern)
        {
            IDictionaryEnumerator CacheEnum = _cache.GetEnumerator();
            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
            while (CacheEnum.MoveNext())
            {
                if (regex.IsMatch(CacheEnum.Key.ToString()))
                    _cache.Remove(CacheEnum.Key.ToString());
            }
        }
        /// <summary>
        /// 根据键值移除Cache
        /// </summary>
        /// <param name="key">缓存键</param>
        public static void Remove(string key)
        {
            _cache.Remove(key);
        }
        /// <summary>
        /// 把对象加载到Cache
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        public static void Insert(string key, object value)
        {
            Insert(key, value, null, _expireTime);
        }
        /// <summary>
        /// 把对象加载到Cache,附加缓存依赖信息
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="dep">缓存依赖</param>
        public static void Insert(string key, object value, CacheDependency dep)
        {
            Insert(key, value, dep, _expireTime);
        }
        /// <summary>
        /// 把对象加载到Cache,附加过期时间信息
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expireTime">过期时间</param>
        public static void Insert(string key, object value, TimeSpan expireTime)
        {
            Insert(key, value, null, expireTime);
        }
        /// <summary>
        /// 把对象加载到Cache,附加过期时间信息和优先级
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expireTime">过期时间</param>
        /// <param name="priority">优先级</param>
        public static void Insert(string key, object value, TimeSpan expireTime, CacheItemPriority priority)
        {
            Insert(key, value, null, expireTime, priority);
        }
        /// <summary>
        /// 把对象加载到Cache,附加缓存依赖和过期时间(多少秒后过期)
        /// (默认优先级为Normal)
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="dep">缓存依赖</param>
        /// <param name="expireTime">过期时间</param>
        public static void Insert(string key, object value, CacheDependency dep, TimeSpan expireTime)
        {
            Insert(key, value, dep, expireTime, CacheItemPriority.Normal);
        }
        /// <summary>
        /// 把对象加载到Cache,附加缓存依赖和过期时间(多少秒后过期)及优先级
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="dep">缓存依赖</param>
        /// <param name="expireTime">过期时间</param>
        /// <param name="priority">优先级</param>
        public static void Insert(string key, object value, CacheDependency dep, TimeSpan expireTime, CacheItemPriority priority)
        {
            if (value != null)
            {
                _cache.Insert(key, value, dep, DateTime.Now.AddSeconds(expireTime.TotalSeconds), TimeSpan.Zero, priority, null);
            }
        }
        /// <summary>
        /// 把对象加到缓存并忽略优先级
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="expireTime">过期时间</param>
        public static void MicroInsert(string key, object value, TimeSpan expireTime)
        {
            if (value != null)
            {
                _cache.Insert(key, value, null, DateTime.Now.AddSeconds(expireTime.TotalSeconds), TimeSpan.Zero);
            }
        }
        /// <summary>
        /// 把对象加到缓存,并把过期时间设为最大值
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        public static void Max(string key, object value)
        {
            Max(key, value, null);
        }
        /// <summary>
        /// 把对象加到缓存,并把过期时间设为最大值,附加缓存依赖信息
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="dep">缓存依赖</param>
        public static void Max(string key, object value, CacheDependency dep)
        {
            if (value != null)
            {
                _cache.Insert(key, value, dep, DateTime.MaxValue, TimeSpan.Zero, CacheItemPriority.AboveNormal, null);
            }
        }
        /// <summary>
        /// 插入持久性缓存
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        public static void Permanent(string key, object value)
        {
            Permanent(key, value, null);
        }
        /// <summary>
        /// 插入持久性缓存,附加缓存依赖
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <param name="value">缓存值</param>
        /// <param name="dep">缓存依赖</param>
        public static void Permanent(string key, object value, CacheDependency dep)
        {
            if (value != null)
            {
                _cache.Insert(key, value, dep, DateTime.MaxValue, TimeSpan.Zero, CacheItemPriority.NotRemovable, null);
            }
        }
        /// <summary>
        /// 根据键获取被缓存的对象
        /// </summary>
        /// <param name="key">缓存键</param>
        /// <returns></returns>
        public static object Get(string key)
        {
            return _cache[key];
        }
    }
}
