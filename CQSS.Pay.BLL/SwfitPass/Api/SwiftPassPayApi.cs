using CQSS.Pay.Model;
using CQSS.Pay.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.SwfitPass.Api
{
    public class SwiftPassPayApi
    {
        /// <summary>
        /// 微信扫码支付
        /// </summary>
        /// <param name="out_trade_no">商户订单号</param>
        /// <param name="total_fee">支付金额</param>
        /// <param name="notify_url">通知地址</param>
        /// <param name="time_expire">订单超时时间</param>
        /// <returns></returns>
        public static string WeChatNativePay(string out_trade_no, int total_fee, string notify_url, string time_expire)
        {
            var reqData = SwiftPassCore.GetDefaultParameter();
            reqData.SetValue("service", "pay.weixin.native");//接口类型：pay.weixin.native
            reqData.SetValue("out_trade_no", out_trade_no);//商户订单号
            reqData.SetValue("body", string.Format("{0}订单:{1}", AppConfig.Global.WebSiteName, out_trade_no));//商品描述
            reqData.SetValue("attach", string.Format("{0}订单:{1}", AppConfig.Global.WebSiteName, out_trade_no));//附加信息
            reqData.SetValue("total_fee", total_fee.ToString());//总金额
            reqData.SetValue("time_start", DateTime.Now.ToString("yyyyMMddHHmmss")); //订单生成时间
            reqData.SetValue("time_expire", time_expire);//订单超时时间
            reqData.SetValue("notify_url", notify_url);//通知地址，必填项，接收平台通知的URL，需给绝对路径，255字符内;此URL要保证外网能访问
            reqData.SetValue("sign", SwiftPassCore.CreateSign(reqData));//以上参数进行签名

            string data = reqData.ToXml();//生成XML报文
            Dictionary<string, string> reqContent = new Dictionary<string, string>();
            reqContent.Add("url", SwiftPassConfig.req_url);
            reqContent.Add("data", data);
            PayHttpClient pay = new PayHttpClient();
            pay.setReqContent(reqContent);
            if (!pay.call())
                throw new BizException(string.Format("HTTP ERROR：Code[{0}] Message[{1}]", pay.getResponseCode(), pay.getErrInfo()));

            var resData = new ParameterData();
            resData.FromXml(pay.getResContent());
            if (resData.GetValue("status") != "0") //校验返回状态
                throw new BizException("返回状态错误代码：" + resData.GetValue("status") + "，错误信息：" + resData.GetValue("message"));

            if (!SwiftPassCore.CheckSign(resData)) //校验数据签名
                throw new BizException("扫码支付签名校验未通过");

            if (resData.GetValue("result_code") != "0") //校验业务结果
                throw new BizException("业务结果错误代码：" + resData.GetValue("err_code") + "，错误信息：" + resData.GetValue("err_msg"));

            return resData.GetValue("code_url");//支付链接，用于生成二维码
        }

        /// <summary>
        /// 微信公众号支付
        /// </summary>
        /// <param name="out_trade_no"></param>
        /// <param name="total_fee"></param>
        /// <param name="notify_url"></param>
        /// <param name="time_expire"></param>
        /// <param name="openid"></param>
        /// <returns></returns>
        public static string WeChatJsApiPay(string out_trade_no, int total_fee, string notify_url, string time_expire, string openid)
        {
            var reqData = SwiftPassCore.GetDefaultParameter();
            reqData.SetValue("service", "pay.weixin.jspay");//接口类型：pay.weixin.jspay
            reqData.SetValue("out_trade_no", out_trade_no);//商户订单号
            reqData.SetValue("body", string.Format("{0}订单:{1}", AppConfig.Global.WebSiteName, out_trade_no));//商品描述
            reqData.SetValue("attach", string.Format("{0}订单:{1}", AppConfig.Global.WebSiteName, out_trade_no));//附加信息
            reqData.SetValue("total_fee", total_fee.ToString());//总金额
            reqData.SetValue("time_start", DateTime.Now.ToString("yyyyMMddHHmmss")); //订单生成时间
            reqData.SetValue("time_expire", time_expire);//订单超时时间
            reqData.SetValue("notify_url", notify_url);//通知地址，必填项，接收平台通知的URL，需给绝对路径，255字符内;此URL要保证外网能访问
            reqData.SetValue("is_raw", "1");//是否原生态支付方式 1.是 0.否 默认0
            reqData.SetValue("sub_openid", openid);//微信用户关注商家公众号的openid
            reqData.SetValue("sub_appid", CQSS.Pay.BLL.WeChatPay.Api.WxPayConfig.APPID);//微信公众平台基本配置中的AppID
            reqData.SetValue("sign", SwiftPassCore.CreateSign(reqData));//以上参数进行签名

            string data = reqData.ToXml();//生成XML报文
            Dictionary<string, string> reqContent = new Dictionary<string, string>();
            reqContent.Add("url", SwiftPassConfig.req_url);
            reqContent.Add("data", data);
            PayHttpClient pay = new PayHttpClient();
            pay.setReqContent(reqContent);
            if (!pay.call())
                throw new BizException(string.Format("HTTP ERROR：Code[{0}] Message[{1}]", pay.getResponseCode(), pay.getErrInfo()));

            var resData = new ParameterData();
            resData.FromXml(pay.getResContent());
            if (resData.GetValue("status") != "0") //校验返回状态
                throw new BizException("返回状态错误代码：" + resData.GetValue("status") + "，错误信息：" + resData.GetValue("message"));

            if (!SwiftPassCore.CheckSign(resData)) //校验数据签名
                throw new BizException("公众号支付签名校验未通过");

            if (resData.GetValue("result_code") != "0") //校验业务结果
                throw new BizException("业务结果错误代码：" + resData.GetValue("err_code") + "，错误信息：" + resData.GetValue("err_msg"));

            return resData.GetValue("pay_info");//json格式的字符串，作用于原生态js支付时的参数
        }

        /// <summary>
        /// 关闭订单
        /// </summary>
        /// <param name="out_trade_no"></param>
        /// <returns></returns>
        public static ParameterData CloseOrder(string out_trade_no)
        {
            var reqData = SwiftPassCore.GetDefaultParameter();
            reqData.SetValue("service", "unified.trade.close");//接口类型 unified.trade.close
            reqData.SetValue("out_trade_no", out_trade_no);//商户订单号
            reqData.SetValue("sign", SwiftPassCore.CreateSign(reqData));//以上参数进行签名

            string data = reqData.ToXml();//生成XML报文
            Dictionary<string, string> reqContent = new Dictionary<string, string>();
            reqContent.Add("url", SwiftPassConfig.req_url);
            reqContent.Add("data", data);
            PayHttpClient pay = new PayHttpClient();
            pay.setReqContent(reqContent);
            if (!pay.call())
                throw new BizException(string.Format("HTTP ERROR：Code[{0}] Message[{1}]", pay.getResponseCode(), pay.getErrInfo()));

            var resData = new ParameterData();
            resData.FromXml(pay.getResContent());
            return resData;
        }

        /// <summary>
        /// 交易单查询
        /// </summary>
        /// <param name="out_trade_no"></param>
        /// <param name="transaction_id"></param>
        /// <returns></returns>
        public static ParameterData OrderQuery(string out_trade_no, string transaction_id)
        {
            var reqData = SwiftPassCore.GetDefaultParameter();
            reqData.SetValue("service", "unified.trade.query");//接口类型 unified.trade.query
            reqData.SetValue("out_trade_no", out_trade_no);//商户订单号
            reqData.SetValue("transaction_id", transaction_id);//平台订单号
            reqData.SetValue("sign", SwiftPassCore.CreateSign(reqData));//以上参数进行签名

            string data = reqData.ToXml();//生成XML报文
            Dictionary<string, string> reqContent = new Dictionary<string, string>();
            reqContent.Add("url", SwiftPassConfig.req_url);
            reqContent.Add("data", data);
            PayHttpClient pay = new PayHttpClient();
            pay.setReqContent(reqContent);
            if (!pay.call())
                throw new BizException(string.Format("HTTP ERROR：Code[{0}] Message[{1}]", pay.getResponseCode(), pay.getErrInfo()));

            var resData = new ParameterData();
            resData.FromXml(pay.getResContent());
            return resData;
        }

        /// <summary>
        /// 申请退款
        /// </summary>
        /// <param name="transaction_id"></param>
        /// <param name="out_trade_no"></param>
        /// <param name="total_fee"></param>
        /// <param name="refund_fee"></param>
        /// <param name="out_refund_no"></param>
        /// <returns></returns>
        public static ParameterData Refund(string transaction_id, string out_trade_no, int total_fee, int refund_fee, string out_refund_no)
        {
            var reqData = SwiftPassCore.GetDefaultParameter();
            reqData.SetValue("service", "unified.trade.refund");//接口类型：unified.trade.refund
            reqData.SetValue("out_trade_no", out_trade_no);//商户订单号
            reqData.SetValue("transaction_id", transaction_id);//平台订单号
            reqData.SetValue("out_refund_no", out_refund_no);//商户退款单号
            reqData.SetValue("total_fee", total_fee.ToString());//总金额
            reqData.SetValue("refund_fee", refund_fee.ToString());//退款金额
            reqData.SetValue("sign", SwiftPassCore.CreateSign(reqData));//以上参数进行签名

            string data = reqData.ToXml();//生成XML报文
            Dictionary<string, string> reqContent = new Dictionary<string, string>();
            reqContent.Add("url", SwiftPassConfig.req_url);
            reqContent.Add("data", data);
            PayHttpClient pay = new PayHttpClient();
            pay.setReqContent(reqContent);
            if (!pay.call())
                throw new BizException(string.Format("HTTP ERROR：Code[{0}] Message[{1}]", pay.getResponseCode(), pay.getErrInfo()));

            var resData = new ParameterData();
            resData.FromXml(pay.getResContent());
            if (resData.GetValue("status") != "0") //校验返回状态
                throw new BizException("返回状态错误代码：" + resData.GetValue("status") + "，错误信息：" + resData.GetValue("message"));

            if (!SwiftPassCore.CheckSign(resData)) //校验数据签名
                throw new BizException("申请退款签名校验未通过");

            if (resData.GetValue("result_code") != "0") //校验业务结果
                throw new BizException("业务结果错误代码：" + resData.GetValue("err_code") + "，错误信息：" + resData.GetValue("err_msg"));

            return resData;
        }

        /// <summary>
        /// 退款单查询
        /// </summary>
        /// <param name="out_trade_no"></param>
        /// <param name="transaction_id"></param>
        /// <param name="out_refund_no"></param>
        /// <param name="refund_id"></param>
        /// <returns></returns>
        public static ParameterData RefundQuery(string out_trade_no, string transaction_id, string out_refund_no, string refund_id)
        {
            var reqData = SwiftPassCore.GetDefaultParameter();
            reqData.SetValue("service", "unified.trade.refundquery");//接口类型 unified.trade.refundquery
            reqData.SetValue("out_trade_no", out_trade_no);//商户订单号
            reqData.SetValue("transaction_id", transaction_id);//平台订单号
            reqData.SetValue("out_refund_no", out_refund_no);//商户退款单号
            reqData.SetValue("refund_id", refund_id);//平台退款单号
            reqData.SetValue("sign", SwiftPassCore.CreateSign(reqData));//以上参数进行签名

            string data = reqData.ToXml();//生成XML报文
            Dictionary<string, string> reqContent = new Dictionary<string, string>();
            reqContent.Add("url", SwiftPassConfig.req_url);
            reqContent.Add("data", data);
            PayHttpClient pay = new PayHttpClient();
            pay.setReqContent(reqContent);
            if (!pay.call())
                throw new BizException(string.Format("HTTP ERROR：Code[{0}] Message[{1}]", pay.getResponseCode(), pay.getErrInfo()));

            var resData = new ParameterData();
            resData.FromXml(pay.getResContent());
            return resData;
        }
    }
}
