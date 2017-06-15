using CQSS.Pay.BLL.Basic;
using CQSS.Pay.BLL.Interface;
using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Model.Api;
using CQSS.Pay.Model.Data;
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
    /// 退款申请
    /// </summary>
    public class RefundRequest : PayBase, IRefundRequest
    {
        /// <summary>
        /// 保存退款请求
        /// </summary>
        /// <param name="appId">业务系统ID</param>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        public virtual RefundRequestInfo SaveRequest(string appId, string data, AppEnum.PayType payType)
        {
            var requestInfo = new RefundRequestInfo()
            {
                PayType = (int)payType,
                RequestData = data,
                ExecuteResult = (int)ResultStatus.None,
                AppId = appId,
                Status = (int)AppEnum.GlobalStatus.Valid,
                CreateTime = DateTime.Now,
            };
            requestInfo.SysNo = RefundRequestDAL.Insert(requestInfo);
            return requestInfo;
        }

        /// <summary>
        /// 校验数据签名
        /// </summary>
        /// <param name="appId">业务系统ID</param>
        /// <param name="sign">数据签名</param>
        /// <param name="data">业务数据报文</param>
        /// <param name="requestInfo">退款请求记录</param>
        /// <returns></returns>
        public virtual ExecuteResult CheckSign(string appId, string sign, string data, RefundRequestInfo requestInfo)
        {
            var result = SignManager.CheckSign(appId, sign, data);
            if (result.Status != ResultStatus.Success || !result.Data)
            {
                requestInfo.ExecuteResult = (int)ResultStatus.Failure;
                requestInfo.ResultDesc = string.IsNullOrWhiteSpace(result.Message) ? "签名校验未通过" : result.Message;
                RefundRequestDAL.Update(requestInfo);
                result.Status = ResultStatus.Failure;
            }
            return result;
        }

        /// <summary>
        /// 校验退款参数
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <param name="requestInfo">退款请求记录</param>
        /// <returns></returns>
        public virtual ExecuteResult<RefundOrderInfo> CheckParamaters(string data, AppEnum.PayType payType, RefundRequestInfo requestInfo)
        {
            var result = this.CheckParamaters(data, payType);
            if (result.Status != ResultStatus.Success)
            {
                requestInfo.ExecuteResult = (int)ResultStatus.Failure;
                requestInfo.ResultDesc = result.Message;
                RefundRequestDAL.Update(requestInfo);
            }
            return result;
        }

        /// <summary>
        /// 校验退款参数
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        private ExecuteResult<RefundOrderInfo> CheckParamaters(string data, AppEnum.PayType payType)
        {
            //校验结果
            var result = new ExecuteResult<RefundOrderInfo>();
            //参数名称
            string paramName = null;
            //金额匹配表达式（最多保留2位小数正实数）
            string amtReg = @"^\d+(\.[0-9]{1,2}0*)?$";

            #region 校验退款报文结构
            var info = JsonHelper.Deserialize<RefundOrderInfo>(data);
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

            #region 校验交易流水号
            paramName = "tradeNo";
            if (string.IsNullOrWhiteSpace(info.TradeNo))
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

            #region 校验退款单编号
            paramName = "refundOrderId";
            if (string.IsNullOrWhiteSpace(info.RefundOrderId))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            #endregion

            #region 校验退款金额
            paramName = "refundAmt";
            if (string.IsNullOrWhiteSpace(info.RefundAmt))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}不能为空", paramName);
                return result;
            }
            decimal refundAmt = 0;
            if (!Regex.IsMatch(info.RefundAmt, amtReg) || !decimal.TryParse(info.RefundAmt, out refundAmt))
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}类型错误", paramName);
                return result;
            }
            if (refundAmt <= 0)
            {
                result.Status = ResultStatus.Failure;
                result.Message = string.Format("参数{0}必须大于0", paramName);
                return result;
            }
            #endregion

            #region 校验退款完成的通知地址
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

            #region 校验退款金额合法性
            if (refundAmt > paymentAmt)
            {
                result.Status = ResultStatus.Failure;
                result.Message = "退款金额不能大于支付金额";
                return result;
            }

            //var refundedAmt = RefundResultDAL.GetRefundedAmt(info.OrderId, payType);
            //if (paymentAmt - refundedAmt < refundAmt)
            //{
            //    result.Status = ResultStatus.Failure;
            //    result.Message = "订单剩余可退款额度小于退款金额，不能完成退款";
            //    return result;
            //}
            #endregion

            result.Status = ResultStatus.Success;
            result.Data = info;
            return result;
        }

        /// <summary>
        /// 解析退款请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="requestInfo">退款请求记录</param>
        /// <returns></returns>
        public virtual ExecuteResult ResolveRequest(string data, RefundRequestInfo requestInfo)
        {
            var result = new ExecuteResult();
            var info = JsonHelper.Deserialize<RefundOrderInfo>(data);
            if (info != null)
            {
                requestInfo.OrderId = info.OrderId;
                requestInfo.TradeNo = info.TradeNo;
                requestInfo.RefundOrderId = info.RefundOrderId;
                decimal refundAmt = 0;
                decimal.TryParse(info.RefundAmt, out refundAmt);
                requestInfo.RefundAmt = refundAmt;
                requestInfo.NotifyUrl = info.NotifyUrl;
                RefundRequestDAL.Update(requestInfo);
                result.Status = ResultStatus.Success;
            }
            else
            {
                result.Status = ResultStatus.Failure;
                result.Message = "解析退款请求参数失败";
            }
            return result;
        }

        /// <summary>
        /// 退款请求执行成功
        /// </summary>
        /// <param name="requestInfo">退款请求记录</param>
        public virtual void ExecuteSuccess(RefundRequestInfo requestInfo)
        {
            //更新退款请求记录的执行结果
            requestInfo.ExecuteResult = (int)ResultStatus.Success;
            requestInfo.Status = (int)AppEnum.GlobalStatus.Valid;
            RefundRequestDAL.Update(requestInfo);

            //作废重复的退款请求记录
            RefundRequestDAL.InvalidateRepeatRequest(requestInfo);
        }
    }
}
