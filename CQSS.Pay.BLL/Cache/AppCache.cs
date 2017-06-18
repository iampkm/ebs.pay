using CQSS.Pay.DAL;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Cache
{
    public class AppCache
    {
        /// <summary>
        /// 获取AppSecret缓存
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        public static string GetAppSecret(string appId)
        {
            var appSecret = CacheHelper.Get("appId_" + appId) as string;
            if (string.IsNullOrEmpty(appSecret))
                appSecret = SetAppSecret(appId);

            return appSecret;
        }

        /// <summary>
        /// 设置AppSecret缓存
        /// </summary>
        /// <param name="appId"></param>
        /// <returns></returns>
        public static string SetAppSecret(string appId)
        {
            string appSecret = PayAppDAL.GetAppSecret(appId);
            string value = string.IsNullOrEmpty(appSecret) ? "" : appSecret;
            CacheHelper.Insert("appId_" + appId, value, string.IsNullOrEmpty(appSecret) ? TimeSpan.FromMinutes(10) : TimeSpan.FromDays(1));
            return value;
        }
    }
}
