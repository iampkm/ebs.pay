using CQSS.Pay.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.SwfitPass.Api
{
    public class SwiftPassConfig
    {
        /// <summary>
        /// 商户号，由平台分配
        /// </summary>
        public static string mch_id = AppConfig.Global.SwiftPassMchID;
        /// <summary>
        /// 商户密钥，由平台分配
        /// </summary>
        public static string key = AppConfig.Global.SwiftPassKey;
        /// <summary>
        /// 接口版本号
        /// </summary>
        public static string version = AppConfig.Global.SwiftPassVersion;
        /// <summary>
        /// 请求url
        /// </summary>
        public static string req_url = AppConfig.Global.SwiftPassServerUrl;
        /// <summary>
        /// 字符集
        /// </summary>
        public static string charset = "UTF-8";
    }
}
