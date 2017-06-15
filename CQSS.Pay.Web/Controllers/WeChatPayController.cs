using CQSS.Pay.BLL;
using CQSS.Pay.BLL.WeChatPay.Api;
using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Model.Api;
using CQSS.Pay.Util;
using CQSS.Pay.Util.Helper;
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
        /// <param name="appId">业务系统ID</param>
        /// <param name="sign">签名</param>
        /// <param name="data">数据报文</param>
        public ActionResult OnlinePay(string appId, string sign, string data)
        {
            try
            {
                var onlinePay = Builder.BuildOnlinePay();

                //校验支付模式
                if (onlinePay.CheckModeIsSimulate())
                    return RedirectToActionPermanent("OnlinePay", "Simulate", new { payType = (int)this._payType, appId = appId, sign = sign, data = data });

                //移动端访问，跳转至微信Wap支付
                if (RequestHelper.IsWapRequest())
                    return RedirectToAction("OnlineWapPay", new { appId = appId, sign = sign, data = data });

                //保存请求报文
                var requestInfo = onlinePay.SaveRequest(appId, data, this._payType);

                //校验签名
                var checkResult = onlinePay.CheckSign(appId, sign, data, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                {
                    ViewBag.ErrorMsg = checkResult.Message;
                    return View();
                }

                //解析支付请求
                checkResult = onlinePay.ResolveRequest(data, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                {
                    ViewBag.ErrorMsg = checkResult.Message;
                    return View();
                }

                //校验支付参数
                var checkResult2 = onlinePay.CheckParamaters(data, this._payType, requestInfo);
                if (checkResult2.Status != ResultStatus.Success)
                {
                    ViewBag.ErrorMsg = checkResult2.Message;
                    return View();
                }
                PayOrderInfo orderInfo = checkResult2.Data;

                #region 生成支付二维码链接code_url
                try
                {
                    NativePay nativePay = new NativePay();
                    //获取订单支付金额
                    int paymentAmt = (int)(requestInfo.PaymentAmt * 100);//微信支付金额的单位为“分”，所以要乘以100
                    //异步通知url
                    string notifyUrl = string.Format("http://{0}/WeChatPay/OnlinePayNotify", AppConfig.Global.Domain);

                    DateTime orderEndTime = DateTime.ParseExact(orderInfo.OrderTime, "yyyyMMddHHmmss", new CultureInfo("zh-CN", true)).AddHours(24);
                    DateTime minExpireTime = DateTime.Now.AddMinutes(6);//为保险，多加1分钟
                    //交易过期时间（最短过期时间间隔必须大于5分钟）
                    string expireTime = (orderEndTime > minExpireTime ? orderEndTime : minExpireTime).ToString("yyyyMMddHHmmss");
                    //向微信发起统一下单请求
                    string codeUrl = nativePay.GetPayUrl(requestInfo.OrderId, paymentAmt, notifyUrl, expireTime);

                    ViewBag.CodeUrl = codeUrl;
                    ViewBag.RequestInfo = requestInfo;
                }
                catch (WxPayException wex)
                {
                    requestInfo.ResultDesc = wex.Message;
                    requestInfo.ExecuteResult = (int)ResultStatus.Error;
                    PayRequestDAL.Update(requestInfo);

                    ViewBag.ErrorMsg = wex.Message;
                    return View();
                }
                catch (Exception ex)
                {
                    requestInfo.ExecuteResult = (int)ResultStatus.Error;
                    requestInfo.ResultDesc = ex.ToString();
                    PayRequestDAL.Update(requestInfo);

                    ViewBag.ErrorMsg = "系统执行时发生异常：" + ex.Message;
                    return View();
                }
                #endregion

                //支付请求执行成功
                onlinePay.ExecuteSuccess(requestInfo);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMsg = "系统执行时发生异常：" + ex.Message;
                string log = string.Format(@"PC支付请求发生异常！{0}异常描述：{1}{0}异常堆栈：{2}{0}请求参数：appId={3} sign={4} data={5}",
                    Environment.NewLine, ex.Message, ex.StackTrace, appId, sign, data);
                LogWriter.WriteLog(log, AppConfig.Global.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
            }
            return View();
        }

        /// <summary>
        /// WAP在线支付
        /// </summary>
        /// <param name="appId">业务系统ID</param>
        /// <param name="sign">签名</param>
        /// <param name="data">数据报文</param>
        /// <returns></returns>
        public ActionResult OnlineWapPay(string appId, string sign, string data)
        {
            try
            {
                var onlinePay = Builder.BuildOnlinePay();

                //校验支付模式
                if (onlinePay.CheckModeIsSimulate())
                    return RedirectToActionPermanent("OnlinePay", "Simulate", new { payType = (int)this._payType, appId = appId, sign = sign, data = data });

                //保存请求报文
                var requestInfo = onlinePay.SaveRequest(appId, data, this._payType);

                //校验签名
                var checkResult = onlinePay.CheckSign(appId, sign, data, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                {
                    ViewBag.ErrorMsg = checkResult.Message;
                    return View();
                }

                //校验支付环境
                checkResult = onlinePay.CheckBrowserType(AppEnum.BrowserType.WeChat, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                {
                    ViewBag.ErrorMsg = checkResult.Message;
                    return View();
                }

                //解析支付请求
                checkResult = onlinePay.ResolveRequest(data, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                {
                    ViewBag.ErrorMsg = checkResult.Message;
                    return View();
                }

                //校验支付参数
                var checkResult2 = onlinePay.CheckParamaters(data, this._payType, requestInfo);
                if (checkResult2.Status != ResultStatus.Success)
                {
                    ViewBag.ErrorMsg = checkResult2.Message;
                    return View();
                }

                //跳转到微信授权系统
                string redirect_uri = HttpUtility.UrlEncode(string.Format("http://{0}/WeChatPay/OnlineWapPayJSAPI?requestSysNo={1}", AppConfig.Global.Domain, requestInfo.SysNo));
                var reqData = new ParameterData();
                reqData.SetValue("appid", WxPayConfig.APPID);
                reqData.SetValue("redirect_uri", redirect_uri);
                reqData.SetValue("response_type", "code");
                reqData.SetValue("scope", "snsapi_base");
                reqData.SetValue("state", "STATE" + "#wechat_redirect");
                string url = "https://open.weixin.qq.com/connect/oauth2/authorize?" + reqData.ToUrl();
                return Redirect(url);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMsg = "系统执行时发生异常：" + ex.Message;
                string log = string.Format(@"WAP支付请求发生异常！{0}异常描述：{1}{0}异常堆栈：{2}{0}请求参数：appId={3} sign={4} data={5}",
                    Environment.NewLine, ex.Message, ex.StackTrace, appId, sign, data);
                LogWriter.WriteLog(log, AppConfig.Global.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
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
                var onlinePay = Builder.BuildOnlinePay();

                //获取支付请求记录
                var requestInfo = PayRequestDAL.GetPayRequest(requestSysNo);
                if (requestInfo == null || requestInfo.SysNo <= 0)
                {
                    ViewBag.ErrorMsg = "您尚未发起支付请求，请返回后重新提交";
                    return View();
                }

                //校验支付环境
                var checkResult = onlinePay.CheckBrowserType(AppEnum.BrowserType.WeChat, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                {
                    ViewBag.ErrorMsg = checkResult.Message;
                    return View();
                }

                #region 组装浏览器调起JS API支付所需的参数
                try
                {
                    JsApiPay jsApiPay = new JsApiPay(System.Web.HttpContext.Current);
                    //获取授权用户信息
                    jsApiPay.GetOpenidAndAccessTokenFromCode(code);
                    //获取订单支付金额
                    int paymentAmt = (int)(requestInfo.PaymentAmt * 100);//微信支付金额的单位为“分”，所以要乘以100
                    //异步通知url
                    string notifyUrl = string.Format("http://{0}/WeChatPay/OnlinePayNotify", AppConfig.Global.Domain);

                    var orderInfo = JsonHelper.Deserialize<PayOrderInfo>(requestInfo.RequestData);
                    DateTime orderEndTime = DateTime.ParseExact(orderInfo.OrderTime, "yyyyMMddHHmmss", new CultureInfo("zh-CN", true)).AddHours(24);
                    DateTime minExpireTime = DateTime.Now.AddMinutes(6);//为保险，多加1分钟
                    //交易过期时间（最短过期时间间隔必须大于5分钟）
                    string expireTime = (orderEndTime > minExpireTime ? orderEndTime : minExpireTime).ToString("yyyyMMddHHmmss");
                    //向微信发起统一下单请求
                    jsApiPay.GetUnifiedOrderResult(requestInfo.OrderId, paymentAmt, notifyUrl, expireTime);
                    //获取调起JS API的参数
                    ViewBag.JsApiParams = jsApiPay.GetJsApiParameters();
                    //订单编号
                    ViewBag.OrderId = requestInfo.OrderId;

                    //异常时返回的业务系统着陆页面
                    var resultInterface = Builder.BuildWeChatPayResult();
                    var notifyInfo = new PayNotifyInfo()
                    {
                        OrderId = requestInfo.OrderId,
                        TradeNo = "",
                        PaymentAmt = requestInfo.PaymentAmt.ToString(),
                        Result = ((int)ResultStatus.Error).ToString()
                    };
                    ViewBag.ReturnUrl = resultInterface.GetReturnUrl(requestInfo, notifyInfo);
                }
                catch (WxPayException wex)
                {
                    requestInfo.ExecuteResult = (int)ResultStatus.Error;
                    requestInfo.ResultDesc = wex.Message;
                    PayRequestDAL.Update(requestInfo);
                    ViewBag.ErrorMsg = requestInfo.ResultDesc;
                    return View();
                }
                catch (Exception ex)
                {
                    requestInfo.ExecuteResult = (int)ResultStatus.Error;
                    requestInfo.ResultDesc = ex.ToString();
                    PayRequestDAL.Update(requestInfo);
                    ViewBag.ErrorMsg = "系统执行时发生异常：" + ex.Message;
                    return View();
                }
                #endregion

                //支付请求执行成功
                onlinePay.ExecuteSuccess(requestInfo);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMsg = "系统执行时发生异常：" + ex.Message;
                string log = string.Format(@"WAP支付JSAPI发生异常！{0}异常描述：{1}{0}异常堆栈：{2}{0}请求参数：requestSysNo={3} code={4}",
                    Environment.NewLine, ex.Message, ex.StackTrace, requestSysNo, code);
                LogWriter.WriteLog(log, AppConfig.Global.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
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
                var resultInterface = Builder.BuildWeChatPayResult();

                //请求报文格式转换
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

                //保存支付结果请求
                var resultInfo = resultInterface.SaveRequest(requestParams, this._payType);

                //校验请求报文
                var checkResult = resultInterface.CheckRequest(requestParams, resultInfo);
                if (checkResult.Status != ResultStatus.Success)
                    return checkResult.Message;

                //解析支付结果
                var resolveResult = resultInterface.ResolveRequest(requestParams, resultInfo);
                if (resolveResult.Status != ResultStatus.Success)
                    return checkResult.Message;

                //更新支付结果记录并关联支付请求记录
                var requestInfo = resultInterface.RelateRequestInfo(resultInfo);

                //通知业务系统支付结果
                resultInterface.NotifyBack(resultInfo, requestInfo);

                //返回执行成功报文（关键代码，请不要修改或删除）
                result.SetValue("return_code", "SUCCESS");
                result.SetValue("return_msg", "OK");
                return result.ToXml();
            }
            catch (Exception ex)
            {
                string log = string.Format(@"支付异步通知发生异常！{0}异常描述：{1}{0}异常堆栈：{2}{0}请求参数：{3}",
                    Environment.NewLine, ex.Message, ex.StackTrace, requestParams);
                LogWriter.WriteLog(log, AppConfig.Global.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                result.SetValue("return_code", "FAIL");
                result.SetValue("return_msg", ex.Message);
                return result.ToXml();
            }
        }
        #endregion

        #region 条码支付
        /// <summary>
        /// 条码支付
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="sign"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string BarcodePay(string appId, string sign, string data)
        {
            try
            {
                #region 处理支付请求
                var barcodePay = Builder.BuildBarcodePay();

                //保存请求报文
                var requestInfo = barcodePay.SaveRequest(appId, data, this._payType);

                //校验签名
                var checkResult = barcodePay.CheckSign(appId, sign, data, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                    return JsonHelper.Serialize(new { status = 0, msg = checkResult.Message });

                //解析支付请求
                checkResult = barcodePay.ResolveRequest(data, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                    return JsonHelper.Serialize(new { status = 0, msg = checkResult.Message });

                //校验支付参数
                var checkResult2 = barcodePay.CheckParamaters(data, this._payType, requestInfo);
                if (checkResult2.Status != ResultStatus.Success)
                    return JsonHelper.Serialize(new { status = 0, msg = checkResult2.Message });

                //支付请求执行成功
                barcodePay.ExecuteSuccess(requestInfo);
                #endregion

                #region 执行支付申请
                var resultInterface = Builder.BuildWeChatPayResult();

                //校验模式
                if (barcodePay.CheckModeIsSimulate())
                    resultInterface = Builder.BuildSimulatePayResult();

                //保存支付结果请求
                var resultInfo = resultInterface.SaveRequest(data, this._payType);
                resultInfo.OrderId = requestInfo.OrderId;

                //执行条码支付
                PayOrderInfo orderInfo = checkResult2.Data;
                var executeResult = resultInterface.ExecuteBarcodePay(orderInfo, resultInfo);
                if (executeResult.Status != ResultStatus.Success)
                    return JsonHelper.Serialize(new { status = 0, msg = executeResult.Message });

                //更新支付结果记录并关联支付请求记录
                resultInterface.RelateRequestInfo(resultInfo);

                //异步线程通知业务系统支付结果
                var thread = new System.Threading.Thread(() => resultInterface.NotifyBack(resultInfo, requestInfo));
                thread.Start();

                var notifyInfo = executeResult.Data;
                return JsonHelper.Serialize(new { status = 1, data = notifyInfo });
                #endregion
            }
            catch (Exception ex)
            {
                string log = string.Format(@"条码支付发生异常！{0}异常描述：{1}{0}异常堆栈：{2}{0}请求参数：appId={3} sign={4} data={5}",
                    Environment.NewLine, ex.Message, ex.StackTrace, appId, sign, data);
                LogWriter.WriteLog(log, AppConfig.Global.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                return JsonHelper.Serialize(new { status = -1, msg = "系统执行时发生异常：" + ex.Message });
            }
        }
        #endregion

        #region 支付退款
        /// <summary>
        /// 同步申请退款
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="sign"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string SyncRefund(string appId, string sign, string data)
        {
            try
            {
                #region 处理退款请求
                var requestInterface = Builder.BuildRefundRequest();

                //保存退款请求
                var requestInfo = requestInterface.SaveRequest(appId, data, this._payType);

                //校验签名
                var checkResult = requestInterface.CheckSign(appId, sign, data, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                    return JsonHelper.Serialize(new { status = 0, msg = checkResult.Message });

                //校验退款参数
                var checkResult2 = requestInterface.CheckParamaters(data, this._payType, requestInfo);
                if (checkResult2.Status != ResultStatus.Success)
                    return JsonHelper.Serialize(new { status = 0, msg = checkResult2.Message });

                RefundOrderInfo orderInfo = checkResult2.Data;

                //解析退款请求
                checkResult = requestInterface.ResolveRequest(data, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                    return JsonHelper.Serialize(new { status = 0, msg = checkResult.Message });

                //退款请求执行成功
                requestInterface.ExecuteSuccess(requestInfo);
                #endregion

                #region 执行退款申请
                var resultInterface = Builder.BuildWechatRefundResult();

                //校验模式
                if (requestInterface.CheckModeIsSimulate())
                    resultInterface = Builder.BuildSimulateRefundResult();

                //保存退款结果请求
                var resultInfo = resultInterface.SaveRequest(data, this._payType);

                //执行同步退款
                var executeResult = resultInterface.ExecuteSyncRefund(orderInfo, resultInfo);
                if (executeResult.Status != ResultStatus.Success)
                    return JsonHelper.Serialize(new { status = 0, msg = executeResult.Message });

                //更新退款结果记录并关联退款请求记录
                var relateResult = resultInterface.RelateRequestInfo(resultInfo);
                if (relateResult.Status != ResultStatus.Success)
                    return JsonHelper.Serialize(new { status = 0, msg = relateResult.Message });

                //异步线程通知业务系统退款结果
                var thread = new System.Threading.Thread(() => resultInterface.NotifyBack(resultInfo, requestInfo));
                thread.Start();

                var notifyInfo = executeResult.Data;
                return JsonHelper.Serialize(new { status = 1, data = notifyInfo });
                #endregion
            }
            catch (Exception ex)
            {
                string log = string.Format(@"支付退款发生异常！{0}异常描述：{1}{0}异常堆栈：{2}{0}请求参数：appId={3} sign={4} data={5}",
                    Environment.NewLine, ex.Message, ex.StackTrace, appId, sign, data);
                LogWriter.WriteLog(log, AppConfig.Global.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                return JsonHelper.Serialize(new { status = -1, msg = "系统执行时发生异常：" + ex.Message });
            }
        }
        #endregion

        #region 支付查询
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
                if (!Request.IsAjaxRequest())
                    return Json(new { status = -2, msg = "非法请求！" });

                var requestInfo = PayRequestDAL.GetValidPayRequest(orderId, this._payType);
                if (requestInfo == null || requestInfo.SysNo <= 0)
                    return Json(new { status = -2, msg = "系统不存在该订单的支付请求，请自行返回下单页面！" });

                var notifyInfo = new PayNotifyInfo()
                {
                    OrderId = requestInfo.OrderId,
                    PaymentAmt = requestInfo.PaymentAmt.ToString(),
                    TradeNo = "",
                    Result = ((int)ResultStatus.Failure).ToString(),
                };

                int status = 0;
                var resultInfo = PayResultDAL.GetValidPayResult(orderId, this._payType);
                if (resultInfo != null && resultInfo.SysNo > 0)
                {
                    status = 1;//有支付成功记录
                    notifyInfo.TradeNo = resultInfo.TradeNo;
                    notifyInfo.Result = resultInfo.ExecuteResult.ToString();
                }

                //当支付页面收到的支付结果为成功时，不管有没有收到支付成功的异步回执，返回到支付结果着陆页时都提示支付成功
                if (string.Equals(resultType, "ok", StringComparison.CurrentCultureIgnoreCase))
                    notifyInfo.Result = ((int)ResultStatus.Success).ToString();

                var resultInterface = Builder.BuildWeChatPayResult();
                var url = resultInterface.GetReturnUrl(requestInfo, notifyInfo);
                return Json(new { status = status, url = url });
            }
            catch (Exception ex)
            {
                string log = string.Format(@"校验支付结果发生异常！{0}异常描述：{1}{0}异常堆栈：{2}{0}请求参数：orderId={3}",
                    Environment.NewLine, ex.Message, ex.StackTrace, orderId);
                LogWriter.WriteLog(log, AppConfig.Global.WeChatPayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                return Json(new { status = -1, msg = "系统执行时发生异常！", error = ex.Message });
            }
        }

        /// <summary>
        /// 查询订单的支付结果
        /// </summary>
        /// <param name="orderId">订单编号</param>
        /// <param name="tradeNo">交易流水号</param>
        /// <returns></returns>
        public string Query(string orderId = "", string tradeNo = "")
        {
            try
            {
                var reqData = new WxPayData();
                reqData.SetValue("out_trade_no", orderId);
                reqData.SetValue("transaction_id", tradeNo);
                var queryData = WxPayApi.OrderQuery(reqData);
                var jsonStr = queryData.ToJson();
                var json = Newtonsoft.Json.Linq.JObject.Parse(jsonStr);
                jsonStr = JsonHelper.Serialize(json, true);
                return jsonStr;
            }
            catch (Exception ex)
            {
                return "系统执行时发生异常：" + ex.Message;
            }
        }
        #endregion
    }
}