using CQSS.Pay.BLL;
using CQSS.Pay.BLL.Basic;
using CQSS.Pay.BLL.Cache;
using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Model.Api;
using CQSS.Pay.Util;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CQSS.Pay.Web.Controllers
{
    [TestModeFilter]
    public class TestController : Controller
    {
        /// <summary>
        /// 测试appId
        /// </summary>
        private static readonly string _appId = AppConfig.Global.TestAppId;

        [HttpPost]
        public JsonResult PayNotify(int type, string appId, string sign, string data)
        {
            var checkResult = SignManager.CheckSign(appId, sign, data);
            if (checkResult.Status != ResultStatus.Success || !checkResult.Data)
                return Json(new { status = 0, msg = "签名验证未通过", type = type });

            var notifyInfo = JsonHelper.Deserialize<PayNotifyInfo>(data);
            if (string.IsNullOrWhiteSpace(notifyInfo.OrderId))
                return Json(new { status = 0, msg = "订单编码为空", data = data, type = type });

            if (string.IsNullOrWhiteSpace(notifyInfo.TradeNo))
                return Json(new { status = 0, msg = "交易流水号为空", data = data, type = type });

            if (string.IsNullOrWhiteSpace(notifyInfo.PaymentAmt))
                return Json(new { status = 0, msg = "支付金额为空", data = data, type = type });

            if (string.IsNullOrWhiteSpace(notifyInfo.Result))
                return Json(new { status = 0, msg = "支付结果为空", data = data, type = type });

            return Json(new { status = 1, type = type });
        }

        public ActionResult PayReturn(int type, string appId, string sign, string data)
        {
            var checkResult = SignManager.CheckSign(appId, sign, data);
            if (checkResult.Status != ResultStatus.Success || !checkResult.Data)
                return Json(new { status = 0, msg = "签名验证未通过", type = type });

            var notifyInfo = JsonHelper.Deserialize<PayNotifyInfo>(data);
            if (string.IsNullOrWhiteSpace(notifyInfo.OrderId))
                return Json(new { status = 0, msg = "订单编码为空", data = data, type = type });

            if (string.IsNullOrWhiteSpace(notifyInfo.PaymentAmt))
                return Json(new { status = 0, msg = "支付金额为空", data = data, type = type });

            if (string.IsNullOrWhiteSpace(notifyInfo.Result))
                return Json(new { status = 0, msg = "支付结果为空", data = data, type = type });

            return Json(new { status = 1, data = data, type = type });
        }

        [HttpPost]
        public JsonResult RefundNotify(int type, string appId, string sign, string data)
        {
            var checkResult = SignManager.CheckSign(appId, sign, data);
            if (checkResult.Status != ResultStatus.Success || !checkResult.Data)
                return Json(new { status = 0, msg = "签名验证未通过", data = data, type = type });

            var notifyInfo = JsonHelper.Deserialize<RefundNotifyInfo>(data);
            if (string.IsNullOrWhiteSpace(notifyInfo.OrderId))
                return Json(new { status = 0, msg = "订单编码为空", data = data, type = type });

            if (string.IsNullOrWhiteSpace(notifyInfo.TradeNo))
                return Json(new { status = 0, msg = "支付交易流水号为空", data = data, type = type });

            if (string.IsNullOrWhiteSpace(notifyInfo.RefundOrderId))
                return Json(new { status = 0, msg = "退款单编号为空", data = data, type = type });

            if (string.IsNullOrWhiteSpace(notifyInfo.RefundAmt))
                return Json(new { status = 0, msg = "退款金额为空", data = data, type = type });

            if (string.IsNullOrWhiteSpace(notifyInfo.Result))
                return Json(new { status = 0, msg = "退款结果为空", data = data, type = type });

            return Json(new { status = 1, data = data, type = type });
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Alipay(string orderId, string paymentAmt)
        {
            PayOrderInfo info = new PayOrderInfo()
            {
                OrderId = orderId,
                PaymentAmt = paymentAmt,
                OrderTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                NotifyUrl = string.Format("http://{0}/Test/PayNotify?type=0", AppConfig.Global.Domain),
                ReturnUrl = string.Format("http://{0}/Test/PayReturn?type=0", AppConfig.Global.Domain),
            };
            var setting = JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(_appId, data).Data;
            ViewBag.AppId = _appId;
            ViewBag.Sign = sign;
            ViewBag.Data = data;
            ViewBag.Title = "支付宝支付（测试）";
            ViewBag.Url = "/alipay/onlinepay";
            return View("Pay");
        }

        public ActionResult WeChatPay(string orderId, string paymentAmt)
        {
            PayOrderInfo info = new PayOrderInfo()
            {
                OrderId = orderId,
                PaymentAmt = paymentAmt,
                OrderTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                NotifyUrl = string.Format("http://{0}/Test/PayNotify?type=0", AppConfig.Global.Domain),
                ReturnUrl = string.Format("http://{0}/Test/PayReturn?type=0", AppConfig.Global.Domain),
            };
            var setting = JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(_appId, data).Data;
            ViewBag.AppId = _appId;
            ViewBag.Sign = sign;
            ViewBag.Data = data;
            ViewBag.Title = "微信支付（测试）";
            ViewBag.Url = "/wechatpay/onlinepay";
            return View("Pay");
        }

        public ActionResult WeChatBarcodePay(string orderId, string paymentAmt, string barcode)
        {
            PayOrderInfo info = new PayOrderInfo()
            {
                OrderId = orderId,
                PaymentAmt = paymentAmt,
                NotifyUrl = string.Format("http://{0}/Test/PayNotify?type=0", AppConfig.Global.Domain),
                Barcode = barcode,
            };
            var setting = JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(_appId, data).Data;
            ViewBag.AppId = _appId;
            ViewBag.Sign = sign;
            ViewBag.Data = data;
            ViewBag.Title = "微信支付（测试）";
            ViewBag.Url = "/wechatpay/barcodepay";
            return View("Pay");
        }

        public ActionResult WeChatPayRefund(string appId, string appSecret, string orderId)
        {
            if (appSecret != AppCache.GetAppSecret(appId))
                return Json(new { msg = "非法appSecret" });

            var resultInfo = PayResultDAL.GetValidPayResult(orderId, AppEnum.PayType.WeChatPay);
            if (resultInfo == null || resultInfo.SysNo <= 0)
                return Json(new { msg = "订单无支付记录" });

            var requestInfo = PayRequestDAL.GetPayRequest(resultInfo.RequestSysNo);
            var orderTime = JsonHelper.Deserialize<PayOrderInfo>(requestInfo.RequestData).OrderTime;
            RefundOrderInfo info = new RefundOrderInfo()
            {
                OrderId = orderId,
                OrderTime = orderTime ?? DateTime.Now.ToString("yyyyMMddHHmmss"),
                TradeNo = resultInfo.TradeNo,
                PaymentAmt = resultInfo.PaymentAmt.ToString(),
                RefundOrderId = DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                RefundAmt = resultInfo.PaymentAmt.ToString(),
                NotifyUrl = string.Format("http://{0}/Test/RefundNotify?type=0", AppConfig.Global.Domain),
            };
            var setting = JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(appId, data).Data;
            ViewBag.AppId = appId;
            ViewBag.Sign = sign;
            ViewBag.Data = data;
            ViewBag.Title = "微信支付退款（测试）";
            ViewBag.Url = "/wechatpay/syncrefund";
            return View("Pay");
        }

        public ActionResult Allinpay(string orderId, string paymentAmt)
        {
            PayOrderInfo info = new PayOrderInfo()
            {
                OrderId = orderId,
                PaymentAmt = paymentAmt,
                OrderTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                NotifyUrl = string.Format("http://{0}/Test/PayNotify?type=1", AppConfig.Global.Domain),
                ReturnUrl = string.Format("http://{0}/Test/PayReturn?type=0", AppConfig.Global.Domain),
            };
            var setting = JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(_appId, data).Data;
            ViewBag.AppId = _appId;
            ViewBag.Sign = sign;
            ViewBag.Data = data;
            ViewBag.Title = "通联支付（测试）";
            ViewBag.Url = "/allinpay/onlinepay";
            return View("Pay");
        }

        public ActionResult AllinpayRefund(string appId, string appSecret, string orderId)
        {
            if (appSecret != AppCache.GetAppSecret(appId))
                return Json(new { msg = "非法appSecret" });

            var resultInfo = PayResultDAL.GetValidPayResult(orderId, AppEnum.PayType.Allinpay);
            if (resultInfo == null || resultInfo.SysNo <= 0)
                return Json(new { msg = "订单无支付记录" });

            var requestInfo = PayRequestDAL.GetPayRequest(resultInfo.RequestSysNo);
            var orderTime = JsonHelper.Deserialize<PayOrderInfo>(requestInfo.RequestData).OrderTime;
            RefundOrderInfo info = new RefundOrderInfo()
            {
                OrderId = orderId,
                OrderTime = orderTime ?? DateTime.Now.ToString("yyyyMMddHHmmss"),
                TradeNo = resultInfo.TradeNo,
                PaymentAmt = resultInfo.PaymentAmt.ToString(),
                RefundOrderId = DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                RefundAmt = resultInfo.PaymentAmt.ToString(),
                NotifyUrl = string.Format("http://{0}/Test/RefundNotify?type=0", AppConfig.Global.Domain),
            };
            var setting = JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(appId, data).Data;
            ViewBag.AppId = appId;
            ViewBag.Sign = sign;
            ViewBag.Data = data;
            ViewBag.Title = "通联支付退款（测试）";
            ViewBag.Url = "/allinpay/syncrefund";
            return View("Pay");
        }

        public ActionResult SwiftPassWeChatPay(string orderId, string paymentAmt)
        {
            PayOrderInfo info = new PayOrderInfo()
            {
                OrderId = orderId,
                PaymentAmt = paymentAmt,
                OrderTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                NotifyUrl = string.Format("http://{0}/Test/PayNotify?type=0", AppConfig.Global.Domain),
                ReturnUrl = string.Format("http://{0}/Test/PayReturn?type=0", AppConfig.Global.Domain),
            };
            var setting = JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(_appId, data).Data;
            ViewBag.AppId = _appId;
            ViewBag.Sign = sign;
            ViewBag.Data = data;
            ViewBag.Title = "微信支付（测试）";
            ViewBag.Url = "/swiftpasswechatpay/onlinepay";
            return View("Pay");
        }
    }
}