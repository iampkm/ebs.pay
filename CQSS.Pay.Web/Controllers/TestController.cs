using CQSS.Pay.BLL.Basic;
using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Util;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CQSS.Pay.Web.Controllers
{
    public class TestController : Controller
    {
        /// <summary>
        /// 模拟在线支付（测试模式下可用）
        /// </summary>
        /// <returns></returns>
        public ActionResult SimulateOnlinePay(int payType, string sign, string data)
        {
            try
            {
                var type = (AppEnum.PayType)payType;

                #region 保存支付请求报文
                var payRequest = PayRequestManager.SavePayRequest(data, type);
                #endregion

                #region 校验签名
                var checkResult = SignManager.CheckSign(sign, data);
                if (checkResult.Status != ResultStatus.Success || !checkResult.Data)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = string.IsNullOrWhiteSpace(checkResult.Message) ? "签名验证未通过" : checkResult.Message;
                    PayRequestDAL.Update(payRequest);
                    return Content(payRequest.ResultDesc);
                }
                #endregion

                #region 获取本次支付请求的基本信息并更新至数据库
                payRequest = PayRequestManager.GetPayRequestInfo(payRequest, data);
                PayRequestDAL.Update(payRequest);
                #endregion

                #region 校验支付环境
                if (!AppConfig.IsTestMode)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = "非测试模式下，模拟支付不可用";
                    PayRequestDAL.Update(payRequest);
                    return Content(payRequest.ResultDesc);
                }
                #endregion

                #region 校验支付请求参数
                var validateResult = PayRequestManager.ValidateOnlinePayParams(data, type);
                if (validateResult.Status != ResultStatus.Success)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = validateResult.Message;
                    PayRequestDAL.Update(payRequest);
                    return Content(payRequest.ResultDesc);
                }

                PayOrderInfo orderInfo = validateResult.Data;
                #endregion

                #region 更新支付请求执行结果
                //更新支付请求记录的执行结果
                payRequest.ExecuteResult = (int)ResultStatus.Success;
                PayRequestDAL.Update(payRequest);

                //作废重复的支付请求记录
                PayRequestDAL.InvalidateRepeatRequest(payRequest.SysNo, payRequest.OrderId, type);
                #endregion


                #region 保存支付结果报文
                var payResult = PayResultManager.SavePayResult(null, type);
                #endregion

                #region 记录支付结果记录，并通知业务系统
                //商户订单号
                payResult.OrderId = payRequest.OrderId;
                //支付宝交易号
                payResult.TradeNo = DateTime.Now.ToString("yyyyMMddHHmmssfff") + RandomHelper.CreateRandomCode(3);
                //支付总金额
                payResult.PaymentAmt = payRequest.PaymentAmt;
                //支付结果
                payResult.ExecuteResult = (int)ResultStatus.Success;
                //对应的请求记录
                payResult.RequestSysNo = payRequest.SysNo;

                //更新支付结果记录信息
                PayResultDAL.Update(payResult);
                //通知各业务系统，订单支付成功
                PayResultManager.NotifyBack(payResult, payRequest);
                #endregion

                #region 返回支付结果页
                //支付完成通知对象
                var payNotify = new PayNotifyInfo();
                //商户订单号
                payNotify.OrderId = payResult.OrderId;
                //支付宝交易号
                payNotify.TradeNo = payResult.TradeNo;
                //支付总金额
                payNotify.PaymentAmt = payResult.PaymentAmt.ToString();
                //支付结果
                payNotify.Result = ((int)ResultStatus.Success).ToString();

                //如果业务系统有支付完成返回地址，则跳转到该地址去
                if (!string.IsNullOrWhiteSpace(payRequest.ReturnUrl))
                {
                    string resultData = JsonHelper.Serialize(payNotify);
                    string resultSign = SignManager.CreateSign(resultData).Data;
                    string returnUrl = payRequest.ReturnUrl + (payRequest.ReturnUrl.IndexOf("?") > 0 ? "&" : "?");
                    returnUrl += "sign=" + resultSign;
                    returnUrl += "&data=" + resultData;
                    return Redirect(returnUrl);
                }
                #endregion

                return Content("支付成功");
            }
            catch (Exception ex)
            {
                string log = string.Format(@"模拟在线支付发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：sign={5} data={6}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, sign, data);
                LogWriter.WriteLog(log, "Test", ExceptionHelper.ExceptionLevel.Exception);
                return Content("系统执行时发生异常：" + ex.Message);
            }
        }

        [HttpPost]
        public JsonResult PayNotify(int type, string sign, string data)
        {
            var checkResult = SignManager.CheckSign(sign, data);
            if (checkResult.Status != ResultStatus.Success || !checkResult.Data)
                return Json(new { status = 0, msg = "签名验证未通过", type = type });

            PayNotifyInfo info = JsonHelper.Deserialize<PayNotifyInfo>(data);
            if (string.IsNullOrWhiteSpace(info.OrderId))
                return Json(new { status = 0, msg = "订单编码为空", type = type });

            if (string.IsNullOrWhiteSpace(info.TradeNo))
                return Json(new { status = 0, msg = "交易流水号为空", type = type });

            if (string.IsNullOrWhiteSpace(info.PaymentAmt))
                return Json(new { status = 0, msg = "支付金额为空", type = type });

            if (string.IsNullOrWhiteSpace(info.Result))
                return Json(new { status = 0, msg = "支付结果为空", type = type });

            return Json(new { status = 1, type = type });
        }

        public ActionResult PayReturn(int type, string sign, string data)
        {
            var checkResult = SignManager.CheckSign(sign, data);
            if (checkResult.Status != ResultStatus.Success || !checkResult.Data)
                return Json(new { status = 0, msg = "签名验证未通过", type = type });

            PayNotifyInfo info = JsonHelper.Deserialize<PayNotifyInfo>(data);
            if (string.IsNullOrWhiteSpace(info.OrderId))
                return Json(new { status = 0, msg = "订单编码为空", type = type });

            if (string.IsNullOrWhiteSpace(info.PaymentAmt))
                return Json(new { status = 0, msg = "支付金额为空", type = type });

            if (string.IsNullOrWhiteSpace(info.Result))
                return Json(new { status = 0, msg = "支付结果为空", type = type });

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
                NotifyUrl = string.Format("http://{0}/Test/PayNotify?type=0", AppConfig.Domain),
                ReturnUrl = string.Format("http://{0}/Test/PayReturn?type=0", AppConfig.Domain),
            };
            var setting = CQSS.Pay.Util.Helper.JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = CQSS.Pay.Util.Helper.JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(data).Data;
            ViewBag.Data = data;
            ViewBag.Sign = sign;
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
                NotifyUrl = string.Format("http://{0}/Test/PayNotify?type=0", AppConfig.Domain),
                ReturnUrl = string.Format("http://{0}/Test/PayReturn?type=0", AppConfig.Domain),
            };
            var setting = CQSS.Pay.Util.Helper.JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = CQSS.Pay.Util.Helper.JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(data).Data;
            ViewBag.Data = data;
            ViewBag.Sign = sign;
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
                Barcode = barcode,
            };
            var setting = CQSS.Pay.Util.Helper.JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = CQSS.Pay.Util.Helper.JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(data).Data;
            ViewBag.Data = data;
            ViewBag.Sign = sign;
            ViewBag.Title = "微信支付（测试）";
            ViewBag.Url = "/wechatpay/barcodepay";
            return View("Pay");
        }

        public ActionResult WeChatPayRefund(string signKey, string orderId)
        {
            if (signKey != CQSS.Pay.Util.AppConfig.SignKey)
                return Json(new { msg = "非法signKey" });

            var payResult = DbHelper.QuerySingle<PayResultInfo>(string.Format("SELECT * FROM Pay_Result WHERE OrderId='{0}' AND ExecuteResult=1", orderId));
            if (payResult == null || payResult.SysNo <= 0)
                return Json(new { msg = "订单无支付记录" });

            var payRequest = PayRequestDAL.GetPayRequest(payResult.RequestSysNo);
            var orderTime = JsonHelper.Deserialize<PayOrderInfo>(payRequest.RequestData).OrderTime;
            RefundOrderInfo info = new RefundOrderInfo()
            {
                OrderId = orderId,
                OrderTime = orderTime ?? DateTime.Now.ToString("yyyyMMddHHmmss"),
                TradeNo = payResult.TradeNo,
                PaymentAmt = payResult.PaymentAmt.ToString(),
                RefundOrderId = DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                RefundAmt = payResult.PaymentAmt.ToString(),
            };
            var setting = CQSS.Pay.Util.Helper.JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = CQSS.Pay.Util.Helper.JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(data).Data;
            ViewBag.Data = data;
            ViewBag.Sign = sign;
            ViewBag.Title = "微信支付退款（测试）";
            ViewBag.Url = "/wechatpay/onlinepayrefund";
            return View("Pay");
        }

        public ActionResult Allinpay(string orderId, string paymentAmt)
        {
            PayOrderInfo info = new PayOrderInfo()
            {
                OrderId = orderId,
                PaymentAmt = paymentAmt,
                OrderTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                NotifyUrl = string.Format("http://{0}/Test/PayNotify?type=1", AppConfig.Domain),
                ReturnUrl = string.Format("http://{0}/Test/PayReturn?type=0", AppConfig.Domain),
                CrossboardType = "1",
                GoodsPrice = "0.01",
                TaxPrice = "0.00",
                FreightPrice = "0.00",
                BuyerName = "张三",
                BuyerCellphone = "13066669999",
                BuyerIdCardNo = "50002319800101783X",
                SystemId = "1"
            };
            var setting = CQSS.Pay.Util.Helper.JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = CQSS.Pay.Util.Helper.JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(data).Data;
            ViewBag.Data = data;
            ViewBag.Sign = sign;
            ViewBag.Title = "通联支付（测试）";
            ViewBag.Url = "/Allinpay/onlinepay";
            return View("Pay");
        }

        public ActionResult AllinpayNormal(string orderId, string paymentAmt)
        {
            PayOrderInfo info = new PayOrderInfo()
            {
                OrderId = orderId,
                PaymentAmt = paymentAmt,
                OrderTime = DateTime.Now.ToString("yyyyMMddHHmmss"),
                NotifyUrl = string.Format("http://{0}/Test/PayNotify?type=1", AppConfig.Domain),
                ReturnUrl = string.Format("http://{0}/Test/PayReturn?type=0", AppConfig.Domain),
            };
            var setting = CQSS.Pay.Util.Helper.JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = CQSS.Pay.Util.Helper.JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(data).Data;
            ViewBag.Data = data;
            ViewBag.Sign = sign;
            ViewBag.Title = "通联支付（测试）";
            ViewBag.Url = "/Allinpay/onlinepay";
            return View("Pay");
        }

        public ActionResult AllinpayRefund(string signKey, string orderId)
        {
            if (signKey != CQSS.Pay.Util.AppConfig.SignKey)
                return Json(new { msg = "非法signKey" });

            var payResult = DbHelper.QuerySingle<PayResultInfo>(string.Format("SELECT * FROM Pay_Result WHERE OrderId='{0}' AND ExecuteResult=1", orderId));
            if (payResult == null || payResult.SysNo <= 0)
                return Json(new { msg = "订单无支付记录" });

            var payRequest = PayRequestDAL.GetPayRequest(payResult.RequestSysNo);
            var orderTime = JsonHelper.Deserialize<PayOrderInfo>(payRequest.RequestData).OrderTime;
            RefundOrderInfo info = new RefundOrderInfo()
            {
                OrderId = orderId,
                OrderTime = orderTime ?? DateTime.Now.ToString("yyyyMMddHHmmss"),
                TradeNo = payResult.TradeNo,
                PaymentAmt = payResult.PaymentAmt.ToString(),
                RefundOrderId = DateTime.Now.ToString("yyyyMMddHHmmssfff"),
                RefundAmt = payResult.PaymentAmt.ToString(),
            };
            var setting = CQSS.Pay.Util.Helper.JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = CQSS.Pay.Util.Helper.JsonHelper.Serialize(info, setting);
            string sign = SignManager.CreateSign(data).Data;
            ViewBag.Data = data;
            ViewBag.Sign = sign;
            ViewBag.Title = "通联支付退款（测试）";
            ViewBag.Url = "/allinpay/onlinepayrefund";
            return View("Pay");
        }
    }
}