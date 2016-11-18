using CQSS.Pay.Util;
using ETSClient.com.allinpay.ets.client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CQSS.Pay.BLL.Allinpay.Api
{
    public class AllinpayCore
    {
        /// <summary>
        /// 支付参数签名（请勿修改本代码）
        /// </summary>
        /// <param name="data">通联支付数据</param>
        /// <returns></returns>
        public static string RequestSign(AllinpayData data)
        {
            RequestOrder requestOrder = new RequestOrder();
            requestOrder.setInputCharset(data.GetValue("inputCharset"));
            requestOrder.setPickupUrl(data.GetValue("pickupUrl"));
            requestOrder.setReceiveUrl(data.GetValue("receiveUrl"));
            requestOrder.setVersion(data.GetValue("version"));
            requestOrder.setLanguage(data.GetValue("language"));
            requestOrder.setSignType(data.GetValue("signType"));
            requestOrder.setMerchantId(data.GetValue("merchantId"));
            requestOrder.setPayerName(data.GetValue("payerName"));
            requestOrder.setPayerEmail(data.GetValue("payerEmail"));
            requestOrder.setPayerTelephone(data.GetValue("payerTelephone"));
            requestOrder.setPayerIDCard(data.GetValue("payerIDCard"));
            requestOrder.setPid(data.GetValue("pid"));
            requestOrder.setOrderNo(data.GetValue("orderNo"));
            requestOrder.setOrderAmount(data.GetValue("orderAmount"));
            requestOrder.setOrderCurrency(data.GetValue("orderCurrency"));
            requestOrder.setOrderDatetime(data.GetValue("orderDatetime"));
            requestOrder.setOrderExpireDatetime(data.GetValue("orderExpireDatetime"));
            requestOrder.setProductName(data.GetValue("productName"));
            requestOrder.setProductPrice(data.GetValue("productPrice"));
            requestOrder.setProductNum(data.GetValue("productNum"));
            requestOrder.setProductId(data.GetValue("productId"));
            requestOrder.setProductDesc(data.GetValue("productDesc"));
            requestOrder.setExt1(data.GetValue("ext1"));
            requestOrder.setExt2(data.GetValue("ext2"));
            requestOrder.setExtTL(data.GetValue("extTL"));
            requestOrder.setPayType(data.GetValue("payType"));
            requestOrder.setIssuerId(data.GetValue("issuerId"));
            requestOrder.setPan(data.GetValue("pan"));
            requestOrder.setTradeNature(data.GetValue("tradeNature"));
            requestOrder.setKey(AppConfig.AllinpayKey);
            string sign = requestOrder.doSign();
            return sign;
        }

        /// <summary>
        /// 退款参数签名（请勿修改本代码，特别是参数顺序）
        /// </summary>
        /// <param name="data">通联支付数据</param>
        /// <returns></returns>
        public static string RefundSign(AllinpayData data)
        {
            StringBuilder buf = new StringBuilder();
            StringUtil.appendSignPara(buf, "version", data.GetValue("version"));
            StringUtil.appendSignPara(buf, "signType", data.GetValue("signType"));
            StringUtil.appendSignPara(buf, "merchantId", data.GetValue("merchantId"));
            StringUtil.appendSignPara(buf, "orderNo", data.GetValue("orderNo"));
            StringUtil.appendSignPara(buf, "refundAmount", data.GetValue("refundAmount"));
            StringUtil.appendSignPara(buf, "orderDatetime", data.GetValue("orderDatetime"));
            StringUtil.appendLastSignPara(buf, "key", AppConfig.AllinpayKey);
            string sign = SecurityUtil.MD5Encode(buf.ToString());
            return sign;
        }

        /// <summary>
        /// 校验支付结果参数及签名是否合法（请勿修改本代码）
        /// </summary>
        /// <param name="data">通联支付数据</param>
        /// <returns></returns>
        public static bool VerifyResultSign(AllinpayData data)
        {
            PaymentResult paymentResult = new PaymentResult();
            paymentResult.setMerchantId(data.GetValue("merchantId"));
            paymentResult.setVersion(data.GetValue("version"));
            paymentResult.setLanguage(data.GetValue("language"));
            paymentResult.setSignType(data.GetValue("signType"));
            paymentResult.setPayType(data.GetValue("payType"));
            paymentResult.setIssuerId(data.GetValue("issuerId"));
            paymentResult.setPaymentOrderId(data.GetValue("paymentOrderId"));
            paymentResult.setOrderNo(data.GetValue("orderNo"));
            paymentResult.setOrderDatetime(data.GetValue("orderDatetime"));
            paymentResult.setOrderAmount(data.GetValue("orderAmount"));
            paymentResult.setPayDatetime(data.GetValue("payDatetime"));
            paymentResult.setPayAmount(data.GetValue("payAmount"));
            paymentResult.setExt1(data.GetValue("ext1"));
            paymentResult.setExt2(data.GetValue("ext2"));
            paymentResult.setPayResult(data.GetValue("payResult"));
            paymentResult.setErrorCode(data.GetValue("errorCode"));
            paymentResult.setReturnDatetime(data.GetValue("returnDatetime"));
            paymentResult.setKey(AppConfig.AllinpayKey);
            paymentResult.setSignMsg(data.GetValue("signMsg"));
            string certPath = Path.Combine(HttpContext.Current.Request.PhysicalApplicationPath, AppConfig.AllinpaySSLCertPath);
            paymentResult.setCertPath(certPath);
            bool verifyResult = paymentResult.verify();
            return verifyResult;
        }

        /// <summary>
        /// 校验退款响应参数及签名是否合法（请勿修改本代码，特别是参数顺序）
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool VerifyRefundSign(AllinpayData data)
        {
            StringBuilder buf = new StringBuilder();
            StringUtil.appendSignPara(buf, "merchantId", data.GetValue("merchantId"));
            StringUtil.appendSignPara(buf, "version", data.GetValue("version"));
            StringUtil.appendSignPara(buf, "signType", data.GetValue("signType"));
            StringUtil.appendSignPara(buf, "orderNo", data.GetValue("orderNo"));
            StringUtil.appendSignPara(buf, "orderAmount", data.GetValue("orderAmount"));
            StringUtil.appendSignPara(buf, "orderDatetime", data.GetValue("orderDatetime"));
            StringUtil.appendSignPara(buf, "refundAmount", data.GetValue("refundAmount"));
            StringUtil.appendSignPara(buf, "refundDatetime", data.GetValue("refundDatetime"));
            StringUtil.appendSignPara(buf, "refundResult", data.GetValue("refundResult"));
            StringUtil.appendSignPara(buf, "errorCode", data.GetValue("errorCode"));
            StringUtil.appendSignPara(buf, "returnDatetime", data.GetValue("returnDatetime"));
            StringUtil.appendLastSignPara(buf, "key", AppConfig.AllinpayKey);
            string sign = SecurityUtil.MD5Encode(buf.ToString());
            return data.GetValue("signMsg") == sign;
        }
    }
}
