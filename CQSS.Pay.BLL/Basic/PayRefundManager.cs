using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Basic
{
    public class PayRefundManager
    {
        /// <summary>
        /// 保存退款请求记录
        /// </summary>
        /// <param name="data"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public static PayRefundInfo SavePayRefund(string data, AppEnum.PayType payType)
        {
            var payRefund = new PayRefundInfo()
            {
                PayType = (int)payType,
                RequestData = data,
                ExecuteResult = (int)ResultStatus.None,
                CreateTime = DateTime.Now,
            };
            payRefund.SysNo = PayRefundDAL.Insert(payRefund);
            return payRefund;
        }

        /// <summary>
        /// 获取退款请求的基本信息
        /// </summary>
        /// <param name="payRefund"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static PayRefundInfo GetPayRefundInfo(PayRefundInfo payRefund, string data)
        {
            var info = JsonHelper.Deserialize<RefundOrderInfo>(data);
            if (info != null)
            {
                payRefund.OrderId = info.OrderId;
                payRefund.TradeNo = info.TradeNo;
                decimal paymentAmt = 0;
                decimal.TryParse(info.PaymentAmt, out paymentAmt);
                payRefund.PaymentAmt = paymentAmt;
                payRefund.RefundOrderId = info.RefundOrderId;
                decimal refundAmt = 0;
                decimal.TryParse(info.RefundAmt, out refundAmt);
                payRefund.RefundAmt = refundAmt;
                int systemId = 0;
                int.TryParse(info.SystemId, out systemId);
                payRefund.RequestSystemId = systemId;
            }
            return payRefund;
        }

        /// <summary>
        /// 校验退款参数
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ExecuteResult<RefundOrderInfo> ValidatePayRefundParams(string data)
        {
            //校验结果
            var result = new ExecuteResult<RefundOrderInfo>();
            //参数名称
            string paramName = null;
            //金额匹配表达式（最多保留2位小数正实数）
            string amtReg = @"^\d+(\.[0-9]{1,2}0*)?$";

            #region 校验支付报文结构
            var info = JsonHelper.Deserialize<RefundOrderInfo>(data);
            if (info == null)
            {
                result.Status = ResultStatus.Failure;
                result.Message = "参数data格式不正确";
                return result;
            }
            #endregion

            #region 校验参数类型和值

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

            #region 校验支付流水号
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
            if (string.IsNullOrWhiteSpace(info.PaymentAmt))
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

            #region 校验业务系统编号
            paramName = "systemId";
            if (!string.IsNullOrWhiteSpace(info.SystemId))
            {
                int systemId = 0;
                if (!int.TryParse(info.SystemId, out systemId))
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}类型错误", paramName);
                    return result;
                }
                if (systemId <= 0)
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}必须大于0", paramName);
                    return result;
                }
            }
            #endregion

            #endregion

            result.Status = ResultStatus.Success;
            result.Data = info;
            return result;
        }
    }
}
