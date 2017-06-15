using System;
using System.Collections.Generic;
using System.Web;
using System.Threading;
using CQSS.Pay.Util;

namespace CQSS.Pay.BLL.WeChatPay.Api
{
    public class MicroPay
    {
        /**
        * 刷卡支付完整业务流程逻辑
        * @param out_trade_no 商户订单号
        * @param total_fee 总金额
        * @param auth_code 支付授权码
        * @throws WxPayException
        * @return 刷卡支付结果
        */
        public static WxPayData Run(string out_trade_no, int total_fee, string auth_code)
        {
            WxPayLog.Info("MicroPay", "Micropay is processing...");

            WxPayData data = new WxPayData();
            data.SetValue("auth_code", auth_code);//授权码
            data.SetValue("body", string.Format("{0}订单:{1}", AppConfig.Global.WebSiteName, out_trade_no));//商品描述
            data.SetValue("total_fee", total_fee);//总金额
            data.SetValue("out_trade_no", out_trade_no);//商户订单号
            WxPayData result = new WxPayData();
            try
            {
                result = WxPayApi.Micropay(data); //提交被扫支付，接收返回结果

                //如果提交被扫支付接口调用失败，则抛异常
                if (!result.IsSet("return_code") || result.GetValue("return_code").ToString() == "FAIL")
                {
                    string returnMsg = result.IsSet("return_msg") ? result.GetValue("return_msg").ToString() : "";
                    WxPayLog.Error("MicroPay", "Micropay API interface call failure, result : " + result.ToXml());
                    throw new WxPayException("Micropay API interface call failure, return_msg : " + returnMsg);
                }

                //签名验证
                result.CheckSign();
                WxPayLog.Debug("MicroPay", "Micropay response check sign success");

                //刷卡支付直接成功
                if (result.GetValue("return_code").ToString() == "SUCCESS" &&
                    result.GetValue("result_code").ToString() == "SUCCESS")
                {
                    WxPayLog.Debug("MicroPay", "Micropay business success, result : " + result.ToXml());
                    return result;
                }
            }
            catch (Exception ex)
            {
                //如果不是HTTP请求超时，则抛出异常（背景：存在支付请求超时但微信已扣款的情况，需要查询支付记录才能确认是否已扣款）
                if (!HttpService.IsHttpTimeout(ex))
                    throw;
            }

            /******************************************************************
             * 剩下的都是接口调用成功，业务失败的情况
             * ****************************************************************/
            //1）业务结果明确失败
            if (result.IsSet("err_code") &&
                result.GetValue("err_code").ToString() != "USERPAYING" &&
                result.GetValue("err_code").ToString() != "SYSTEMERROR")
            {
                WxPayLog.Error("MicroPay", "micropay API interface call success, business failure, result : " + result.ToXml());
                return result;
            }

            //2）不能确定是否失败，需查单
            //用商户订单号去查单
            out_trade_no = data.GetValue("out_trade_no").ToString();
            bool success = false;
            WxPayData queryResult = Query(out_trade_no, out success);
            if (success)
            {
                return queryResult;
            }
            else if (queryResult == null)
            {
                //长时间未完成交易，则撤销订单
                WxPayLog.Error("MicroPay", "Micropay failure, Reverse order is processing...");
                if (!Cancel(out_trade_no))
                {
                    WxPayLog.Error("MicroPay", "Reverse order failure");
                    throw new WxPayException("Reverse order failure！");
                }
            }

            return result;
        }


        /**
         * 查询订单情况
         * @param string out_trade_no  商户订单号
         * @param bool success         支付结果：true表示支付成功，false表示支付失败
         * @return 订单查询接口返回的数据，参见协议接口
         */
        public static WxPayData Query(string out_trade_no, out bool success)
        {
            success = false;
            //确认支付是否成功,每隔一段时间查询一次订单，共查询10次
            int queryTimes = 0;//查询次数计数器
            while (queryTimes++ <= 10)
            {
                int succResult = 0;//查询结果
                WxPayData queryResult = Query(out_trade_no, out succResult);
                //如果需要继续查询，则等待2s后继续
                if (succResult == 2)
                {
                    Thread.Sleep(2000);
                    continue;
                }
                //查询成功,返回订单查询接口返回的数据
                else if (succResult == 1)
                {
                    WxPayLog.Debug("MicroPay", "Mircopay success, return order query result : " + queryResult.ToXml());
                    success = true;
                    return queryResult;
                }
                //订单交易失败，返回订单查询接口返回的数据
                else
                {
                    WxPayLog.Error("MicroPay", "Micropay failure, return order query result : " + queryResult.ToXml());
                    return queryResult;
                }
            }

            return null;
        }


        /**
	    * 
	    * 查询订单情况
	    * @param string out_trade_no  商户订单号
	    * @param int succCode         查询订单结果：0表示订单不成功，1表示订单成功，2表示继续查询
	    * @return 订单查询接口返回的数据，参见协议接口
	    */
        public static WxPayData Query(string out_trade_no, out int succCode)
        {
            WxPayData queryOrderInput = new WxPayData();
            queryOrderInput.SetValue("out_trade_no", out_trade_no);
            try
            {
                WxPayData result = WxPayApi.OrderQuery(queryOrderInput);

                if (result.GetValue("return_code").ToString() == "SUCCESS"
                    && result.GetValue("result_code").ToString() == "SUCCESS")
                {
                    //支付成功
                    if (result.GetValue("trade_state").ToString() == "SUCCESS")
                    {
                        succCode = 1;
                        return result;
                    }
                    //用户支付中，需要继续查询
                    else if (result.GetValue("trade_state").ToString() == "USERPAYING")
                    {
                        succCode = 2;
                        return result;
                    }
                }

                //如果返回错误码为“此交易订单号不存在”则直接认定失败
                if (result.GetValue("err_code").ToString() == "ORDERNOTEXIST")
                {
                    succCode = 0;
                }
                else
                {
                    //如果是系统错误，则后续继续
                    succCode = 2;
                }
                return result;
            }
            catch (Exception ex)
            {
                //如果非HTTP请求超时的异常，直接抛出；如果是，认为用户还在支付中
                if (!HttpService.IsHttpTimeout(ex))
                    throw;

                succCode = 2;
                return new WxPayData();
            }
        }


        /**
        * 
        * 撤销订单，如果失败会重复调用10次
        * @param string out_trade_no 商户订单号
        * @param depth 调用次数，这里用递归深度表示
        * @return false表示撤销失败，true表示撤销成功
        */
        public static bool Cancel(string out_trade_no, int depth = 0)
        {
            if (depth > 10)
            {
                return false;
            }

            WxPayData reverseInput = new WxPayData();
            reverseInput.SetValue("out_trade_no", out_trade_no);
            WxPayData result = WxPayApi.Reverse(reverseInput);

            //接口调用失败
            if (result.GetValue("return_code").ToString() != "SUCCESS")
            {
                return false;
            }

            //如果结果为success且不需要重新调用撤销，则表示撤销成功
            if (result.GetValue("result_code").ToString() != "SUCCESS" && result.GetValue("recall").ToString() == "N")
            {
                return true;
            }
            else if (result.GetValue("recall").ToString() == "Y")
            {
                return Cancel(out_trade_no, ++depth);
            }
            return false;
        }
    }
}