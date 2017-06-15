using CQSS.Pay.BLL;
using CQSS.Pay.BLL.Basic;
using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Model.Api;
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
                var resultInfo = PayResultDAL.GetPayResult(resultSysNo);
                if (resultInfo == null || resultInfo.SysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录不存在" });

                if (resultInfo.ExecuteResult != (int)ResultStatus.Success)
                    return Json(new { status = 0, msg = "支付结果记录不是成功且有效的支付" });

                if (resultInfo.RequestSysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录无对应的请求记录" });

                var requestInfo = PayRequestDAL.GetPayRequest(resultInfo.RequestSysNo);
                if (requestInfo == null || requestInfo.SysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录对应的请求记录不存在" });

                if (!requestInfo.NotifyUrl.IsUrl())
                    return Json(new { status = 0, msg = "支付请求记录的通知地址无效" });

                var resultInterface = Builder.BuildAlipayResult();
                var notifyResult = resultInterface.NotifyBack(resultInfo, requestInfo);
                if (notifyResult.Status == ResultStatus.Success)
                    return Json(new { status = 1, msg = "通知成功" });

                //如果已经通知多次，则将通知状态改成已作废
                int notifyCount = PayResultDAL.GetNotifyBackCount(resultInfo.SysNo);
                if (notifyCount >= 5 && resultInfo.NotifyStatus != (int)AppEnum.NotifyStatus.Canceled)
                {
                    resultInfo.NotifyStatus = (int)AppEnum.NotifyStatus.Canceled;
                    PayResultDAL.Update(resultInfo);
                }
                return Json(new { status = 0, msg = "通知失败，原因：" + notifyResult.Message });
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog(ex.Message + "\r\n" + ex.StackTrace, _logDirName, ExceptionHelper.ExceptionLevel.Exception);
                return Json(new { status = -1, msg = ex.Message, error = ex.StackTrace });
            }
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
                var resultInfo = PayResultDAL.GetPayResult(resultSysNo);
                if (resultInfo == null || resultInfo.SysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录不存在" });

                if (resultInfo.RequestSysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录无对应的请求记录" });

                var requestInfo = PayRequestDAL.GetPayRequest(resultInfo.RequestSysNo);
                if (requestInfo == null || requestInfo.SysNo <= 0)
                    return Json(new { status = 0, msg = "支付结果记录对应的请求记录不存在" });

                if (!requestInfo.ReturnUrl.IsUrl())
                    return Json(new { status = 0, msg = "支付请求记录的返回地址不是有效URL" });

                var notifyInfo = new PayNotifyInfo()
                {
                    OrderId = resultInfo.OrderId,
                    PaymentAmt = resultInfo.PaymentAmt.ToString(),
                    TradeNo = resultInfo.TradeNo,
                    ExtTradeNo = resultInfo.ExtTradeNo,
                    Result = resultInfo.ExecuteResult.ToString(),
                };

                var resultInterface = Builder.BuildAlipayResult();
                string returnUrl = resultInterface.GetReturnUrl(requestInfo, notifyInfo);
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