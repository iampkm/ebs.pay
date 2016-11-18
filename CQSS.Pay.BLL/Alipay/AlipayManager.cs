using CQSS.Pay.BLL.Basic;
using CQSS.Pay.Model;
using CQSS.Pay.Util;
using CQSS.Pay.Util.Extension;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Alipay
{
    public class AlipayManager
    {
        /// <summary>
        /// 创建支付提交的表单
        /// </summary>
        /// <param name="orderInfo"></param>
        /// <returns></returns>
        public static string BuildRequestForm(PayOrderInfo orderInfo)
        {
            var paramsDict = new SortedDictionary<string, string>();
            //接口名称 不可空
            paramsDict.Add("service", "create_direct_pay_by_user");
            //合作者身份ID 签约的支付宝账号对应的支付宝唯一用户号 不可空
            paramsDict.Add("partner", AlipayConfig.partner);
            //卖家支付宝用户号 seller_id是以2088开头的纯16位数字 不可空
            paramsDict.Add("seller_id", AlipayConfig.seller_id);
            //参数编码字符集 商户网站使用的编码格式，如utf-8、gbk、gb2312等 不可空
            paramsDict.Add("_input_charset", AlipayConfig.input_charset.ToLower());
            //支付类型 只支持取值为1（商品购买） 不可空
            paramsDict.Add("payment_type", AlipayConfig.payment_type);
            //服务器异步通知页面路径 支付宝服务器主动通知商户网站里指定的页面http路径 可空
            paramsDict.Add("notify_url", string.Format("http://{0}/Alipay/OnlinePayNotify", AppConfig.Domain));
            //页面跳转同步通知页面路径 支付宝处理完请求后，当前页面自动跳转到商户网站里指定页面的http路径 可空
            paramsDict.Add("return_url", string.Format("http://{0}/Alipay/OnlinePayReturn", AppConfig.Domain));
            //商户网站唯一订单号 不可空
            paramsDict.Add("out_trade_no", orderInfo.OrderId);
            //商品的标题/交易标题/订单标题/订单关键字等，最长为128个汉字 不可空
            paramsDict.Add("subject", "世纪购订单:" + orderInfo.OrderId);
            //交易金额 该笔订单的资金总额，单位为RMB-Yuan。取值范围为[0.01，100000000.00]，精确到小数点后两位 不可空
            paramsDict.Add("total_fee", Convert.ToDecimal(orderInfo.PaymentAmt).ToString("#########0.00"));
            //商品描述 对一笔交易的具体描述信息。如果是多种商品，请将商品描述字符串累加传给body 可空
            paramsDict.Add("body", orderInfo.OrderId);

            int diffMinutes = (int)((DateTime.ParseExact(orderInfo.OrderTime, "yyyyMMddHHmmss", new CultureInfo("zh-CN", true)).AddHours(24) - DateTime.Now).TotalMinutes);
            //超时时间 设置未付款交易的超时时间，一旦超时，该笔交易就会自动被关闭。取值范围：1m～15d。m-分钟，h-小时，d-天，1c-当天（1c-当天的情况下，无论交易何时创建，都在0点关闭） 可空
            paramsDict.Add("it_b_pay", Math.Max(diffMinutes, 1) + "m");

            //建立请求
            string formHtml = AlipaySubmit.BuildRequest(paramsDict, "get", "确认");
            return formHtml;
        }

        /// <summary>
        /// 创建移动端支付提交的表单
        /// </summary>
        /// <param name="orderInfo"></param>
        /// <returns></returns>
        public static string BuildWapRequestForm(PayOrderInfo orderInfo)
        {
            var paramsDict = new SortedDictionary<string, string>();
            //接口名称 不可空
            paramsDict.Add("service", "alipay.wap.create.direct.pay.by.user");
            //合作者身份ID 签约的支付宝账号对应的支付宝唯一用户号 不可空
            paramsDict.Add("partner", AlipayConfig.partner);
            //卖家支付宝用户号 seller_id是以2088开头的纯16位数字 不可空
            paramsDict.Add("seller_id", AlipayConfig.seller_id);
            //参数编码字符集 商户网站使用的编码格式，如utf-8、gbk、gb2312等 不可空
            paramsDict.Add("_input_charset", AlipayConfig.input_charset.ToLower());
            //支付类型 只支持取值为1（商品购买） 不可空
            paramsDict.Add("payment_type", AlipayConfig.payment_type);
            //服务器异步通知页面路径 支付宝服务器主动通知商户网站里指定的页面http路径 可空
            paramsDict.Add("notify_url", string.Format("http://{0}/Alipay/OnlinePayNotify", AppConfig.Domain));
            //页面跳转同步通知页面路径 支付宝处理完请求后，当前页面自动跳转到商户网站里指定页面的http路径 可空
            paramsDict.Add("return_url", string.Format("http://{0}/Alipay/OnlinePayReturn", AppConfig.Domain));
            //商户网站唯一订单号 不可空
            paramsDict.Add("out_trade_no", orderInfo.OrderId);
            //商品的标题/交易标题/订单标题/订单关键字等，最长为128个汉字 不可空
            paramsDict.Add("subject", "世纪购订单:" + orderInfo.OrderId);
            //交易金额 该笔订单的资金总额，单位为RMB-Yuan。取值范围为[0.01，100000000.00]，精确到小数点后两位 不可空
            paramsDict.Add("total_fee", Convert.ToDecimal(orderInfo.PaymentAmt).ToString("#########0.00"));
            //商品描述 对一笔交易的具体描述信息。如果是多种商品，请将商品描述字符串累加传给body 可空
            paramsDict.Add("body", orderInfo.OrderId);

            int diffMinutes = (int)((DateTime.ParseExact(orderInfo.OrderTime, "yyyyMMddHHmmss", new CultureInfo("zh-CN", true)).AddHours(24) - DateTime.Now).TotalMinutes);
            //超时时间 设置未付款交易的超时时间，一旦超时，该笔交易就会自动被关闭。取值范围：1m～15d。m-分钟，h-小时，d-天，1c-当天（1c-当天的情况下，无论交易何时创建，都在0点关闭） 可空
            paramsDict.Add("it_b_pay", Math.Max(diffMinutes, 1) + "m");

            //建立请求
            string formHtml = AlipaySubmit.BuildRequest(paramsDict, "get", "确认");
            return formHtml;
        }
    }
}
