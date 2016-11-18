using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Util.Helper;
using CQSS.Pay.Util.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CQSS.Pay.BLL.Basic
{
    /// <summary>
    /// 支付请求管理器
    /// </summary>
    public class PayRequestManager
    {
        /// <summary>
        /// 保存支付请求记录
        /// </summary>
        /// <param name="data"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public static PayRequestInfo SavePayRequest(string data, AppEnum.PayType payType)
        {
            var payRequest = new PayRequestInfo()
            {
                PayType = (int)payType,
                RequestData = data,
                ExecuteResult = (int)ResultStatus.None,
                Status = (int)AppEnum.GlobalStatus.Invalid,
                CreateTime = DateTime.Now,
            };
            payRequest.SysNo = PayRequestDAL.Insert(payRequest);
            return payRequest;
        }

        /// <summary>
        /// 获取支付请求的基本信息
        /// </summary>
        /// <param name="payRequest"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static PayRequestInfo GetPayRequestInfo(PayRequestInfo payRequest, string data)
        {
            var info = JsonHelper.Deserialize<PayOrderInfo>(data);
            if (info != null)
            {
                payRequest.OrderId = info.OrderId;
                decimal paymentAmt = 0;
                decimal.TryParse(info.PaymentAmt, out paymentAmt);
                payRequest.PaymentAmt = paymentAmt;
                payRequest.NotifyUrl = info.NotifyUrl;
                payRequest.ReturnUrl = info.ReturnUrl;
                int systemId = 0;
                int.TryParse(info.SystemId, out systemId);
                payRequest.RequestSystemId = systemId;
            }
            return payRequest;
        }

        /// <summary>
        /// 校验在线支付参数
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ExecuteResult<PayOrderInfo> ValidateOnlinePayParams(string data, AppEnum.PayType payType)
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

            #region 校验跨境支付订单
            if (info.CrossboardType == "1")
            {
                #region 校验货款金额
                paramName = "goodsPrice";
                if (string.IsNullOrWhiteSpace(info.GoodsPrice))
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}不能为空", paramName);
                    return result;
                }
                decimal goodsPrice = 0;
                if (!Regex.IsMatch(info.GoodsPrice, amtReg) || !decimal.TryParse(info.GoodsPrice, out goodsPrice))
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}类型错误", paramName);
                    return result;
                }
                if (goodsPrice <= 0)
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}必须大于0", paramName);
                    return result;
                }
                #endregion

                #region 校验税费金额
                paramName = "taxPrice";
                if (string.IsNullOrWhiteSpace(info.TaxPrice))
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}不能为空", paramName);
                    return result;
                }
                decimal taxPrice = 0;
                if (!Regex.IsMatch(info.TaxPrice, amtReg) || !decimal.TryParse(info.TaxPrice, out taxPrice))
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}类型错误", paramName);
                    return result;
                }
                if (taxPrice < 0)
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}必须大于等于0", paramName);
                    return result;
                }
                #endregion

                #region 校验运费金额
                paramName = "freightPrice";
                if (string.IsNullOrWhiteSpace(info.FreightPrice))
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}不能为空", paramName);
                    return result;
                }
                decimal freightPrice = 0;
                if (!Regex.IsMatch(info.FreightPrice, amtReg) || !decimal.TryParse(info.FreightPrice, out freightPrice))
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}类型错误", paramName);
                    return result;
                }
                if (freightPrice < 0)
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}必须大于等于0", paramName);
                    return result;
                }
                #endregion

                #region 校验货款、税费、运费与支付金额的关系
                if (paymentAmt != goodsPrice + taxPrice + freightPrice)
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = "关系“paymentAmt = goodsPrice + taxPrice + freightPrice”不成立";
                    return result;
                }
                #endregion

                #region 校验购买人姓名
                paramName = "buyerName";
                if (string.IsNullOrWhiteSpace(info.BuyerName))
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}不能为空", paramName);
                    return result;
                }
                #endregion

                #region 校验购买人手机号码
                paramName = "buyerCellphone";
                if (string.IsNullOrWhiteSpace(info.BuyerCellphone))
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}不能为空", paramName);
                    return result;
                }
                #endregion

                #region 校验购买人身份证号
                paramName = "buyerIdCardNo";
                if (string.IsNullOrWhiteSpace(info.BuyerIdCardNo))
                {
                    result.Status = ResultStatus.Failure;
                    result.Message = string.Format("参数{0}不能为空", paramName);
                    return result;
                }
                #endregion
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

            #region 校验是否已支付
            if (PayRequestDAL.ExistValidPayResult(info.OrderId, payType))
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

        /// <summary>
        /// 校验条形码支付参数
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ExecuteResult<PayOrderInfo> ValidateBarcodePayParams(string data, AppEnum.PayType payType)
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

            #region 校验是否已支付
            if (PayRequestDAL.ExistValidPayResult(info.OrderId, payType))
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
