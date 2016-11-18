using System;
using System.Collections.Generic;
using System.Web;

namespace CQSS.Pay.BLL.WeChatPay.Api
{
    public class NativePay
    {
        /**
        * 生成扫描支付模式一URL
        * @param productId 商品ID
        * @return 模式一URL
        */
        public string GetPrePayUrl(string productId)
        {
            WxPayLog.Info(this.GetType().ToString(), "Native pay mode 1 url is producing...");

            WxPayData data = new WxPayData();
            data.SetValue("appid", WxPayConfig.APPID);//公众帐号id
            data.SetValue("mch_id", WxPayConfig.MCHID);//商户号
            data.SetValue("time_stamp", WxPayApi.GenerateTimeStamp());//时间戳
            data.SetValue("nonce_str", WxPayApi.GenerateNonceStr());//随机字符串
            data.SetValue("product_id", productId);//商品ID
            data.SetValue("sign", data.MakeSign());//签名
            string str = ToUrlParams(data.GetValues());//转换为URL串
            string url = "weixin://wxpay/bizpayurl?" + str;

            WxPayLog.Info(this.GetType().ToString(), "Get native pay mode 1 url : " + url);
            return url;
        }

        /**
        * 生成直接支付url，支付url有效期为2小时,模式二
        * @param out_trade_no 商户订单号
        * @param total_fee 总金额
        * @param notifyUrl 异步通知url
        * @param time_expire 交易结束时间
        * @return 模式二URL
        */
        public string GetPayUrl(string out_trade_no, int total_fee, string notify_url, string time_expire)
        {
            WxPayLog.Info(this.GetType().ToString(), "Native pay mode 2 url is producing...");

            WxPayData data = new WxPayData();
            data.SetValue("body", "世纪购订单:" + out_trade_no);//商品描述
            data.SetValue("attach", out_trade_no);//附加数据
            data.SetValue("out_trade_no", out_trade_no);//商户订单号
            data.SetValue("total_fee", total_fee);//总金额
            data.SetValue("time_start", DateTime.Now.ToString("yyyyMMddHHmmss"));//交易起始时间
            data.SetValue("time_expire", time_expire);//交易结束时间
            //data.SetValue("goods_tag", "SJG365");//商品标记
            data.SetValue("trade_type", "NATIVE");//交易类型
            data.SetValue("product_id", out_trade_no);//商品ID
            data.SetValue("notify_url", notify_url);

            WxPayData result = WxPayApi.UnifiedOrder(data);//调用统一下单接口
            if (result.GetValue("return_code").ToString() != "SUCCESS")
            {
                throw new WxPayException(result.GetValue("return_msg").ToString());
            }
            else if (result.GetValue("result_code").ToString() != "SUCCESS")
            {
                throw new WxPayException(result.GetValue("err_code_des").ToString());
            }

            string url = result.GetValue("code_url").ToString();//获得统一下单接口返回的二维码链接

            WxPayLog.Info(this.GetType().ToString(), "Get native pay mode 2 url : " + url);
            return url;
        }

        /**
        * 参数数组转换为url格式
        * @param map 参数名与参数值的映射表
        * @return URL字符串
        */
        private string ToUrlParams(SortedDictionary<string, object> map)
        {
            string buff = "";
            foreach (KeyValuePair<string, object> pair in map)
            {
                buff += pair.Key + "=" + pair.Value + "&";
            }
            buff = buff.Trim('&');
            return buff;
        }
    }
}