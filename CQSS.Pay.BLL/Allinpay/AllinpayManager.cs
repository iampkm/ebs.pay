using CQSS.Pay.BLL.Allinpay.Api;
using CQSS.Pay.BLL.Basic;
using CQSS.Pay.Model;
using CQSS.Pay.Util;
using CQSS.Pay.Util.Helper;
using ETSClient.com.allinpay.ets.client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Allinpay
{
    public class AllinpayManager
    {
        /// <summary>
        /// 创建支付提交的表单
        /// </summary>
        /// <param name="orderInfo"></param>
        /// <returns></returns>
        public static string BuildRequestForm(PayOrderInfo orderInfo)
        {
            var data = new AllinpayData(AllinpayDataType.PayRequest);
            //页面跳转同步通知页面路径
            data.SetValue("pickupUrl", string.Format("http://{0}/Allinpay/OnlinePayReturn", AppConfig.Domain));
            //服务器接受支付结果的后台地址
            data.SetValue("receiveUrl", string.Format("http://{0}/Allinpay/OnlinePayNotify", AppConfig.Domain));
            //商户号
            data.SetValue("merchantId", AppConfig.AllinpayMerchantId);
            //付款人姓名
            if (!string.IsNullOrWhiteSpace(orderInfo.BuyerName))
                data.SetValue("payerName", orderInfo.BuyerName.Trim());
            //付款人电话联系方式
            if (!string.IsNullOrWhiteSpace(orderInfo.BuyerCellphone))
                data.SetValue("payerTelephone", orderInfo.BuyerCellphone.Trim());
            //付款人类型及证件号
            if (!string.IsNullOrWhiteSpace(orderInfo.BuyerIdCardNo))
                data.SetValue("payerIDCard", SecurityUtil.MD5Encode("01" + orderInfo.BuyerIdCardNo.Trim()));
            //商户订单号
            data.SetValue("orderNo", orderInfo.OrderId.Trim());
            //商户订单金额
            data.SetValue("orderAmount", ((int)(Convert.ToDecimal(orderInfo.PaymentAmt) * 100)).ToString());
            //商户订单提交时间
            data.SetValue("orderDatetime", orderInfo.OrderTime);

            int diffMinutes = (int)((DateTime.ParseExact(orderInfo.OrderTime, "yyyyMMddHHmmss", new CultureInfo("zh-CN", true)).AddHours(24) - DateTime.Now).TotalMinutes);
            //订单过期时间
            data.SetValue("orderExpireDatetime", Math.Max(diffMinutes, 1).ToString());//默认下单后的24小时
            if (orderInfo.CrossboardType == "1")
            {
                int goodsPrice = (int)(Convert.ToDecimal(orderInfo.GoodsPrice) * 100);//单位为“分”，所以要乘以100
                int taxPrice = (int)(Convert.ToDecimal(orderInfo.TaxPrice) * 100);//单位为“分”，所以要乘以100
                data.SetCustomsExt(AppConfig.AllinpayEshopEntCode, AppConfig.AllinpayEshopEntName, goodsPrice, taxPrice);
            }
            //签名字符串
            data.SetValue("signMsg", AllinpayCore.RequestSign(data));
            //建立请求
            string formHtml = data.ToForm(AppConfig.AllinpayServerUrl, "post");
            return formHtml;
        }

        /// <summary>
        /// 创建支付提交的表单
        /// </summary>
        /// <param name="orderInfo"></param>
        /// <returns></returns>
        public static string BuildWapRequestForm(PayOrderInfo orderInfo)
        {
            var data = new AllinpayData(AllinpayDataType.PayRequest);
            //页面跳转同步通知页面路径
            data.SetValue("pickupUrl", string.Format("http://{0}/Allinpay/OnlinePayReturn", AppConfig.Domain));
            //服务器接受支付结果的后台地址
            data.SetValue("receiveUrl", string.Format("http://{0}/Allinpay/OnlinePayNotify", AppConfig.Domain));
            //商户号
            data.SetValue("merchantId", AppConfig.AllinpayMerchantId);
            //付款人姓名
            if (!string.IsNullOrWhiteSpace(orderInfo.BuyerName))
                data.SetValue("payerName", orderInfo.BuyerName.Trim());
            //付款人电话联系方式
            if (!string.IsNullOrWhiteSpace(orderInfo.BuyerCellphone))
                data.SetValue("payerTelephone", orderInfo.BuyerCellphone.Trim());
            //付款人类型及证件号
            if (!string.IsNullOrWhiteSpace(orderInfo.BuyerIdCardNo))
                data.SetValue("payerIDCard", SecurityUtil.MD5Encode("01" + orderInfo.BuyerIdCardNo.Trim()));
            //商户订单号
            data.SetValue("orderNo", orderInfo.OrderId.Trim());
            //商户订单金额
            data.SetValue("orderAmount", ((int)(Convert.ToDecimal(orderInfo.PaymentAmt) * 100)).ToString());
            //商户订单提交时间
            data.SetValue("orderDatetime", orderInfo.OrderTime);

            int diffMinutes = (int)((DateTime.ParseExact(orderInfo.OrderTime, "yyyyMMddHHmmss", new CultureInfo("zh-CN", true)).AddHours(24) - DateTime.Now).TotalMinutes);
            //订单过期时间
            data.SetValue("orderExpireDatetime", Math.Max(diffMinutes, 1).ToString());//默认下单后的24小时
            //支付方式
            data.SetValue("payType", "0");//接入手机网关时，该值填固定填0
            //发卡方代码
            data.SetValue("issuerId", "");//payType为0时，issuerId必须为空
            if (orderInfo.CrossboardType == "1")
            {
                int goodsPrice = (int)(Convert.ToDecimal(orderInfo.GoodsPrice) * 100);//单位为“分”，所以要乘以100
                int taxPrice = (int)(Convert.ToDecimal(orderInfo.TaxPrice) * 100);//单位为“分”，所以要乘以100
                data.SetCustomsExt(AppConfig.AllinpayEshopEntCode, AppConfig.AllinpayEshopEntName, goodsPrice, taxPrice);
            }
            //签名字符串
            data.SetValue("signMsg", AllinpayCore.RequestSign(data));
            //建立请求
            string formHtml = data.ToForm(AppConfig.AllinpayWapServerUrl, "post");
            return formHtml;
        }

        /// <summary>
        /// 发起退款请求
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ExecuteResult<AllinpayData> RefundRequest(AllinpayData data)
        {
            var result = new ExecuteResult<AllinpayData>();
            string param = data.ToUrl();
            string response = HttpHelper.HttpRequest("post", AppConfig.AllinpayRefundServerUrl, param);
            if (response == null)
            {
                result.Status = ResultStatus.Failure;
                result.Message = "请求发送异常";
                return result;
            }
            var responseData = new AllinpayData(AllinpayDataType.RefundResponse);
            responseData.FromUrl(response);
            result.Data = responseData;
            if (responseData.HasValue("ERRORCODE") || responseData.HasValue("ERRORMSG"))
            {
                LogWriter.WriteLog(data.GetValue("orderNo") + "退款响应数据：" + response, AppConfig.AllinpayLogFolder);
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("{0}（错误码：{1}）", responseData.GetValue("ERRORMSG"), responseData.GetValue("ERRORCODE"));
                return result;
            }
            if (!AllinpayCore.VerifyRefundSign(responseData))
            {
                LogWriter.WriteLog(data.GetValue("orderNo") + "退款响应数据：" + response, AppConfig.AllinpayLogFolder);
                result.Status = ResultStatus.Failure;
                result.Message = "退款响应数据签名校验未通过";
                return result;
            }
            //退款结果（成功：20  其他为失败）
            if (responseData.GetValue("refundResult") != "20")
            {
                LogWriter.WriteLog(data.GetValue("orderNo") + "退款响应数据：" + response, AppConfig.AllinpayLogFolder);
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("退款失败（错误码：{0}）", responseData.GetValue("errorCode"));
                return result;
            }
            result.Status = ResultStatus.Success;
            return result;
        }
    }
}
