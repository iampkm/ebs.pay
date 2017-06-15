using CQSS.Pay.BLL;
using CQSS.Pay.BLL.Alipay;
using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Model.Api;
using CQSS.Pay.Model.Data;
using CQSS.Pay.Util;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CQSS.Pay.Web.Controllers
{
    /// <summary>
    /// 支付宝支付
    /// </summary>
    public class AlipayController : Controller
    {
        /// <summary>
        /// 支付方式（支付宝）
        /// </summary>
        private AppEnum.PayType _payType = AppEnum.PayType.Alipay;

        #region 在线支付
        /// <summary>
        /// 在线支付
        /// </summary>
        /// <param name="appId">业务系统ID</param>
        /// <param name="sign">签名</param>
        /// <param name="data">数据报文</param>
        /// <returns></returns>
        public ActionResult OnlinePay(string appId, string sign, string data)
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
                if (RequestHelper.IsWapRequest())
                {
                    checkResult = onlinePay.CheckBrowserType(AppEnum.BrowserType.Others, requestInfo);
                    if (checkResult.Status != ResultStatus.Success)
                    {
                        ViewBag.ErrorMsg = checkResult.Message;
                        return View();
                    }
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


                //组装支付请求表单数据
                if (!RequestHelper.IsWapRequest())
                    ViewBag.Form = AlipayManager.BuildRequestForm(orderInfo);//PC端访问
                else
                    ViewBag.Form = AlipayManager.BuildWapRequestForm(orderInfo);//移动端访问

                //支付请求执行成功
                onlinePay.ExecuteSuccess(requestInfo);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMsg = "系统执行时发生异常：" + ex.Message;
                string log = string.Format(@"支付发生异常！{0}异常描述：{1}{0}异常堆栈：{2}{0}请求参数：appId={3} sign={4} data={5}",
                    Environment.NewLine, ex.Message, ex.StackTrace, appId, sign, data);
                LogWriter.WriteLog(log, AppConfig.Global.AlipayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
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
            try
            {
                var resultInterface = Builder.BuildAlipayResult();
                requestParams = HttpUtility.UrlDecode(Request.Form.ToString());

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

                return "success";//关键代码，请不要修改或删除
            }
            catch (Exception ex)
            {
                string log = string.Format(@"支付异步通知发生异常！{0}异常描述：{1}{0}异常堆栈：{2}{0}请求参数：{3}",
                    Environment.NewLine, ex.Message, ex.StackTrace, requestParams);
                LogWriter.WriteLog(log, AppConfig.Global.AlipayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                return "exception";
            }
        }

        /// <summary>
        /// 支付完成返回页面
        /// </summary>
        /// <returns></returns>
        public ActionResult OnlinePayReturn()
        {
            //请求参数报文
            string requestParams = string.Empty;
            //支付完成通知对象
            var notifyInfo = new PayNotifyInfo();
            try
            {
                var resultInterface = Builder.BuildAlipayResult();
                requestParams = HttpUtility.UrlDecode(Request.QueryString.ToString());
                var resultInfo = new PayResultInfo();

                //校验请求报文
                var checkResult = resultInterface.CheckRequest(requestParams, resultInfo);
                if (checkResult.Status != ResultStatus.Success)
                {
                    ViewBag.ErrorMsg = checkResult.Message;
                    return View();
                }

                //解析支付结果
                var resolveResult = resultInterface.ResolveRequest(requestParams, resultInfo);
                if (resolveResult.Status != ResultStatus.Success)
                {
                    ViewBag.ErrorMsg = resolveResult.Message;
                    return View();
                }

                //获取支付完成返回地址，如果有值，则跳转到该地址去
                notifyInfo = resolveResult.Data;
                var requestInfo = PayRequestDAL.GetValidPayRequest(notifyInfo.OrderId, this._payType);
                var returnUrl = resultInterface.GetReturnUrl(requestInfo, notifyInfo);
                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);
            }
            catch (Exception ex)
            {
                string log = string.Format(@"支付完成返回发生异常！{0}异常描述：{1}{0}异常堆栈：{2}{0}请求参数：{3}",
                    Environment.NewLine, ex.Message, ex.StackTrace, requestParams);
                LogWriter.WriteLog(log, AppConfig.Global.AlipayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                ViewBag.ErrorMsg = "系统执行发生异常：" + ex.Message;
            }
            ViewBag.NotifyInfo = notifyInfo;
            return View();
        }
        #endregion

        #region 条码支付

        #endregion
    }
}