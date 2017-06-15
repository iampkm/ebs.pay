using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Util
{
    /// <summary>
    /// 应用配置
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// 应用配置
        /// 在此构造函数中实现配置文件加载
        /// </summary>
        static AppConfig()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "global.config");
            new ConfigLoader<GlobalConfigInfo>(path, (GlobalConfigInfo data) =>
            {
                Global = data;
            });
        }

        /// <summary>
        /// 全站配置
        /// </summary>
        public static GlobalConfigInfo Global { get; private set; }
    }

    /// <summary>
    /// 全站配置
    /// </summary>
    public class GlobalConfigInfo
    {
        /// <summary>
        /// 全站配置
        /// 请在此构造函数中设置配置项的默认值
        /// </summary>
        public GlobalConfigInfo()
        {
            this.DocumentAppId = "app_id";
            this.DocumentAppSecret = "C4DDF940-0E1D-4CAC-AD38-883B930D5170";
        }

        /// <summary>
        /// 本站点域名
        /// 备注：页面直接引用
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// 本站点网站名称
        /// 备注：页面直接引用
        /// </summary>
        public string WebSiteName { get; set; }

        #region 文档及测试配置
        /// <summary>
        /// 是否是测试模式
        /// </summary>
        public bool IsTestMode { get; set; }

        /// <summary>
        /// 是否是模拟模式
        /// </summary>
        public bool IsSimulateMode { get; set; }

        /// <summary>
        /// 测试AppId
        /// </summary>
        public string TestAppId { get; set; }

        /// <summary>
        /// 文档AppId
        /// 备注：页面直接引用
        /// </summary>
        public string DocumentAppId { get; set; }

        /// <summary>
        /// 文档AppSecret
        /// 备注：页面直接引用
        /// </summary>
        public string DocumentAppSecret { get; set; }
        #endregion

        #region Alipay配置
        /// <summary>
        /// Alipay合作者身份ID
        /// </summary>
        public string AlipayPartner { get; set; }

        /// <summary>
        /// Alipay卖家支付宝账号
        /// </summary>
        public string AlipaySellerEmail { get; set; }

        /// <summary>
        /// Alipay密钥
        /// </summary>
        public string AlipayKey { get; set; }

        /// <summary>
        /// 支付宝支付日志存放的文件夹名称
        /// </summary>
        public string AlipayLogFolder { get { return "Alipay"; } }
        #endregion

        #region WeChatPay配置
        /// <summary>
        /// 微信AppID
        /// </summary>
        public string WeChatAppID { get; set; }

        /// <summary>
        /// 微信AppSecret
        /// </summary>
        public string WeChatAppSecret { get; set; }

        /// <summary>
        /// 微信商户号
        /// </summary>
        public string WeChatMchID { get; set; }

        /// <summary>
        /// 微信商户支付密钥
        /// </summary>
        public string WeChatPayKey { get; set; }

        /// <summary>
        /// 微信支付证书路径
        /// </summary>
        public string WeChatPaySSLCertPath { get; set; }

        /// <summary>
        /// 微信支付证书密钥
        /// </summary>
        public string WeChatPaySSLCertPassword { get; set; }

        /// <summary>
        /// 微信支付日志存放的文件夹名称
        /// </summary>
        public string WeChatPayLogFolder { get { return "WeChatPay"; } }
        #endregion

        #region Allinpay配置
        /// <summary>
        /// 通联商户号
        /// </summary>
        public string AllinpayMerchantId { get; set; }

        /// <summary>
        /// 电商企业代码（10位海关代码）
        /// </summary>
        public string AllinpayEshopEntCode { get; set; }

        /// <summary>
        /// 电商企业名称（企业备案的企业全称）
        /// </summary>
        public string AllinpayEshopEntName { get; set; }

        /// <summary>
        /// Allinpay加密密钥
        /// </summary>
        public string AllinpayKey { get; set; }

        /// <summary>
        /// 通联支付服务器网关地址
        /// </summary>
        public string AllinpayServerUrl { get; set; }

        /// <summary>
        /// 通联WAP支付服务器网关地址
        /// </summary>
        public string AllinpayWapServerUrl { get; set; }

        /// <summary>
        /// 通联退款服务器网关地址
        /// </summary>
        public string AllinpayRefundServerUrl { get; set; }

        /// <summary>
        /// 通联支付证书路径
        /// </summary>
        public string AllinpaySSLCertPath { get; set; }

        /// <summary>
        /// 通联支付日志存放的文件夹名称
        /// </summary>
        public string AllinpayLogFolder { get { return "Allinpay"; } }
        #endregion

        #region SwiftPass配置
        /// <summary>
        /// 威富通商户号
        /// </summary>
        public string SwiftPassMchID { get; set; }

        /// <summary>
        /// 威富通商户密钥
        /// </summary>
        public string SwiftPassKey { get; set; }

        /// <summary>
        /// 威富通接口版本号
        /// </summary>
        public string SwiftPassVersion { get; set; }

        /// <summary>
        /// 威富通服务器网关地址
        /// </summary>
        public string SwiftPassServerUrl { get; set; }

        /// <summary>
        /// 威富通微信支付日志存放的文件夹名称
        /// </summary>
        public string SwiftPassWeChatPayLogFolder { get { return "SwiftPassWeChatPay"; } }
        #endregion
    }
}
