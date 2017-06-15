using CQSS.Pay.BLL;
using CQSS.Pay.Model;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CQSS.Pay.Web.Controllers
{
    public class SimulateController : Controller
    {
        /// <summary>
        /// 模拟在线支付（测试模式下可用）
        /// </summary>
        /// <param name="payType"></param>
        /// <param name="appId"></param>
        /// <param name="sign"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public ActionResult OnlinePay(int payType, string appId, string sign, string data)
        {
            try
            {
                var type = (AppEnum.PayType)payType;

                #region 处理支付请求
                var onlinePay = Builder.BuildOnlinePay();
                //校验支付模式
                if (!onlinePay.CheckModeIsSimulate())
                    return Content("非测试模式下，模拟支付不可用");

                //保存支付请求报文
                var requestInfo = onlinePay.SaveRequest(appId, data, type);

                //校验签名
                var checkResult = onlinePay.CheckSign(appId, sign, data, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                    return Content(checkResult.Message);

                //解析支付请求
                checkResult = onlinePay.ResolveRequest(data, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                    return Content(checkResult.Message);

                //校验支付参数
                checkResult = onlinePay.CheckParamaters(data, type, requestInfo);
                if (checkResult.Status != ResultStatus.Success)
                    return Content(checkResult.Message);

                //支付请求执行成功
                onlinePay.ExecuteSuccess(requestInfo);
                #endregion

                #region 处理支付结果
                var resultInterface = Builder.BuildSimulatePayResult();

                //保存支付结果报文
                var resultInfo = resultInterface.SaveRequest(null, type);

                //解析支付结果
                var resolveResult = resultInterface.ResolveRequest(data, resultInfo);
                if (resolveResult.Status != ResultStatus.Success)
                    return Content(resolveResult.Message);

                //更新支付结果记录并关联支付请求记录
                resultInterface.RelateRequestInfo(resultInfo);

                //通知业务系统支付结果
                resultInterface.NotifyBack(resultInfo, requestInfo);

                //获取支付完成返回地址，如果有值，则跳转到该地址去
                var notifyInfo = resolveResult.Data;
                var returnUrl = resultInterface.GetReturnUrl(requestInfo, notifyInfo);
                if (!string.IsNullOrEmpty(returnUrl))
                    return Redirect(returnUrl);

                #endregion

                return Content("支付成功");
            }
            catch (Exception ex)
            {
                string log = string.Format(@"模拟在线支付发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：sign={5} data={6}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, sign, data);
                LogWriter.WriteLog(log, "Simulate", ExceptionHelper.ExceptionLevel.Exception);
                return Content("系统执行时发生异常：" + ex.Message);
            }
        }
    }
}