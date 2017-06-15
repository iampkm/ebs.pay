using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model
{
    public class AppEnum
    {
        /// <summary>
        /// 支付方式
        /// </summary>
        public enum PayType
        {
            /// <summary>
            /// 未知
            /// </summary>
            [Description("未知")]
            Unknown = 0,
            /// <summary>
            /// 支付宝支付
            /// </summary>
            [Description("支付宝")]
            Alipay = 1,
            /// <summary>
            /// 微信支付
            /// </summary>
            [Description("微信支付")]
            WeChatPay = 2,
            /// <summary>
            /// 通联支付
            /// </summary>
            [Description("通联支付")]
            Allinpay = 3,
            /// <summary>
            /// 通联POS机支付
            /// </summary>
            [Description("通联POS支付")]
            AllinpayPOS = 4,
            /// <summary>
            /// 威富通支付宝支付
            /// </summary>
            [Description("威富通支付宝")]
            SwiftPassAlipay = 5,
            /// <summary>
            /// 威富通微信支付
            /// </summary>
            [Description("威富通微信支付")]
            SwiftPassWeChatPay = 6,
        }

        /// <summary>
        /// 有效状态（全局）
        /// </summary>
        public enum GlobalStatus
        {
            /// <summary>
            /// 无效的
            /// </summary>
            Invalid = 0,
            /// <summary>
            /// 有效的
            /// </summary>
            Valid = 1,
        }

        /// <summary>
        /// 浏览器类型
        /// </summary>
        public enum BrowserType
        {
            /// <summary>
            /// 微信浏览器
            /// </summary>
            WeChat = 0,
            /// <summary>
            /// 其他浏览器
            /// </summary>
            Others = 1,
        }

        /// <summary>
        /// 支付通知状态
        /// </summary>
        public enum NotifyStatus
        {
            /// <summary>
            /// 已作废
            /// </summary>
            Canceled = -1,
            /// <summary>
            /// 初始
            /// </summary>
            Original = 0,
            /// <summary>
            /// 已完成
            /// </summary>
            Finished = 1,
        }

        /// <summary>
        /// 读取枚举值的Description
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns></returns>
        public static string GetEnumDescription(Enum value)
        {
            Type enumType = value.GetType();
            // 获取枚举常数名称。
            string name = Enum.GetName(enumType, value);
            if (name != null)
            {
                // 获取枚举字段。
                FieldInfo fieldInfo = enumType.GetField(name);
                if (fieldInfo != null)
                {
                    // 获取描述的属性。
                    DescriptionAttribute attr = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute), false) as DescriptionAttribute;
                    if (attr != null)
                    {
                        return attr.Description;
                    }
                }
            }
            return "未知";
        }
    }
}
