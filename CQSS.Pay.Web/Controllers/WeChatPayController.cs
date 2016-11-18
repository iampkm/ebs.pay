using CQSS.Pay.BLL.Alipay;
using CQSS.Pay.BLL.Basic;
using CQSS.Pay.BLL.WeChatPay.Api;
using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Util;
using CQSS.Pay.Util.Helper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace CQSS.Pay.Web.Controllers
{
    /// <summary>
    /// 微信支付
    /// </summary>
    public class WeChatPayController : Controller
    {
        /// <summary>
        /// 支付方式（微信支付）
        /// </summary>
        private AppEnum.PayType _payType = AppEnum.PayType.WeChatPay;

        #region 在线支付
        /// <summary>
        /// PC在线支付
        /// </summary>
        /// <param name="sign"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ActionResult OnlinePay(string sign, string data)
        {
            try
            {
                #region 校验支付模式
                if (AppConfig.IsTestMode)
                    return RedirectToActionPermanent("SimulateOnlinePay", "Test", new { payType = (int)this._payType, sign = sign, data = data });
                #endregion

                //移动端访问，跳转至微信Wap支付
                if (RequestHelper.IsWapRequest())
                    return RedirectToAction("OnlineWapPay", new { sign = sign, data = data });

                #region 保存请求报文
                var payRequest = PayRequestManager.SavePayRequest(data, this._payType);
                #endregion

                #region 校验签名
                var checkResult = SignManager.CheckSign(sign, data);
                if (checkResult.Status != ResultStatus.Success || !checkResult.Data)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = string.IsNullOrWhiteSpace(checkResult.Message) ? "签名验证未通过" : checkResult.Message;
                    PayRequestDAL.Update(payRequest);
                    ViewBag.ErrorMsg = payRequest.ResultDesc;
                    return View();
                }
                #endregion

                #region 获取支付请求参数并更新至数据库
                payRequest = PayRequestManager.GetPayRequestInfo(payRequest, data);
                PayRequestDAL.Update(payRequest);
                #endregion

                #region 校验支付请求参数
                var validateResult = PayRequestManager.ValidateOnlinePayParams(data, this._payType);
                if (validateResult.Status != ResultStatus.Success)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = validateResult.Message;
                    PayRequestDAL.Update(payRequest);
                    ViewBag.ErrorMsg = payRequest.ResultDesc;
                    return View();
                }

                PayOrderInfo orderInfo = validateResult.Data;
                #endregion

                #region 生成支付二维码链接code_url
                try
                {
                    NativePay nativePay = new NativePay();
                    //获取订单支付金额
                    int paymentAmt = (int)(payRequest.PaymentAmt * 100);//微信支付金额的单位为“分”，所以要乘以100
                    //异步通知url
                    string notifyUrl = string.Format("http://{0}/WeChatPay/OnlinePayNotify", AppConfig.Domain);

                    DateTime orderEndTime = DateTime.ParseExact(orderInfo.OrderTime, "yyyyMMddHHmmss", new CultureInfo("zh-CN", true)).AddHours(24);
                    DateTime minExpireTime = DateTime.Now.AddMinutes(6);//为保险，多加1分钟
                    //交易过期时间（最短过期时间间隔必须大于5分钟）
                    string expireTime = (orderEndTime > minExpireTime ? orderEndTime : minExpireTime).ToString("yyyyMMddHHmmss");
                    //向微信发起统一下单请求
                    string codeUrl = nativePay.GetPayUrl(payRequest.OrderId, paymentAmt, notifyUrl, expireTime);

                    ViewBag.CodeUrl = codeUrl;
                    ViewBag.PayRequest = payRequest;
                }
                catch (WxPayException wex)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = wex.Message;
                    PayRequestDAL.Update(payRequest);
                    ViewBag.ErrorMsg = payRequest.ResultDesc;
                    return View();
                }
                #endregion

                #region 更新支付请求执行结果
                payRequest.ExecuteResult = (int)ResultStatus.Success;
                payRequest.Status = (int)AppEnum.GlobalStatus.Valid;
                //更新支付请求记录的执行结果
                PayRequestDAL.Update(payRequest);

                //作废重复的支付请求记录
                PayRequestDAL.InvalidateRepeatRequest(payRequest.SysNo, payRequest.OrderId, this._payType);
                #endregion
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMsg = "系统执行时发生异常：" + ex.Message;
                string log = string.Format(@"PC支付请求发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：sign={5} data={6}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, sign, data);
                LogWriter.WriteLog(log, AppConfig.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
            }
            return View();
        }

        /// <summary>
        /// WAP在线支付
        /// </summary>
        /// <param name="sign"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ActionResult OnlineWapPay(string sign, string data)
        {
            try
            {
                #region 保存请求报文
                var payRequest = PayRequestManager.SavePayRequest(data, this._payType);
                #endregion

                #region 校验签名
                var checkResult = SignManager.CheckSign(sign, data);
                if (checkResult.Status != ResultStatus.Success || !checkResult.Data)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = string.IsNullOrWhiteSpace(checkResult.Message) ? "签名验证未通过" : checkResult.Message;
                    PayRequestDAL.Update(payRequest);
                    ViewBag.ErrorMsg = payRequest.ResultDesc;
                    return View();
                }
                #endregion

                #region 获取支付请求参数并更新至数据库
                payRequest = PayRequestManager.GetPayRequestInfo(payRequest, data);
                PayRequestDAL.Update(payRequest);
                #endregion

                #region 校验支付环境
                //非微信环境访问
                if (!RequestHelper.IsWapRequest() || !RequestHelper.IsWeChatRequest())
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = "非微信环境中不能使用微信支付，请更换其他支付方式";
                    PayRequestDAL.Update(payRequest);
                    ViewBag.ErrorMsg = payRequest.ResultDesc;
                    return View();
                }
                #endregion

                #region 校验支付请求参数
                var validateResult = PayRequestManager.ValidateOnlinePayParams(data, this._payType);
                if (validateResult.Status != ResultStatus.Success)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = validateResult.Message;
                    PayRequestDAL.Update(payRequest);
                    ViewBag.ErrorMsg = payRequest.ResultDesc;
                    return View();
                }
                #endregion

                #region 跳转到微信授权系统
                string redirect_uri = HttpUtility.UrlEncode(string.Format("http://{0}/WeChatPay/OnlineWapPayJSAPI?requestSysNo={1}", AppConfig.Domain, payRequest.SysNo));
                WxPayData reqData = new WxPayData();
                reqData.SetValue("appid", WxPayConfig.APPID);
                reqData.SetValue("redirect_uri", redirect_uri);
                reqData.SetValue("response_type", "code");
                reqData.SetValue("scope", "snsapi_base");
                reqData.SetValue("state", "STATE" + "#wechat_redirect");
                string url = "https://open.weixin.qq.com/connect/oauth2/authorize?" + reqData.ToUrl();
                return Redirect(url);
                #endregion
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMsg = "系统执行时发生异常：" + ex.Message;
                string log = string.Format(@"WAP支付请求发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：sign={5} data={6}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, sign, data);
                LogWriter.WriteLog(log, AppConfig.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
            }
            return View();
        }

        /// <summary>
        /// WAP在线支付JSPAI页面
        /// </summary>
        /// <param name="requestSysNo"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public ActionResult OnlineWapPayJSAPI(int requestSysNo, string code)
        {
            try
            {
                #region 校验支付环境
                //非微信环境访问
                if (!RequestHelper.IsWapRequest() || !RequestHelper.IsWeChatRequest())
                {
                    ViewBag.ErrorMsg = "非微信环境中不能使用微信支付，请更换其他支付方式";
                    return View();
                }
                #endregion

                #region 获取支付请求记录
                var payRequest = PayRequestDAL.GetPayRequest(requestSysNo);
                if (payRequest == null || payRequest.SysNo <= 0)
                {
                    ViewBag.ErrorMsg = "您尚未发起支付请求，请返回后重新提交";
                    return View();
                }
                #endregion

                #region 组装浏览器调起JS API支付所需的参数
                try
                {
                    JsApiPay jsApiPay = new JsApiPay(System.Web.HttpContext.Current);
                    //获取授权用户信息
                    jsApiPay.GetOpenidAndAccessTokenFromCode(code);
                    //获取订单支付金额
                    int paymentAmt = (int)(payRequest.PaymentAmt * 100);//微信支付金额的单位为“分”，所以要乘以100
                    //异步通知url
                    string notifyUrl = string.Format("http://{0}/WeChatPay/OnlinePayNotify", AppConfig.Domain);

                    var orderInfo = JsonHelper.Deserialize<PayOrderInfo>(payRequest.RequestData);
                    DateTime orderEndTime = DateTime.ParseExact(orderInfo.OrderTime, "yyyyMMddHHmmss", new CultureInfo("zh-CN", true)).AddHours(24);
                    DateTime minExpireTime = DateTime.Now.AddMinutes(6);//为保险，多加1分钟
                    //交易过期时间（最短过期时间间隔必须大于5分钟）
                    string expireTime = (orderEndTime > minExpireTime ? orderEndTime : minExpireTime).ToString("yyyyMMddHHmmss");
                    //向微信发起统一下单请求
                    jsApiPay.GetUnifiedOrderResult(payRequest.OrderId, paymentAmt, notifyUrl, expireTime);
                    //获取调起JS API的参数
                    ViewBag.JsApiParams = jsApiPay.GetJsApiParameters();
                    //订单编号
                    ViewBag.OrderId = payRequest.OrderId;

                    //异常时返回的业务系统着陆页面
                    string urlFormat = payRequest.ReturnUrl + (payRequest.ReturnUrl.IndexOf("?") > 0 ? "&" : "?") + "sign={0}&&data={1}";
                    var payNotify = new PayNotifyInfo()
                    {
                        OrderId = payRequest.OrderId,
                        TradeNo = "",
                        PaymentAmt = payRequest.PaymentAmt.ToString(),
                        Result = ((int)ResultStatus.Error).ToString()
                    };
                    string data = JsonHelper.Serialize(payNotify);
                    string sign = SignManager.CreateSign(data).Data;
                    ViewBag.ReturnUrl = string.Format(urlFormat, sign, data);
                }
                catch (WxPayException wex)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = wex.Message;
                    PayRequestDAL.Update(payRequest);
                    ViewBag.ErrorMsg = payRequest.ResultDesc;
                    return View();
                }
                #endregion

                #region 更新支付请求执行结果
                payRequest.ExecuteResult = (int)ResultStatus.Success;
                payRequest.Status = (int)AppEnum.GlobalStatus.Valid;
                //更新支付请求记录的执行结果
                PayRequestDAL.Update(payRequest);

                //作废重复的支付请求记录
                PayRequestDAL.InvalidateRepeatRequest(payRequest.SysNo, payRequest.OrderId, this._payType);
                #endregion
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMsg = "系统执行时发生异常：" + ex.Message;
                string log = string.Format(@"WAP支付JSAPI发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：requestSysNo={5} code={6}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, requestSysNo, code);
                LogWriter.WriteLog(log, AppConfig.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
            }
            return View();
        }

        /// <summary>
        /// 支付完成异步回执
        /// </summary>
        /// <returns></returns>
        public string OnlinePayNotify()
        {
            //请求参数报文
            string requestParams = string.Empty;
            //返回结果
            WxPayData result = new WxPayData();
            try
            {
                #region 保存请求报文
                StringBuilder builder = new StringBuilder();
                using (System.IO.Stream s = Request.InputStream)
                {
                    int count = 0;
                    byte[] buffer = new byte[1024];
                    while ((count = s.Read(buffer, 0, 1024)) > 0)
                    {
                        builder.Append(Encoding.UTF8.GetString(buffer, 0, count));
                    }
                }
                requestParams = builder.ToString();
                var payResult = PayResultManager.SavePayResult(requestParams, _payType);
                #endregion

                #region 校验请求报文
                //转换数据格式并验证签名
                WxPayData notifyData = new WxPayData();
                try
                {
                    notifyData.FromXml(requestParams);
                }
                catch (WxPayException ex)
                {
                    payResult.ExecuteResult = (int)ResultStatus.Failure;
                    payResult.ResultDesc = ex.Message;
                    PayResultDAL.Update(payResult);
                    result.SetValue("return_code", "FAIL");
                    result.SetValue("return_msg", payResult.ResultDesc);
                    return result.ToXml();
                }

                //检查支付结果中transaction_id是否存在
                if (!notifyData.IsSet("transaction_id"))
                {
                    payResult.ExecuteResult = (int)ResultStatus.Failure;
                    payResult.ResultDesc = "支付结果中微信支付单号不存在";
                    PayResultDAL.Update(payResult);
                    result.SetValue("return_code", "FAIL");
                    result.SetValue("return_msg", payResult.ResultDesc);
                    return result.ToXml();
                }

                //查询支付单，判断支付单真实性
                payResult.TradeNo = notifyData.GetValue("transaction_id").ToString();
                WxPayData req = new WxPayData();
                req.SetValue("transaction_id", payResult.TradeNo);
                WxPayData queryData = WxPayApi.OrderQuery(req);
                if (queryData.GetValue("return_code").ToString() != "SUCCESS" || queryData.GetValue("result_code").ToString() != "SUCCESS")
                {
                    payResult.ExecuteResult = (int)ResultStatus.Failure;
                    payResult.ResultDesc = "微信支付单查询失败";
                    PayResultDAL.Update(payResult);
                    result.SetValue("return_code", "FAIL");
                    result.SetValue("return_msg", payResult.ResultDesc);
                    return result.ToXml();
                }
                #endregion

                #region 记录支付结果记录，并通知业务系统
                //商户订单号
                payResult.OrderId = notifyData.GetValue("out_trade_no").ToString();
                //支付宝交易号
                payResult.TradeNo = notifyData.GetValue("transaction_id").ToString();
                //支付总金额
                payResult.PaymentAmt = Convert.ToDecimal(notifyData.GetValue("total_fee")) / 100; //微信支付金额的单位为“分”，所以要除以100
                //订单交易成功
                payResult.ExecuteResult = (int)ResultStatus.Success;

                //判断是否存在有效的支付结果记录，如果存在，则当前支付结果记录不执行
                bool exist = PayResultDAL.ExistValidPayResult(payResult.OrderId, payResult.TradeNo, this._payType);
                if (exist)
                {
                    payResult.ExecuteResult = (int)ResultStatus.Failure;
                    payResult.ResultDesc = "已存在有效的支付结果记录";
                }

                PayRequestInfo payRequest = null;
                if (payResult.ExecuteResult == (int)ResultStatus.Success)
                {
                    payRequest = PayRequestDAL.GetValidPayRequest(payResult.OrderId, this._payType);
                    if (payRequest != null && payRequest.SysNo > 0)
                    {
                        payResult.RequestSysNo = payRequest.SysNo;
                    }
                }

                //更新支付结果记录信息
                PayResultDAL.Update(payResult);
                //通知各业务系统，订单支付成功
                PayResultManager.NotifyBack(payResult, payRequest);
                #endregion

                result.SetValue("return_code", "SUCCESS");
                result.SetValue("return_msg", "OK");
                return result.ToXml();
            }
            catch (Exception ex)
            {
                string log = string.Format(@"支付异步通知发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：{5}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, requestParams);
                LogWriter.WriteLog(log, AppConfig.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                result.SetValue("return_code", "FAIL");
                result.SetValue("return_msg", ex.Message);
                return result.ToXml();
            }
        }

        /// <summary>
        /// 校验支付结果
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="resultType"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CheckPay(string orderId, string resultType)
        {
            try
            {
                var payRequest = PayRequestDAL.GetValidPayRequest(orderId, this._payType);
                if (payRequest == null || payRequest.SysNo <= 0)
                    return Json(new { status = -2, msg = "系统不存在该订单的支付请求，请自行返回下单页面！" });

                int status = 0;
                string urlFormat = payRequest.ReturnUrl + (payRequest.ReturnUrl.IndexOf("?") > 0 ? "&" : "?") + "sign={0}&&data={1}";
                var payNotify = new PayNotifyInfo()
                {
                    OrderId = payRequest.OrderId,
                    TradeNo = "",
                    PaymentAmt = payRequest.PaymentAmt.ToString()
                };
                var payResult = PayResultDAL.GetValidPayResult(orderId, this._payType);
                if (payResult == null || payResult.SysNo <= 0)
                {
                    status = 0;//无支付记录
                    payNotify.Result = ((int)ResultStatus.Failure).ToString();
                }
                else
                {
                    status = 1;//有支付记录
                    payNotify.TradeNo = payResult.TradeNo;
                    payNotify.Result = ((int)ResultStatus.Success).ToString();
                }

                //当支付页面收到的支付结果为成功时，不管有没有收到支付成功的异步回执，返回到支付结果着陆页时都提示支付成功
                if (string.Equals(resultType, "ok", StringComparison.CurrentCultureIgnoreCase))
                {
                    payNotify.Result = ((int)ResultStatus.Success).ToString();
                }

                string returnData = JsonHelper.Serialize(payNotify);
                string returnSign = SignManager.CreateSign(returnData).Data;
                string url = string.Format(urlFormat, returnSign, returnData);
                return Json(new { status = status, url = url });
            }
            catch (Exception ex)
            {
                string log = string.Format(@"校验支付结果发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：orderId={5}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, orderId);
                LogWriter.WriteLog(log, AppConfig.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                return Json(new { status = -1, msg = "系统执行时发生异常！", error = ex.Message });
            }
        }
        #endregion

        #region 条码支付
        /// <summary>
        /// 条码支付
        /// </summary>
        /// <param name="sign"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string BarcodePay(string sign, string data)
        {
            try
            {
                #region 保存请求报文
                var payRequest = PayRequestManager.SavePayRequest(data, this._payType);
                #endregion

                #region 校验签名
                var checkResult = SignManager.CheckSign(sign, data);
                if (checkResult.Status != ResultStatus.Success || !checkResult.Data)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = string.IsNullOrWhiteSpace(checkResult.Message) ? "签名验证未通过" : checkResult.Message;
                    PayRequestDAL.Update(payRequest);
                    return JsonHelper.Serialize(new { status = 0, msg = payRequest.ResultDesc });
                }
                #endregion

                #region 获取支付请求参数并更新至数据库
                payRequest = PayRequestManager.GetPayRequestInfo(payRequest, data);
                PayRequestDAL.Update(payRequest);
                #endregion

                #region 校验支付请求参数
                var validateResult = PayRequestManager.ValidateBarcodePayParams(data, this._payType);
                if (validateResult.Status != ResultStatus.Success)
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = validateResult.Message;
                    PayRequestDAL.Update(payRequest);
                    return JsonHelper.Serialize(new { status = 0, msg = payRequest.ResultDesc });
                }

                //支付请求对象
                PayOrderInfo payOrder = validateResult.Data;
                #endregion

                #region 更新支付请求执行结果
                payRequest.ExecuteResult = (int)ResultStatus.Success;
                payRequest.Status = (int)AppEnum.GlobalStatus.Valid;
                //更新支付请求记录的执行结果
                PayRequestDAL.Update(payRequest);

                //作废重复的支付请求记录
                PayRequestDAL.InvalidateRepeatRequest(payRequest.SysNo, payOrder.OrderId, this._payType);
                #endregion

                #region 提交条形码支付
                var payResult = PayResultManager.SavePayResult(null, _payType);
                try
                {
                    payResult.OrderId = payRequest.OrderId;
                    payResult.RequestSysNo = payRequest.SysNo;
                    //获取订单支付金额
                    int paymentAmt = (int)(payRequest.PaymentAmt * 100);//微信支付金额的单位为“分”，所以要乘以100
                    //提交条形码支付
                    WxPayData result = MicroPay.Run(payRequest.OrderId, paymentAmt, payOrder.Barcode);
                    //校验返回参数
                    if (result.GetValue("return_code").ToString() != "SUCCESS")
                    {
                        payResult.ExecuteResult = (int)ResultStatus.Failure;
                        payResult.ResultDesc = "微信条码支付请求失败：" + result.GetValue("return_msg").ToString();
                        PayResultDAL.Update(payResult);
                        return JsonHelper.Serialize(new { status = 0, msg = payResult.ResultDesc });
                    }
                    else if (result.GetValue("result_code").ToString() != "SUCCESS")
                    {
                        payResult.ExecuteResult = (int)ResultStatus.Failure;
                        payResult.ResultDesc = "微信条码支付失败：" + result.GetValue("err_code_des").ToString();
                        PayResultDAL.Update(payResult);
                        return JsonHelper.Serialize(new { status = 0, msg = payResult.ResultDesc });
                    }

                    //支付成功，更新支付结果记录
                    payResult.TradeNo = result.GetValue("transaction_id").ToString();
                    payResult.PaymentAmt = Convert.ToDecimal(result.GetValue("total_fee")) / 100; //微信支付金额的单位为“分”，所以要除以100
                    payResult.ExecuteResult = (int)ResultStatus.Success;
                    payResult.NotifyStatus = (int)AppEnum.GlobalStatus.Valid;//条码支付，默认通知成功
                    PayResultDAL.Update(payResult);

                    var payNotify = new PayNotifyInfo()
                    {
                        OrderId = payResult.OrderId,
                        TradeNo = payResult.TradeNo,
                        PaymentAmt = payResult.PaymentAmt.ToString(),
                        Result = ((int)ResultStatus.Success).ToString()
                    };
                    return JsonHelper.Serialize(new { status = 1, data = payNotify });
                }
                catch (WxPayException wex)
                {
                    payResult.ExecuteResult = (int)ResultStatus.Failure;
                    payResult.ResultDesc = wex.Message;
                    PayResultDAL.Update(payResult);
                    return JsonHelper.Serialize(new { status = 0, msg = payResult.ResultDesc });
                }
                #endregion
            }
            catch (Exception ex)
            {
                string log = string.Format(@"条码支付发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：sign={5} data={6}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, sign, data);
                LogWriter.WriteLog(log, AppConfig.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                return JsonHelper.Serialize(new { status = -1, msg = "系统执行时发生异常：" + ex.Message });
            }
        }
        #endregion

        #region 支付退款
        /// <summary>
        /// 在线支付退款
        /// </summary>
        /// <param name="sign"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string OnlinePayRefund(string sign, string data)
        {
            try
            {
                #region 保存请求报文
                var payRefund = PayRefundManager.SavePayRefund(data, this._payType);
                #endregion

                #region 校验签名
                var checkResult = SignManager.CheckSign(sign, data);
                if (checkResult.Status != ResultStatus.Success || !checkResult.Data)
                {
                    payRefund.ExecuteResult = (int)ResultStatus.Failure;
                    payRefund.ResultDesc = string.IsNullOrWhiteSpace(checkResult.Message) ? "签名验证未通过" : checkResult.Message;
                    PayRefundDAL.Update(payRefund);
                    return JsonHelper.Serialize(new { status = 0, msg = payRefund.ResultDesc });
                }
                #endregion

                #region 获取退款请求参数并更新至数据库
                payRefund = PayRefundManager.GetPayRefundInfo(payRefund, data);
                PayRefundDAL.Update(payRefund);
                #endregion

                #region 校验退款请求参数
                var validateResult = PayRefundManager.ValidatePayRefundParams(data);
                if (validateResult.Status != ResultStatus.Success)
                {
                    payRefund.ExecuteResult = (int)ResultStatus.Failure;
                    payRefund.ResultDesc = validateResult.Message;
                    PayRefundDAL.Update(payRefund);
                    return JsonHelper.Serialize(new { status = 0, msg = payRefund.ResultDesc });
                }
                #endregion

                #region 提交退款申请
                try
                {
                    //获取支付金额
                    int paymentAmt = (int)(payRefund.PaymentAmt * 100);//微信支付金额的单位为“分”，所以要乘以100
                    //获取退款金额
                    int refundAmt = (int)(payRefund.RefundAmt * 100);//微信退款金额的单位为“分”，所以要乘以100
                    //提交退款请求
                    WxPayData result = PayRefund.Run(payRefund.TradeNo, payRefund.OrderId, paymentAmt, refundAmt, payRefund.RefundOrderId);
                    //校验返回参数
                    if (result.GetValue("return_code").ToString() != "SUCCESS")
                    {
                        LogWriter.WriteLog(payRefund.OrderId + "退款响应数据：" + result.ToUrl(), AppConfig.WeChatPayLogFolder);
                        payRefund.ExecuteResult = (int)ResultStatus.Failure;
                        payRefund.ResultDesc = "申请微信退款请求失败：" + result.GetValue("return_msg").ToString();
                        PayRefundDAL.Update(payRefund);
                        return JsonHelper.Serialize(new { status = 0, msg = payRefund.ResultDesc });
                    }
                    else if (result.GetValue("result_code").ToString() != "SUCCESS")
                    {
                        LogWriter.WriteLog(payRefund.OrderId + "退款响应数据：" + result.ToUrl(), AppConfig.WeChatPayLogFolder);
                        payRefund.ExecuteResult = (int)ResultStatus.Failure;
                        payRefund.ResultDesc = "申请微信退款失败：" + result.GetValue("err_code_des").ToString();
                        PayRefundDAL.Update(payRefund);
                        return JsonHelper.Serialize(new { status = 0, msg = payRefund.ResultDesc });
                    }

                    //退款成功，更新退款记录
                    payRefund.RefundNo = result.GetValue("refund_id").ToString();
                    payRefund.ExecuteResult = (int)ResultStatus.Success;
                    PayRefundDAL.Update(payRefund);

                    var refundNotify = new RefundNotifyInfo()
                    {
                        OrderId = payRefund.OrderId,
                        TradeNo = payRefund.TradeNo,
                        RefundOrderId = payRefund.RefundOrderId,
                        RefundNo = payRefund.RefundNo,
                        RefundAmt = payRefund.RefundAmt.ToString(),
                    };
                    return JsonHelper.Serialize(new { status = 1, data = refundNotify });
                }
                catch (WxPayException wex)
                {
                    payRefund.ExecuteResult = (int)ResultStatus.Failure;
                    payRefund.ResultDesc = wex.Message;
                    PayRefundDAL.Update(payRefund);
                    return JsonHelper.Serialize(new { status = 0, msg = payRefund.ResultDesc });
                }
                #endregion
            }
            catch (Exception ex)
            {
                string log = string.Format(@"支付退款发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：sign={5} data={6}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, sign, data);
                LogWriter.WriteLog(log, AppConfig.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                return JsonHelper.Serialize(new { status = -1, msg = "系统执行时发生异常：" + ex.Message });
            }
        }
        #endregion

        #region 支付查询
        /// <summary>
        /// 查询订单的支付结果
        /// </summary>
        /// <param name="orderId">订单编号</param>
        /// <param name="tradeNo">交易流水号</param>
        /// <returns></returns>
        public string Query(string orderId = "", string tradeNo = "")
        {
            var reqData = new WxPayData();
            reqData.SetValue("out_trade_no", orderId);
            reqData.SetValue("transaction_id", tradeNo);
            var queryData = WxPayApi.OrderQuery(reqData);
            var jsonStr = queryData.ToJson();
            var json = JObject.Parse(jsonStr);
            jsonStr = JsonHelper.Serialize(json, true);
            return jsonStr;
        }
        #endregion
    }
}