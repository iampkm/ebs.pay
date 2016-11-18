using CQSS.Pay.BLL.Allinpay;
using CQSS.Pay.BLL.Allinpay.Api;
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
    /// <summary>
    /// 通联支付
    /// </summary>
    public class AllinpayController : Controller
    {
        /// <summary>
        /// 支付方式（通联支付）
        /// </summary>
        private AppEnum.PayType _payType = AppEnum.PayType.Allinpay;

        #region 在线支付
        /// <summary>
        /// 在线支付
        /// </summary>
        /// <param name="sign">签名</param>
        /// <param name="data">数据报文</param>
        /// <returns></returns>
        public ActionResult OnlinePay(string sign, string data)
        {
            try
            {
                #region 校验支付模式
                if (AppConfig.IsTestMode)
                    return RedirectToActionPermanent("SimulateOnlinePay", "Test", new { payType = (int)this._payType, sign = sign, data = data });
                #endregion

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
                //微信环境访问
                if (RequestHelper.IsWapRequest() && RequestHelper.IsWeChatRequest())
                {
                    payRequest.ExecuteResult = (int)ResultStatus.Failure;
                    payRequest.ResultDesc = "微信中不能使用通联支付，请更换其他支付方式";
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
                PayOrderInfo orderInfo = validateResult.Data;
                #endregion

                #region 组装支付请求表单数据
                //PC端访问
                if (!RequestHelper.IsWapRequest())
                {
                    ViewBag.Form = AllinpayManager.BuildRequestForm(orderInfo);
                }
                //移动端访问
                else
                {
                    ViewBag.Form = AllinpayManager.BuildWapRequestForm(orderInfo);
                }
                #endregion

                #region 更新支付请求执行结果
                //更新支付请求记录的执行结果
                payRequest.ExecuteResult = (int)ResultStatus.Success;
                payRequest.Status = (int)AppEnum.GlobalStatus.Valid;
                PayRequestDAL.Update(payRequest);

                //作废重复的支付请求记录
                PayRequestDAL.InvalidateRepeatRequest(payRequest.SysNo, payRequest.OrderId, this._payType);
                #endregion
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMsg = "系统执行时发生异常：" + ex.Message;
                string log = string.Format(@"支付发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：sign={5} data={6}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, sign, data);
                LogWriter.WriteLog(log, AppConfig.AllinpayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
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
                #region 保存请求报文
                requestParams = HttpUtility.UrlDecode(Request.Form.ToString());
                var payResult = PayResultManager.SavePayResult(requestParams, this._payType);
                #endregion

                #region 解析请求报文
                var paramsDict = new Dictionary<string, string>();
                string[] requestKeys = Request.Form.AllKeys;
                foreach (string key in requestKeys)
                {
                    paramsDict.Add(key, Request.Form[key]);
                }
                //判断是否有带返回参数
                if (paramsDict.Count == 0)
                {
                    payResult.ExecuteResult = (int)ResultStatus.Failure;
                    payResult.ResultDesc = "无通知参数";
                    PayResultDAL.Update(payResult);
                    return payResult.ResultDesc;
                }
                #endregion

                #region 校验请求报文
                var data = new AllinpayData(AllinpayDataType.PayResult);
                data.FromUrl(requestParams);
                bool verifyResult = AllinpayCore.VerifyResultSign(data);
                if (!verifyResult)
                {
                    payResult.ExecuteResult = (int)ResultStatus.Failure;
                    payResult.ResultDesc = "verify failed";
                    PayResultDAL.Update(payResult);
                    return payResult.ResultDesc;
                }
                #endregion

                #region 记录支付结果记录，并通知业务系统
                //商户订单号
                payResult.OrderId = data.GetValue("orderNo");
                //支付宝交易号
                payResult.TradeNo = data.GetValue("paymentOrderId");
                //支付总金额
                payResult.PaymentAmt = Convert.ToDecimal(data.GetValue("payAmount")) / 100;//通联支付金额单位为“分”，所以除以100
                //交易状态 1：支付成功 0：未付款
                if (data.GetValue("payResult") == "1")
                {
                    payResult.ExecuteResult = (int)ResultStatus.Success;
                }

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

                return "success";//关键代码，请不要修改或删除
            }
            catch (Exception ex)
            {
                string log = string.Format(@"支付异步通知发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：{5}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, requestParams);
                LogWriter.WriteLog(log, AppConfig.AllinpayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
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
            var payNotify = new PayNotifyInfo();
            try
            {
                requestParams = HttpUtility.UrlDecode(Request.Form.ToString());

                #region 解析请求报文
                var paramsDict = new Dictionary<string, string>();
                string[] requestKeys = Request.Form.AllKeys;
                foreach (string key in requestKeys)
                {
                    paramsDict.Add(key, Request.Form[key]);
                }
                //判断是否有带返回参数
                if (paramsDict.Count == 0)
                {
                    ViewBag.ErrorMsg = "支付结果无通知参数";
                    return View();
                }
                #endregion

                #region 校验请求报文
                var data = new AllinpayData(AllinpayDataType.PayResult);
                data.FromUrl(requestParams);
                bool verifyResult = AllinpayCore.VerifyResultSign(data);
                if (!verifyResult)
                {
                    ViewBag.ErrorMsg = "支付结果回执校验未通过";
                    return View();
                }
                #endregion

                #region 解析支付结果，跳转到业务系统
                //商户订单号
                payNotify.OrderId = data.GetValue("orderNo");
                //支付宝交易号
                payNotify.TradeNo = data.GetValue("paymentOrderId");
                //支付总金额
                payNotify.PaymentAmt = (Convert.ToDecimal(data.GetValue("payAmount")) / 100).ToString();//通联支付金额单位为“分”，所以除以100
                //交易状态 1：支付成功 0：未付款
                if (data.GetValue("payResult") == "1")
                    payNotify.Result = ((int)ResultStatus.Success).ToString();
                else
                    payNotify.Result = ((int)ResultStatus.Failure).ToString();

                //支付完成返回业务系统的地址
                string returnUrl = string.Empty;
                var payRequest = PayRequestDAL.GetValidPayRequest(payNotify.OrderId, this._payType);
                if (payRequest != null && payRequest.SysNo > 0)
                    returnUrl = payRequest.ReturnUrl;

                //如果业务系统有支付完成返回地址，则跳转到该地址去
                if (!string.IsNullOrWhiteSpace(returnUrl))
                {
                    string resultData = JsonHelper.Serialize(payNotify);
                    string sign = SignManager.CreateSign(resultData).Data;
                    returnUrl += returnUrl.IndexOf("?") > 0 ? "&" : "?";
                    returnUrl += "sign=" + sign;
                    returnUrl += "&data=" + resultData;
                    return Redirect(returnUrl);
                }
                #endregion
            }
            catch (Exception ex)
            {
                string log = string.Format(@"支付完成返回发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：{5}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, requestParams);
                LogWriter.WriteLog(log, AppConfig.AllinpayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                ViewBag.ErrorMsg = "系统执行发生异常：" + ex.Message;
            }
            ViewBag.PayNotify = payNotify;
            return View();
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
                RefundOrderInfo refundInfo = validateResult.Data;
                #endregion

                #region 提交退款申请
                //获取退款金额
                int refundAmt = (int)(payRefund.RefundAmt * 100);//通联退款金额的单位为“分”，所以要乘以100
                var refundData = new AllinpayData(AllinpayDataType.RefundRequest);
                //商户号
                refundData.SetValue("merchantId", AppConfig.AllinpayMerchantId);
                //商户订单号
                refundData.SetValue("orderNo", payRefund.OrderId);
                //退款金额
                refundData.SetValue("refundAmount", refundAmt.ToString());
                //商户订单提交时间
                refundData.SetValue("orderDatetime", refundInfo.OrderTime);
                //签名字符串
                refundData.SetValue("signMsg", AllinpayCore.RefundSign(refundData));

                var result = AllinpayManager.RefundRequest(refundData);
                if (result.Status != ResultStatus.Success)
                {
                    payRefund.ExecuteResult = (int)ResultStatus.Failure;
                    payRefund.ResultDesc = "通联退款请求失败：" + result.Message;
                    PayRefundDAL.Update(payRefund);
                    return JsonHelper.Serialize(new { status = 0, msg = payRefund.ResultDesc });
                }

                //退款成功，更新退款记录
                payRefund.ExecuteResult = (int)ResultStatus.Success;
                payRefund.RefundNo = "";
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
                #endregion
            }
            catch (Exception ex)
            {
                string log = string.Format(@"支付退款发生异常！{0}异常描述：{1}{2}异常堆栈：{3}{4}请求参数：sign={5} data={6}",
                    Environment.NewLine, ex.Message, Environment.NewLine, ex.StackTrace, Environment.NewLine, sign, data);
                LogWriter.WriteLog(log, AppConfig.AllinpayLogFolder, ExceptionHelper.ExceptionLevel.Exception);
                return JsonHelper.Serialize(new { status = -1, msg = "系统执行时发生异常：" + ex.Message });
            }
        }
        #endregion
    }
}