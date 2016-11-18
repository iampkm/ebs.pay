using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Util
{
    public class AppConfig
    {
        #region 异常日志
        private static string strFormat = "Config/appSettings中“{0}”读取异常！";
        private static void WriteLog(string configKey)
        {
            LogWriter.WriteLog(string.Format(strFormat, configKey), ExceptionHelper.ExceptionLevel.Exception);
        }
        #endregion

        /// <summary>
        /// 本站点域名
        /// </summary>
        public static string Domain
        {
            get
            {
                string configKey = "Domain";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 系统加密密钥
        /// </summary>
        public static string SignKey
        {
            get
            {
                string configKey = "SignKey";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 是否是测试模式
        /// </summary>
        public static bool IsTestMode
        {
            get
            {
                string configKey = "IsTestMode";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim() == "1";
                }
                catch { return false; }
            }
        }

        #region Alipay配置
        /// <summary>
        /// Alipay合作者身份ID
        /// </summary>
        public static string AlipayPartner
        {
            get
            {
                string configKey = "AlipayPartner";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// Alipay卖家支付宝账号
        /// </summary>
        public static string AlipaySellerEmail
        {
            get
            {
                string configKey = "AlipaySellerEmail";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// Alipay密钥
        /// </summary>
        public static string AlipayKey
        {
            get
            {
                string configKey = "AlipayKey";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 支付宝支付日志存放的文件夹名称
        /// </summary>
        public static string AlipayLogFolder
        {
            get { return "Alipay"; }
        }
        #endregion

        #region WeChatPay配置
        /// <summary>
        /// 微信AppID
        /// </summary>
        public static string WeChatAppID
        {
            get
            {
                string configKey = "WeChatAppID";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 微信AppSecret
        /// </summary>
        public static string WeChatAppSecret
        {
            get
            {
                string configKey = "WeChatAppSecret";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 微信商户号
        /// </summary>
        public static string WeChatMchID
        {
            get
            {
                string configKey = "WeChatMchID";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 微信商户支付密钥
        /// </summary>
        public static string WeChatPayKey
        {
            get
            {
                string configKey = "WeChatPayKey";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 微信支付证书路径
        /// </summary>
        public static string WeChatPaySSLCertPath
        {
            get
            {
                string configKey = "WeChatPaySSLCertPath";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 微信支付证书密钥
        /// </summary>
        public static string WeChatPaySSLCertPassword
        {
            get
            {
                string configKey = "WeChatPaySSLCertPassword";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 微信支付日志存放的文件夹名称
        /// </summary>
        public static string WeChatPayLogFolder
        {
            get { return "WeChatPay"; }
        }
        #endregion

        #region Allinpay配置
        /// <summary>
        /// 通联商户号
        /// </summary>
        public static string AllinpayMerchantId
        {
            get
            {
                string configKey = "AllinpayMerchantId";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 电商企业代码（10位海关代码）
        /// </summary>
        public static string AllinpayEshopEntCode
        {
            get
            {
                string configKey = "AllinpayEshopEntCode";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 电商企业名称（企业备案的企业全称）
        /// </summary>
        public static string AllinpayEshopEntName
        {
            get
            {
                string configKey = "AllinpayEshopEntName";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// Allinpay加密密钥
        /// </summary>
        public static string AllinpayKey
        {
            get
            {
                string configKey = "AllinpayKey";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 通联支付服务器网关地址
        /// </summary>
        public static string AllinpayServerUrl
        {
            get
            {
                string configKey = "AllinpayServerUrl";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 通联WAP支付服务器网关地址
        /// </summary>
        public static string AllinpayWapServerUrl
        {
            get
            {
                string configKey = "AllinpayWapServerUrl";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 通联退款服务器网关地址
        /// </summary>
        public static string AllinpayRefundServerUrl
        {
            get
            {
                string configKey = "AllinpayRefundServerUrl";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 通联支付证书路径
        /// </summary>
        public static string AllinpaySSLCertPath
        {
            get
            {
                string configKey = "AllinpaySSLCertPath";
                try
                {
                    return ConfigurationManager.AppSettings[configKey].ToString().Trim();
                }
                catch { WriteLog(configKey); return ""; }
            }
        }

        /// <summary>
        /// 通联支付日志存放的文件夹名称
        /// </summary>
        public static string AllinpayLogFolder
        {
            get { return "Allinpay"; }
        }
        #endregion
    }
}
