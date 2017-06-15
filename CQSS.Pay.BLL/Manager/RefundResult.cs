using CQSS.Pay.BLL.Allinpay;
using CQSS.Pay.BLL.Allinpay.Api;
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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Manager
{
    /// <summary>
    /// 退款结果
    /// </summary>
    public abstract class RefundResult : PayBase, IRefundResult
    {
        /// <summary>
        /// 保存退款结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        public RefundResultInfo SaveRequest(string data, AppEnum.PayType payType)
        {
            var resultInfo = new RefundResultInfo()
            {
                PayType = (int)payType,
                RequestData = data,
                ExecuteResult = (int)ResultStatus.None,
                NotifyStatus = (int)AppEnum.NotifyStatus.Original,
                CreateTime = DateTime.Now,
            };
            resultInfo.SysNo = RefundResultDAL.Insert(resultInfo);
            return resultInfo;
        }

        /// <summary>
        /// 校验异步退款结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public abstract ExecuteResult CheckRequest(string data, RefundResultInfo resultInfo);

        /// <summary>
        /// 解析异步退款结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public abstract ExecuteResult<RefundNotifyInfo> ResolveRequest(string data, RefundResultInfo resultInfo);

        /// <summary>
        /// 执行同步退款
        /// </summary>
        /// <param name="orderInfo">退款订单</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public abstract ExecuteResult<RefundNotifyInfo> ExecuteSyncRefund(RefundOrderInfo orderInfo, RefundResultInfo resultInfo);

        /// <summary>
        /// 更新退款结果记录并关联退款请求记录
        /// </summary>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public virtual ExecuteResult<RefundRequestInfo> RelateRequestInfo(RefundResultInfo resultInfo)
        {
            var result = new ExecuteResult<RefundRequestInfo>();

            //判断是否存在有效的退款结果记录，如果存在，则当前退款结果记录不执行
            bool exist = RefundResultDAL.ExistValidRefundResult(resultInfo);
            if (exist)
            {
                resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                resultInfo.ResultDesc = "已存在有效的退款结果记录";
                RefundResultDAL.Update(resultInfo);

                result.Status = ResultStatus.Failure;
                result.Message = resultInfo.ResultDesc;
                return result;
            }

            var requestInfo = RefundRequestDAL.GetValidRefundRequest(resultInfo.OrderId, resultInfo.RefundOrderId, (AppEnum.PayType)resultInfo.PayType);
            if (requestInfo != null && requestInfo.SysNo > 0)
                resultInfo.RequestSysNo = requestInfo.SysNo;

            RefundResultDAL.Update(resultInfo);
            result.Status = ResultStatus.Success;
            result.Data = requestInfo;
            return result;
        }

        /// <summary>
        /// 通知业务系统退款结果
        /// </summary>
        /// <param name="resultInfo">退款结果记录</param>
        /// <param name="requestInfo">退款请求记录</param>
        /// <returns></returns>
        public virtual ExecuteResult NotifyBack(RefundResultInfo resultInfo, RefundRequestInfo requestInfo)
        {
            var result = new ExecuteResult() { Status = ResultStatus.Failure };

            //退款结果记录对象无效，则不执行
            if (resultInfo == null || resultInfo.SysNo <= 0)
            {
                result.Message = "退款结果记录对象无效";
                return result;
            }

            //退款请求记录对象无效，则不执行
            if (requestInfo == null || requestInfo.SysNo <= 0)
            {
                result.Message = "退款请求记录对象无效";
                return result;
            }

            //退款结果记录与退款请求记录不对应，则不执行
            if (requestInfo.SysNo != resultInfo.RequestSysNo)
            {
                result.Message = "退款结果记录与退款请求记录不对应";
                return result;
            }

            //退款结果记录未成功执行，或者已通知，则不执行
            if (resultInfo.ExecuteResult != (int)ResultStatus.Success || resultInfo.NotifyStatus == (int)AppEnum.NotifyStatus.Finished)
            {
                result.Message = "退款结果记录未成功执行或已通知";
                return result;
            }

            //退款请求记录中不存在有效的通知地址，则不执行
            if (!requestInfo.NotifyUrl.IsUrl())
            {
                result.Message = "退款请求记录中不存在有效的通知地址";
                return result;
            }

            var notifyInfo = this.GetRefundNotifyInfo(resultInfo, requestInfo.TradeNo);
            string data = JsonHelper.Serialize(notifyInfo);
            string sign = SignManager.CreateSign(requestInfo.AppId, data).Data;
            string param = string.Format("appId={0}&sign={1}&data={2}", requestInfo.AppId, sign, data);
            var response = HttpHelper.HttpRequest("POST", requestInfo.NotifyUrl, param, 10000);
            string status = JsonHelper.GetJsonNode(response, "status");
            string msg = JsonHelper.GetJsonNode(response, "msg");
            result.Message = msg;

            var backInfo = new NotifyBackInfo()
            {
                ResultSysNo = resultInfo.SysNo,
                Status = (int)AppEnum.GlobalStatus.Invalid,
                Msg = msg,
                CreateTime = DateTime.Now,
                ResponseData = response,
            };
            if (status == "1")
            {
                backInfo.Status = (int)AppEnum.GlobalStatus.Valid;
                resultInfo.NotifyStatus = (int)AppEnum.NotifyStatus.Finished;
                RefundResultDAL.Update(resultInfo);
                result.Status = ResultStatus.Success;
            }
            RefundResultDAL.InsertNotifyBack(backInfo);
            return result;
        }

        /// <summary>
        /// 获取退款结果通知对象
        /// </summary>
        /// <param name="resultInfo">退款结果记录</param>
        /// <param name="tradeNo">支付交易流水号</param>
        /// <returns></returns>
        protected RefundNotifyInfo GetRefundNotifyInfo(RefundResultInfo resultInfo, string tradeNo)
        {
            var notifyInfo = new RefundNotifyInfo()
            {
                OrderId = resultInfo.OrderId,
                TradeNo = tradeNo,
                RefundOrderId = resultInfo.RefundOrderId,
                RefundNo = resultInfo.RefundNo,
                RefundAmt = resultInfo.RefundAmt.ToString(),
                Result = resultInfo.ExecuteResult.ToString(),
            };
            return notifyInfo;
        }
    }

    /// <summary>
    /// 模拟退款结果
    /// </summary>
    public class SimulateRefundResult : RefundResult, IRefundResult
    {
        /// <summary>
        /// 校验异步退款结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public override ExecuteResult CheckRequest(string data, RefundResultInfo resultInfo)
        {
            return new ExecuteResult();
        }

        /// <summary>
        /// 解析异步退款结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<RefundNotifyInfo> ResolveRequest(string data, RefundResultInfo resultInfo)
        {
            return new ExecuteResult<RefundNotifyInfo>();
        }

        /// <summary>
        /// 执行同步退款
        /// </summary>
        /// <param name="orderInfo">退款订单</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<RefundNotifyInfo> ExecuteSyncRefund(RefundOrderInfo orderInfo, RefundResultInfo resultInfo)
        {
            var result = new ExecuteResult<RefundNotifyInfo>();

            resultInfo.OrderId = orderInfo.OrderId;
            resultInfo.RefundOrderId = orderInfo.RefundOrderId;
            resultInfo.RefundAmt = Convert.ToDecimal(orderInfo.RefundAmt);
            resultInfo.RefundNo = DateTime.Now.ToString("yyyyMMddHHmmssfff") + RandomHelper.CreateRandomCode(3);
            resultInfo.ExecuteResult = (int)ResultStatus.Success;

            result.Data = this.GetRefundNotifyInfo(resultInfo, orderInfo.TradeNo);
            result.Status = ResultStatus.Success;
            return result;
        }
    }

    /// <summary>
    /// 微信退款结果
    /// </summary>
    public class WechatPayRefundResult : RefundResult, IRefundResult
    {
        /// <summary>
        /// 校验异步退款结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public override ExecuteResult CheckRequest(string data, RefundResultInfo resultInfo)
        {
            return new ExecuteResult();
        }

        /// <summary>
        /// 解析异步退款结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<RefundNotifyInfo> ResolveRequest(string data, RefundResultInfo resultInfo)
        {
            return new ExecuteResult<RefundNotifyInfo>();
        }

        /// <summary>
        /// 执行同步退款
        /// </summary>
        /// <param name="orderInfo">退款订单</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<RefundNotifyInfo> ExecuteSyncRefund(RefundOrderInfo orderInfo, RefundResultInfo resultInfo)
        {
            var result = new ExecuteResult<RefundNotifyInfo>();
            try
            {
                resultInfo.OrderId = orderInfo.OrderId;
                resultInfo.RefundOrderId = orderInfo.RefundOrderId;
                resultInfo.RefundAmt = Convert.ToDecimal(orderInfo.RefundAmt);
                int paymentAmt = (int)(Convert.ToDecimal(orderInfo.PaymentAmt) * 100);//微信支付金额的单位为“分”，所以要乘以100
                int refundAmt = (int)(resultInfo.RefundAmt * 100);//微信退款金额的单位为“分”，所以要乘以100

                //提交退款请求
                WxPayData wxData = PayRefund.Run(orderInfo.TradeNo, orderInfo.OrderId, paymentAmt, refundAmt, orderInfo.RefundOrderId);

                //校验返回参数
                if (wxData.GetValue("return_code").ToString() != "SUCCESS")
                {
                    resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                    resultInfo.ResultDesc = "微信退款请求失败：" + wxData.GetValue("return_msg").ToString();
                    RefundResultDAL.Update(resultInfo);

                    result.Status = ResultStatus.Failure;
                    result.Message = resultInfo.ResultDesc;
                    return result;
                }
                else if (wxData.GetValue("result_code").ToString() != "SUCCESS")
                {
                    resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                    resultInfo.ResultDesc = "微信退款执行失败：" + wxData.GetValue("err_code_des").ToString();
                    RefundResultDAL.Update(resultInfo);

                    result.Status = ResultStatus.Failure;
                    result.Message = resultInfo.ResultDesc;
                    return result;
                }

                //退款成功，解析返回参数
                resultInfo.RefundNo = wxData.GetValue("refund_id").ToString();
                resultInfo.ExecuteResult = (int)ResultStatus.Success;

                result.Data = this.GetRefundNotifyInfo(resultInfo, orderInfo.TradeNo);
                result.Status = ResultStatus.Success;
            }
            catch (WxPayException wex)
            {
                resultInfo.ExecuteResult = (int)ResultStatus.Error;
                resultInfo.ResultDesc = wex.Message;
                RefundResultDAL.Update(resultInfo);

                result.Status = ResultStatus.Error;
                result.Message = wex.Message;
            }
            catch (Exception ex)
            {
                resultInfo.ExecuteResult = (int)ResultStatus.Error;
                resultInfo.ResultDesc = ex.ToString();
                RefundResultDAL.Update(resultInfo);

                result.Status = ResultStatus.Error;
                result.Message = ex.Message;
            }
            return result;
        }
    }

    /// <summary>
    /// 通联退款结果
    /// </summary>
    public class AllinpayRefundResult : RefundResult, IRefundResult
    {
        /// <summary>
        /// 校验异步退款结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public override ExecuteResult CheckRequest(string data, RefundResultInfo resultInfo)
        {
            return new ExecuteResult();
        }

        /// <summary>
        /// 解析异步退款结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<RefundNotifyInfo> ResolveRequest(string data, RefundResultInfo resultInfo)
        {
            return new ExecuteResult<RefundNotifyInfo>();
        }

        /// <summary>
        /// 执行同步退款
        /// </summary>
        /// <param name="orderInfo">退款订单</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<RefundNotifyInfo> ExecuteSyncRefund(RefundOrderInfo orderInfo, RefundResultInfo resultInfo)
        {
            var result = new ExecuteResult<RefundNotifyInfo>();
            try
            {
                resultInfo.OrderId = orderInfo.OrderId;
                resultInfo.RefundOrderId = orderInfo.RefundOrderId;
                resultInfo.RefundAmt = Convert.ToDecimal(orderInfo.RefundAmt);
                //获取退款金额
                int refundAmt = (int)(resultInfo.RefundAmt * 100);//通联退款金额的单位为“分”，所以要乘以100
                var refundData = new AllinpayData(AllinpayDataType.RefundRequest);
                //商户号
                refundData.SetValue("merchantId", AppConfig.Global.AllinpayMerchantId);
                //商户订单号
                refundData.SetValue("orderNo", orderInfo.OrderId);
                //退款金额
                refundData.SetValue("refundAmount", refundAmt.ToString());
                //商户订单提交时间
                refundData.SetValue("orderDatetime", orderInfo.OrderTime);
                //签名字符串
                refundData.SetValue("signMsg", AllinpayCore.RefundSign(refundData));

                var executeResult = AllinpayManager.RefundRequest(refundData);
                if (executeResult.Status != ResultStatus.Success)
                {
                    resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                    resultInfo.ResultDesc = "通联退款请求失败：" + executeResult.Message;
                    RefundResultDAL.Update(resultInfo);

                    result.Status = ResultStatus.Error;
                    result.Message = resultInfo.ResultDesc;
                    return result;
                }

                //退款成功，解析返回参数
                resultInfo.RefundNo = "";
                resultInfo.ExecuteResult = (int)ResultStatus.Success;

                result.Data = this.GetRefundNotifyInfo(resultInfo, orderInfo.TradeNo);
                result.Status = ResultStatus.Success;
            }
            catch (Exception ex)
            {
                resultInfo.ExecuteResult = (int)ResultStatus.Error;
                resultInfo.ResultDesc = ex.Message;
                RefundResultDAL.Update(resultInfo);

                result.Status = ResultStatus.Error;
                result.Message = ex.Message;
            }
            return result;
        }
    }
}
