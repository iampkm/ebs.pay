using CQSS.Pay.BLL.Basic;
using CQSS.Pay.BLL.Interface;
using CQSS.Pay.BLL.WeChatPay.Api;
using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Model.Api;
using CQSS.Pay.Model.Data;
using CQSS.Pay.Util;
using CQSS.Pay.Util.Extension;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Manager
{
    /// <summary>
    /// 支付请求
    /// </summary>
    public abstract class PayRequest : PayBase, IPayRequest
    {
        /// <summary>
        /// 保存支付请求
        /// </summary>
        /// <param name="appId">业务系统ID</param>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        public virtual PayRequestInfo SaveRequest(string appId, string data, AppEnum.PayType payType)
        {
            var requestInfo = new PayRequestInfo()
            {
                PayType = (int)payType,
                RequestData = data,
                ExecuteResult = (int)ResultStatus.None,
                AppId = appId,
                Status = (int)AppEnum.GlobalStatus.Invalid,
                CreateTime = DateTime.Now,
            };
            requestInfo.SysNo = PayRequestDAL.Insert(requestInfo);
            return requestInfo;
        }

        /// <summary>
        /// 校验数据签名
        /// </summary>
        /// <param name="appId">业务系统ID</param>
        /// <param name="sign">数据签名</param>
        /// <param name="data">业务数据报文</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        public virtual ExecuteResult CheckSign(string appId, string sign, string data, PayRequestInfo requestInfo)
        {
            var result = SignManager.CheckSign(appId, sign, data);
            if (result.Status != ResultStatus.Success || !result.Data)
            {
                requestInfo.ExecuteResult = (int)ResultStatus.Failure;
                requestInfo.ResultDesc = string.IsNullOrWhiteSpace(result.Message) ? "签名校验未通过" : result.Message;
                PayRequestDAL.Update(requestInfo);
                result.Status = ResultStatus.Failure;
            }
            return result;
        }

        /// <summary>
        /// 校验浏览器是否是某种类型
        /// </summary>
        /// <param name="browserType">浏览器类型</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        public virtual ExecuteResult CheckBrowserType(AppEnum.BrowserType browserType, PayRequestInfo requestInfo)
        {
            var result = new ExecuteResult();
            if (browserType == AppEnum.BrowserType.WeChat && !RequestHelper.IsWeChatRequest())
            {
                result.Status = ResultStatus.Failure;
                requestInfo.ExecuteResult = (int)ResultStatus.Failure;
                result.Message = requestInfo.ResultDesc = "非微信环境中不能使用微信支付，请更换其他支付方式";
                PayRequestDAL.Update(requestInfo);
                return result;
            }

            if (browserType == AppEnum.BrowserType.Others && RequestHelper.IsWeChatRequest())
            {
                result.Status = ResultStatus.Failure;
                requestInfo.ExecuteResult = (int)ResultStatus.Failure;
                result.Message = requestInfo.ResultDesc = "微信中只能使用微信支付，请更换支付方式";
                PayRequestDAL.Update(requestInfo);
                return result;
            }

            result.Status = ResultStatus.Success;
            return result;
        }

        /// <summary>
        /// 校验支付参数
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        public abstract ExecuteResult<PayOrderInfo> CheckParamaters(string data, AppEnum.PayType payType, PayRequestInfo requestInfo);

        /// <summary>
        /// 解析支付请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        public virtual ExecuteResult ResolveRequest(string data, PayRequestInfo requestInfo)
        {
            var result = new ExecuteResult();
            var info = JsonHelper.Deserialize<PayOrderInfo>(data);
            if (info != null)
            {
                requestInfo.OrderId = info.OrderId;
                decimal paymentAmt = 0;
                decimal.TryParse(info.PaymentAmt, out paymentAmt);
                requestInfo.PaymentAmt = paymentAmt;
                requestInfo.NotifyUrl = info.NotifyUrl;
                requestInfo.ReturnUrl = info.ReturnUrl;
                PayRequestDAL.Update(requestInfo);
                result.Status = ResultStatus.Success;
            }
            else
            {
                result.Status = ResultStatus.Failure;
                result.Message = "解析支付请求参数失败";
            }
            return result;
        }

        /// <summary>
        /// 支付请求执行成功
        /// </summary>
        /// <param name="requestInfo">支付请求记录</param>
        public virtual void ExecuteSuccess(PayRequestInfo requestInfo)
        {
            //更新支付请求记录的执行结果
            requestInfo.ExecuteResult = (int)ResultStatus.Success;
            requestInfo.Status = (int)AppEnum.GlobalStatus.Valid;
            PayRequestDAL.Update(requestInfo);

            //作废重复的支付请求记录
            PayRequestDAL.InvalidateRepeatRequest(requestInfo);
        }
    }

    /// <summary>
    /// 在线支付
    /// </summary>
    public class OnlinePay : PayRequest, IPayRequest
    {
        /// <summary>
        /// 校验支付参数
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        public override ExecuteResult<PayOrderInfo> CheckParamaters(string data, AppEnum.PayType payType, PayRequestInfo requestInfo)
        {
            var result = this.CheckParamaters(data, payType);
            if (result.Status != ResultStatus.Success)
            {
                requestInfo.ExecuteResult = (int)ResultStatus.Failure;
                requestInfo.ResultDesc = result.Message;
                PayRequestDAL.Update(requestInfo);
            }
            return result;
        }

        /// <summary>
        /// 校验支付参数
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        private ExecuteResult<PayOrderInfo> CheckParamaters(string data, AppEnum.PayType payType)
        {
            //校验结果
            var result = new ExecuteResult<PayOrderInfo>();
            //参数名称
            string paramName = null;
            //金额匹配表达式（最多保留2位小数正实数）
            string amtReg = @"^\d+(\.[0-9]{1,2}0*)?$";

            #region 校验支付报文结构
            var info = JsonHelper.Deserialize<PayOrderInfo>(data);
            if (info == null)
            {
                result.Status = ResultStatus.Failure;
                result.Message = "参数data格式不正确";
                return result;
            }
            #endregion

            #region 校验参数类型和取值范围

            #region 校验订单编号
            paramName = "orderId";
            if (string.IsNullOrWhiteSpace(info.OrderId))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            #endregion

            #region 校验支付金额
            paramName = "paymentAmt";
            if (string.IsNullOrWhiteSpace(info.PaymentAmt))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            decimal paymentAmt = 0;
            if (!Regex.IsMatch(info.PaymentAmt, amtReg) || !decimal.TryParse(info.PaymentAmt, out paymentAmt))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}类型错误", paramName);
                return result;
            }
            if (paymentAmt <= 0)
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}必须大于0", paramName);
                return result;
            }
            #endregion

            #region 校验下单时间
            paramName = "orderTime";
            if (string.IsNullOrWhiteSpace(info.OrderTime))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            DateTime orderTime;
            if (!DateTime.TryParseExact(info.OrderTime, "yyyyMMddHHmmss", new CultureInfo("zh-CN", true), DateTimeStyles.None, out orderTime))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}格式错误", paramName);
                return result;
            }
            #endregion

            #region 校验支付完成的通知地址
            paramName = "notifyUrl";
            if (string.IsNullOrWhiteSpace(info.NotifyUrl))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            if (!info.NotifyUrl.IsUrl())
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}格式错误", paramName);
                return result;
            }
            #endregion

            #region 校验支付完成的返回地址
            paramName = "returnUrl";
            if (string.IsNullOrWhiteSpace(info.ReturnUrl))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            if (!info.ReturnUrl.IsUrl())
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}格式错误", paramName);
                return result;
            }
            #endregion

            #endregion

            #region 校验是否已支付
            if (PayResultDAL.ExistValidPayResult(info.OrderId, payType))
            {
                result.Status = ResultStatus.Failure;
                result.Message = "该订单已成功支付，不能重复支付";
                return result;
            }
            #endregion

            result.Status = ResultStatus.Success;
            result.Data = info;
            return result;
        }
    }

    /// <summary>
    /// 条码支付
    /// </summary>
    public class BarcodePay : PayRequest, IPayRequest
    {
        /// <summary>
        /// 校验支付参数
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        public override ExecuteResult<PayOrderInfo> CheckParamaters(string data, AppEnum.PayType payType, PayRequestInfo requestInfo)
        {
            var result = this.CheckParamaters(data, payType);
            if (result.Status != ResultStatus.Success)
            {
                requestInfo.ExecuteResult = (int)ResultStatus.Failure;
                requestInfo.ResultDesc = result.Message;
                PayRequestDAL.Update(requestInfo);
            }
            return result;
        }

        /// <summary>
        /// 校验支付参数
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        private ExecuteResult<PayOrderInfo> CheckParamaters(string data, AppEnum.PayType payType)
        {
            //校验结果
            var result = new ExecuteResult<PayOrderInfo>();
            //参数名称
            string paramName = null;
            //金额匹配表达式（最多保留2位小数正实数）
            string amtReg = @"^\d+(\.[0-9]{1,2}0*)?$";

            #region 校验支付报文结构
            var info = JsonHelper.Deserialize<PayOrderInfo>(data);
            if (info == null)
            {
                result.Status = ResultStatus.Failure;
                result.Message = "参数data格式不正确";
                return result;
            }
            #endregion

            #region 校验参数类型和取值范围

            #region 校验订单编号
            paramName = "orderId";
            if (string.IsNullOrWhiteSpace(info.OrderId))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            #endregion

            #region 校验支付金额
            paramName = "paymentAmt";
            if (string.IsNullOrWhiteSpace(info.PaymentAmt))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            decimal paymentAmt = 0;
            if (!Regex.IsMatch(info.PaymentAmt, amtReg) || !decimal.TryParse(info.PaymentAmt, out paymentAmt))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}类型错误", paramName);
                return result;
            }
            if (paymentAmt <= 0)
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}必须大于0", paramName);
                return result;
            }
            #endregion

            #region 校验支付条形码
            paramName = "barcode";
            if (string.IsNullOrWhiteSpace(info.Barcode))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            #endregion

            #region 校验支付完成的通知地址
            paramName = "notifyUrl";
            if (string.IsNullOrWhiteSpace(info.NotifyUrl))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            if (!info.NotifyUrl.IsUrl())
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}格式错误", paramName);
                return result;
            }
            #endregion

            #endregion

            #region 校验是否已支付
            if (PayResultDAL.ExistValidPayResult(info.OrderId, payType))
            {
                result.Status = ResultStatus.Failure;
                result.Message = "该订单已成功支付，不能重复支付";
                return result;
            }
            #endregion

            result.Status = ResultStatus.Success;
            result.Data = info;
            return result;
        }
    }
}
