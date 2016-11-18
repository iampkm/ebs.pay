using CQSS.Pay.BLL.Basic;
using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Util.Extension;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CQSS.Pay.Web.Controllers
{
    public class ReissueNotifyController : Controller
    {
        /// <summary>
        /// 日志存放的文件夹名称
        /// </summary>
        private const string _logDirName = "ReissueNotify";

        /// <summary>
        /// 支付结果列表
        /// </summary>
        /// <param name="pagger"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public ActionResult Index(Pagger pagger, int type = 0)
        {
            var model = new SearchResult();
            var list = PayResultDAL.GetPayResultList(pagger, type);
            model.PaggerData = pagger;
            model.ResultData = list;
            ViewBag.SearchType = type;
            return View(model);
        }

        /// <summary>
        /// 通知业务系统支付结果
        /// </summary>
        /// <param name="resultSysNo"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult Notify(int resultSysNo)
        {
            try
            {
                if (!Request.IsAjaxRequest())
                    return Json(new { status = 0, msg = "非法请求" });

                var payResult = PayResultDAL.GetPayResult(resultSysNo);
                if (payResult == null || payResult.SysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录不存在" });

                if (payResult.ExecuteResult != (int)ResultStatus.Success)
                    return Json(new { status = 0, msg = "支付结果记录不是成功且有效的支付" });

                if (payResult.RequestSysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录无对应的请求记录" });

                var payRequest = PayRequestDAL.GetPayRequest(payResult.RequestSysNo);
                if (payRequest == null || payRequest.SysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录对应的请求记录不存在" });

                if (!payRequest.NotifyUrl.IsUrl())
                    return Json(new { status = 0, msg = "支付请求记录的通知地址无效" });

                bool notifyResult = PayResultManager.NotifyBack(payResult, payRequest);
                if (notifyResult)
                    return Json(new { status = 1, msg = "通知成功" });
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog(ex.Message + "\r\n" + ex.StackTrace, _logDirName, ExceptionHelper.ExceptionLevel.Exception);
                return Json(new { status = -1, msg = ex.Message, error = ex.StackTrace });
            }
            return Json(new { status = 0, msg = "通知失败" });
        }

        /// <summary>
        /// 获取支付结果返回地址
        /// </summary>
        /// <param name="resultSysNo"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetReturnUrl(int resultSysNo)
        {
            try
            {
                if (!Request.IsAjaxRequest())
                    return Json(new { status = 0, msg = "非法请求" });

                var payResult = PayResultDAL.GetPayResult(resultSysNo);
                if (payResult == null || payResult.SysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录不存在" });

                if (payResult.RequestSysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录无对应的请求记录" });

                var payRequest = PayRequestDAL.GetPayRequest(payResult.RequestSysNo);
                if (payRequest == null || payRequest.SysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录对应的请求记录不存在" });

                if (!payRequest.ReturnUrl.IsUrl())
                    return Json(new { status = 0, msg = "支付请求记录的返回地址不是有效URL" });

                var payNotify = new PayNotifyInfo()
                {
                    OrderId = payResult.OrderId,
                    PaymentAmt = payResult.PaymentAmt.ToString(),
                    TradeNo = payResult.TradeNo,
                    Result = payResult.ExecuteResult.ToString(),
                };
                string data = JsonHelper.Serialize(payNotify);
                string sign = SignManager.CreateSign(data).Data;
                string returnUrl = payRequest.ReturnUrl;
                returnUrl += returnUrl.IndexOf("?") > 0 ? "&" : "?";
                returnUrl += "sign=" + sign;
                returnUrl += "&data=" + data;
                return Json(new { status = 1, href = returnUrl });
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog(ex.Message + "\r\n" + ex.StackTrace, _logDirName, ExceptionHelper.ExceptionLevel.Exception);
                return Json(new { status = -1, msg = ex.Message, error = ex.StackTrace });
            }
        }
    }
}