using CQSS.Pay.BLL.Alipay;
using CQSS.Pay.BLL.Allinpay.Api;
using CQSS.Pay.BLL.Basic;
using CQSS.Pay.BLL.Interface;
using CQSS.Pay.BLL.SwfitPass.Api;
using CQSS.Pay.BLL.WeChatPay.Api;
using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Model.Api;
using CQSS.Pay.Model.Data;
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
    /// 支付结果
    /// </summary>
    public abstract class PayResult : PayBase, IPayResult
    {
        /// <summary>
        /// 保存支付结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        public virtual PayResultInfo SaveRequest(string data, AppEnum.PayType payType)
        {
            var resultInfo = new PayResultInfo()
            {
                PayType = (int)payType,
                RequestData = data,
                ExecuteResult = (int)ResultStatus.None,
                NotifyStatus = (int)AppEnum.NotifyStatus.Original,
                CreateTime = DateTime.Now,
            };
            resultInfo.SysNo = PayResultDAL.Insert(resultInfo);
            return resultInfo;
        }

        /// <summary>
        /// 校验支付结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public abstract ExecuteResult CheckRequest(string data, PayResultInfo resultInfo);

        /// <summary>
        /// 解析支付结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public abstract ExecuteResult<PayNotifyInfo> ResolveRequest(string data, PayResultInfo resultInfo);

        /// <summary>
        /// 执行条码支付
        /// </summary>
        /// <param name="orderInfo">支付订单</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public virtual ExecuteResult<PayNotifyInfo> ExecuteBarcodePay(PayOrderInfo orderInfo, PayResultInfo resultInfo)
        {
            return new ExecuteResult<PayNotifyInfo>();
        }

        /// <summary>
        /// 更新支付结果记录并关联支付请求记录
        /// </summary>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public virtual PayRequestInfo RelateRequestInfo(PayResultInfo resultInfo)
        {
            //判断是否存在有效的支付结果记录，如果存在，则当前支付结果记录不执行
            bool exist = PayResultDAL.ExistValidPayResult(resultInfo.OrderId, resultInfo.TradeNo, (AppEnum.PayType)resultInfo.PayType);
            if (exist)
            {
                resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                resultInfo.ResultDesc = "已存在有效的支付结果记录";
            }

            PayRequestInfo requestInfo = null;
            if (resultInfo.ExecuteResult == (int)ResultStatus.Success)
            {
                requestInfo = PayRequestDAL.GetValidPayRequest(resultInfo.OrderId, (AppEnum.PayType)resultInfo.PayType);
                if (requestInfo != null && requestInfo.SysNo > 0)
                {
                    resultInfo.RequestSysNo = requestInfo.SysNo;
                }
            }

            //更新支付结果记录信息
            PayResultDAL.Update(resultInfo);

            return requestInfo;
        }

        /// <summary>
        /// 通知业务系统支付结果
        /// </summary>
        /// <param name="resultInfo">支付结果记录</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        public virtual ExecuteResult NotifyBack(PayResultInfo resultInfo, PayRequestInfo requestInfo)
        {
            var result = new ExecuteResult() { Status = ResultStatus.Failure };

            //支付结果记录对象无效，则不执行
            if (resultInfo == null || resultInfo.SysNo <= 0)
            {
                result.Message = "支付结果记录对象无效";
                return result;
            }

            //支付请求记录对象无效，则不执行
            if (requestInfo == null || requestInfo.SysNo <= 0)
            {
                result.Message = "支付请求记录对象无效";
                return result;
            }

            //支付结果记录与支付请求记录不对应，则不执行
            if (requestInfo.SysNo != resultInfo.RequestSysNo)
            {
                result.Message = "支付结果记录与支付请求记录不对应";
                return result;
            }

            //支付结果记录未成功执行，或者已通知，则不执行
            if (resultInfo.ExecuteResult != (int)ResultStatus.Success || resultInfo.NotifyStatus == (int)AppEnum.NotifyStatus.Finished)
            {
                result.Message = "支付结果记录未成功执行或已通知成功";
                return result;
            }

            //支付请求记录中不存在有效的通知地址，则不执行
            if (!requestInfo.NotifyUrl.IsUrl())
            {
                result.Message = "支付请求记录中不存在有效的通知地址";
                return result;
            }

            var notifyInfo = this.GetPayNotifyInfo(resultInfo);
            var setting = JsonHelper.GetDefaultSettings();
            setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            string data = JsonHelper.Serialize(notifyInfo, setting);
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
                PayResultDAL.Update(resultInfo);
                result.Status = ResultStatus.Success;
            }
            PayResultDAL.InsertNotifyBack(backInfo);
            return result;
        }

        /// <summary>
        /// 获取返回链接
        /// </summary>
        /// <param name="requestInfo">支付请求记录</param>
        /// <param name="notifyInfo">支付结果通知</param>
        /// <returns></returns>
        public virtual string GetReturnUrl(PayRequestInfo requestInfo, PayNotifyInfo notifyInfo)
        {
            if (requestInfo != null && !string.IsNullOrWhiteSpace(requestInfo.ReturnUrl))
            {
                var setting = JsonHelper.GetDefaultSettings();
                setting.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                string data = JsonHelper.Serialize(notifyInfo, setting);
                string sign = SignManager.CreateSign(requestInfo.AppId, data).Data;
                string returnUrl = requestInfo.ReturnUrl + (requestInfo.ReturnUrl.IndexOf("?") > 0 ? "&" : "?");
                returnUrl += string.Format("appId={0}&sign={1}&data={2}", requestInfo.AppId, sign, data);
                return returnUrl;
            }
            return null;
        }

        /// <summary>
        /// 获取支付结果通知对象
        /// </summary>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        protected PayNotifyInfo GetPayNotifyInfo(PayResultInfo resultInfo)
        {
            var notifyInfo = new PayNotifyInfo()
            {
                OrderId = resultInfo.OrderId,
                TradeNo = resultInfo.TradeNo,
                PaymentAmt = resultInfo.PaymentAmt.ToString(),
                ExtTradeNo = resultInfo.ExtTradeNo,
                Result = resultInfo.ExecuteResult.ToString(),
            };
            return notifyInfo;
        }
    }

    /// <summary>
    /// 模拟支付结果
    /// </summary>
    public class SimulatePayResult : PayResult, IPayResult
    {
        /// <summary>
        /// 校验支付结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult CheckRequest(string data, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult();
            result.Status = ResultStatus.Success;
            return result;
        }

        /// <summary>
        /// 解析支付结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<PayNotifyInfo> ResolveRequest(string data, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult<PayNotifyInfo>();

            var orderInfo = JsonHelper.Deserialize<PayOrderInfo>(data);
            resultInfo.OrderId = orderInfo.OrderId;
            resultInfo.TradeNo = DateTime.Now.ToString("yyyyMMddHHmmssfff") + RandomHelper.CreateRandomCode(3);
            resultInfo.PaymentAmt = Convert.ToDecimal(orderInfo.PaymentAmt);
            resultInfo.ExecuteResult = (int)ResultStatus.Success;

            result.Data = this.GetPayNotifyInfo(resultInfo);
            result.Status = ResultStatus.Success;
            return result;
        }

        /// <summary>
        /// 执行条码支付
        /// </summary>
        /// <param name="orderInfo">支付订单</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<PayNotifyInfo> ExecuteBarcodePay(PayOrderInfo orderInfo, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult<PayNotifyInfo>();

            resultInfo.OrderId = orderInfo.OrderId;
            resultInfo.TradeNo = DateTime.Now.ToString("yyyyMMddHHmmssfff") + RandomHelper.CreateRandomCode(3);
            resultInfo.PaymentAmt = Convert.ToDecimal(orderInfo.PaymentAmt);
            resultInfo.ExecuteResult = (int)ResultStatus.Success;

            result.Data = this.GetPayNotifyInfo(resultInfo);
            result.Status = ResultStatus.Success;
            return result;
        }
    }

    /// <summary>
    /// 支付宝支付结果
    /// </summary>
    public class AlipayResult : PayResult, IPayResult
    {
        /// <summary>
        /// 校验支付结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult CheckRequest(string data, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult();
            var paramsData = new ParameterData();
            paramsData.FromUrl(data);
            var paramsDict = paramsData.GetValues();

            //判断是否有带返回参数
            if (paramsDict.Count == 0)
            {
                result.Status = ResultStatus.Failure;
                result.Message = resultInfo.ResultDesc = "支付结果无通知参数";
                resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                PayResultDAL.Update(resultInfo);
                return result;
            }

            //校验请求报文
            resultInfo.OrderId = paramsData.GetValue("out_trade_no");
            var aliNotify = new AlipayNotify();
            bool verifyResult = aliNotify.Verify(paramsDict, paramsData.GetValue("notify_id"), paramsData.GetValue("sign"));
            if (!verifyResult)
            {
                result.Status = ResultStatus.Failure;
                result.Message = resultInfo.ResultDesc = "verify failed";
                resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                PayResultDAL.Update(resultInfo);
                return result;
            }

            result.Status = ResultStatus.Success;
            return result;
        }

        /// <summary>
        /// 解析支付结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<PayNotifyInfo> ResolveRequest(string data, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult<PayNotifyInfo>();
            try
            {
                var paramsData = new ParameterData();
                paramsData.FromUrl(data);

                resultInfo.OrderId = paramsData.GetValue("out_trade_no");
                resultInfo.TradeNo = paramsData.GetValue("trade_no");
                resultInfo.PaymentAmt = Convert.ToDecimal(paramsData.GetValue("total_fee"));
                string tradeStatus = paramsData.GetValue("trade_status");
                var tradeResult = (tradeStatus == "TRADE_FINISHED" || tradeStatus == "TRADE_SUCCESS") ? ResultStatus.Success : ResultStatus.Failure;
                resultInfo.ResultDesc = tradeStatus;
                resultInfo.ExecuteResult = (int)tradeResult;

                result.Data = this.GetPayNotifyInfo(resultInfo);
                result.Status = ResultStatus.Success;
            }
            catch
            {
                result.Status = ResultStatus.Error;
                resultInfo.ResultDesc = result.Message = "解析支付结果参数失败";
                resultInfo.ExecuteResult = (int)ResultStatus.Error;
                PayResultDAL.Update(resultInfo);
            }
            return result;
        }
    }

    /// <summary>
    /// 微信支付结果
    /// </summary>
    public class WeChatPayResult : PayResult, IPayResult
    {
        /// <summary>
        /// 校验支付结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult CheckRequest(string data, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult();
            var errorData = new WxPayData();
            try
            {
                //校验请求报文
                var notifyData = new ParameterData();
                notifyData.FromXml(data);

                //检查支付结果中transaction_id是否存在
                resultInfo.OrderId = notifyData.GetValue("out_trade_no") as string;
                resultInfo.TradeNo = notifyData.GetValue("transaction_id") as string;
                if (string.IsNullOrEmpty(resultInfo.TradeNo))
                {
                    resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                    resultInfo.ResultDesc = "支付结果中微信支付单号不存在";
                    PayResultDAL.Update(resultInfo);

                    errorData.SetValue("return_code", "FAIL");
                    errorData.SetValue("return_msg", resultInfo.ResultDesc);
                    result.Message = errorData.ToXml();
                    result.Status = ResultStatus.Failure;
                    return result;
                }

                //查询支付单，判断支付单真实性
                var req = new WxPayData();
                req.SetValue("transaction_id", resultInfo.TradeNo);
                var queryData = WxPayApi.OrderQuery(req);
                if (queryData.GetValue("return_code").ToString() != "SUCCESS" || queryData.GetValue("result_code").ToString() != "SUCCESS")
                {
                    resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                    resultInfo.ResultDesc = "微信支付单查询失败";
                    PayResultDAL.Update(resultInfo);

                    errorData.SetValue("return_code", "FAIL");
                    errorData.SetValue("return_msg", resultInfo.ResultDesc);
                    result.Message = errorData.ToXml();
                    result.Status = ResultStatus.Failure;
                    return result;
                }
            }
            catch (WxPayException wex)
            {
                resultInfo.ExecuteResult = (int)ResultStatus.Error;
                resultInfo.ResultDesc = wex.Message;
                PayResultDAL.Update(resultInfo);

                errorData.SetValue("return_code", "FAIL");
                errorData.SetValue("return_msg", resultInfo.ResultDesc);
                result.Message = errorData.ToXml();
                result.Status = ResultStatus.Error;
                return result;
            }
            result.Status = ResultStatus.Success;
            return result;
        }

        /// <summary>
        /// 解析支付结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<PayNotifyInfo> ResolveRequest(string data, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult<PayNotifyInfo>();
            try
            {
                var notifyData = new ParameterData();
                notifyData.FromXml(data);

                resultInfo.OrderId = notifyData.GetValue("out_trade_no") as string;
                resultInfo.TradeNo = notifyData.GetValue("transaction_id") as string;
                resultInfo.PaymentAmt = Convert.ToDecimal(notifyData.GetValue("total_fee")) / 100;//微信支付金额的单位为“分”，所以要除以100
                string tradeStatus = notifyData.GetValue("return_code");
                var tradeResult = ResultStatus.Success;//由于在校验支付结果请求时已经验证了是否支付成功，所以此处肯定是支付成功的
                resultInfo.ResultDesc = tradeStatus;
                resultInfo.ExecuteResult = (int)tradeResult;

                result.Data = this.GetPayNotifyInfo(resultInfo);
                result.Status = ResultStatus.Success;
            }
            catch
            {
                resultInfo.ResultDesc = "解析支付结果参数失败";
                resultInfo.ExecuteResult = (int)ResultStatus.Error;
                PayResultDAL.Update(resultInfo);

                var errorData = new WxPayData();
                errorData.SetValue("return_code", "FAIL");
                errorData.SetValue("return_msg", resultInfo.ResultDesc);
                result.Message = errorData.ToXml();
                result.Status = ResultStatus.Error;
            }
            return result;
        }

        /// <summary>
        /// 执行条码支付
        /// </summary>
        /// <param name="orderInfo">支付订单</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<PayNotifyInfo> ExecuteBarcodePay(PayOrderInfo orderInfo, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult<PayNotifyInfo>();
            try
            {
                int paymentAmt = (int)(Convert.ToDecimal(orderInfo.PaymentAmt) * 100);//微信支付金额的单位为“分”，所以要乘以100
                WxPayData runData = MicroPay.Run(orderInfo.OrderId, paymentAmt, orderInfo.Barcode);
                if (runData.GetValue("return_code").ToString() != "SUCCESS")
                {
                    resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                    resultInfo.ResultDesc = "微信条码支付请求失败：" + runData.GetValue("return_msg").ToString();
                    PayResultDAL.Update(resultInfo);

                    result.Status = ResultStatus.Failure;
                    result.Message = resultInfo.ResultDesc;
                    return result;
                }
                else if (runData.GetValue("result_code").ToString() != "SUCCESS")
                {
                    resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                    resultInfo.ResultDesc = "微信条码支付失败：" + runData.GetValue("err_code_des").ToString();
                    PayResultDAL.Update(resultInfo);

                    result.Status = ResultStatus.Failure;
                    result.Message = resultInfo.ResultDesc;
                    return result;
                }

                //支付成功，更新支付结果记录
                resultInfo.OrderId = orderInfo.OrderId;
                resultInfo.TradeNo = runData.GetValue("transaction_id").ToString();
                resultInfo.PaymentAmt = Convert.ToDecimal(runData.GetValue("total_fee")) / 100; //微信支付金额的单位为“分”，所以要除以100
                resultInfo.ExecuteResult = (int)ResultStatus.Success;

                result.Data = this.GetPayNotifyInfo(resultInfo);
                result.Status = ResultStatus.Success;
            }
            catch (WxPayException wex)
            {
                resultInfo.ExecuteResult = (int)ResultStatus.Error;
                resultInfo.ResultDesc = wex.Message;
                PayResultDAL.Update(resultInfo);

                result.Status = ResultStatus.Error;
                result.Message = resultInfo.ResultDesc;
            }
            catch (Exception ex)
            {
                resultInfo.ExecuteResult = (int)ResultStatus.Error;
                resultInfo.ResultDesc = ex.ToString();
                PayResultDAL.Update(resultInfo);

                result.Status = ResultStatus.Error;
                result.Message = ex.Message;
            }
            return result;
        }
    }

    /// <summary>
    /// 通联支付结果
    /// </summary>
    public class AllinpayResult : PayResult, IPayResult
    {
        /// <summary>
        /// 校验支付结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult CheckRequest(string data, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult();
            var notifyData = new AllinpayData(AllinpayDataType.PayResult);
            notifyData.FromUrl(data);

            //校验参数个数
            if (notifyData.GetValues().Count == 0)
            {
                result.Status = ResultStatus.Failure;
                result.Message = resultInfo.ResultDesc = "支付结果无通知参数";
                resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                PayResultDAL.Update(resultInfo);
                return result;
            }

            //校验参数合法性
            resultInfo.OrderId = notifyData.GetValue("orderNo");
            bool verifyResult = AllinpayCore.VerifyResultSign(notifyData);
            if (!verifyResult)
            {
                result.Status = ResultStatus.Failure;
                result.Message = resultInfo.ResultDesc = "verify failed";
                resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                PayResultDAL.Update(resultInfo);
                return result;
            }

            result.Status = ResultStatus.Success;
            return result;
        }

        /// <summary>
        /// 解析支付结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<PayNotifyInfo> ResolveRequest(string data, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult<PayNotifyInfo>();
            try
            {
                var notifyData = new ParameterData();
                notifyData.FromUrl(data);

                resultInfo.OrderId = notifyData.GetValue("orderNo");
                resultInfo.TradeNo = notifyData.GetValue("paymentOrderId");
                resultInfo.PaymentAmt = Convert.ToDecimal(notifyData.GetValue("payAmount")) / 100;//通联支付金额单位为“分”，所以除以100
                string tradeStatus = notifyData.GetValue("payResult");
                var tradeResult = tradeStatus == "1" ? ResultStatus.Success : ResultStatus.Failure;
                resultInfo.ResultDesc = tradeStatus;
                resultInfo.ExecuteResult = (int)tradeResult;

                result.Data = this.GetPayNotifyInfo(resultInfo);
                result.Status = ResultStatus.Success;
            }
            catch
            {
                result.Status = ResultStatus.Error;
                result.Message = "解析支付结果参数失败";
            }
            return result;
        }
    }

    /// <summary>
    /// 威富通微信支付
    /// </summary>
    public class SwiftPassWeChatPayResult : PayResult, IPayResult
    {
        /// <summary>
        /// 校验支付结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult CheckRequest(string data, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult();
            try
            {
                //校验请求报文
                var notifyData = new ParameterData();
                notifyData.FromXml(data);
                resultInfo.OrderId = notifyData.GetValue("out_trade_no");
                if (notifyData.GetValue("status") != "0") //校验返回状态
                    throw new BizException("返回状态错误代码：" + notifyData.GetValue("status") + "，错误信息：" + notifyData.GetValue("message"));

                if (!SwiftPassCore.CheckSign(notifyData)) //校验数据签名
                    throw new BizException("签名校验未通过");

                if (notifyData.GetValue("result_code") != "0") //校验业务结果
                    throw new BizException("业务结果错误代码：" + notifyData.GetValue("err_code") + "，错误信息：" + notifyData.GetValue("err_msg"));

                if (notifyData.GetValue("pay_result") != "0") //校验支付结果
                {
                    resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                    resultInfo.ResultDesc = "用户支付失败：" + notifyData.GetValue("pay_info");
                    PayResultDAL.Update(resultInfo);

                    result.Message = resultInfo.ResultDesc;
                    result.Status = ResultStatus.Failure;
                    return result;
                }

                //检查支付结果中transaction_id是否存在
                resultInfo.TradeNo = notifyData.GetValue("transaction_id");
                if (string.IsNullOrEmpty(resultInfo.TradeNo))
                {
                    resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                    resultInfo.ResultDesc = "支付结果中平台交易单号不存在";
                    PayResultDAL.Update(resultInfo);

                    result.Message = resultInfo.ResultDesc;
                    result.Status = ResultStatus.Failure;
                    return result;
                }

                //查询支付单，判断支付单真实性
                var queryData = SwiftPassPayApi.OrderQuery(resultInfo.OrderId, resultInfo.TradeNo);
                if (queryData.GetValue("status") != "0" //校验返回状态
                    || queryData.GetValue("result_code") != "0" //校验业务结果
                    || !SwiftPassCore.CheckSign(queryData) //校验数据签名
                    || queryData.GetValue("trade_state") != "SUCCESS") //校验交易状态
                {
                    resultInfo.ExecuteResult = (int)ResultStatus.Failure;
                    resultInfo.ResultDesc = "平台交易单号查询失败";
                    PayResultDAL.Update(resultInfo);

                    result.Message = resultInfo.ResultDesc;
                    result.Status = ResultStatus.Failure;
                    return result;
                }
            }
            catch (BizException wex)
            {
                resultInfo.ExecuteResult = (int)ResultStatus.Error;
                resultInfo.ResultDesc = wex.Message;
                PayResultDAL.Update(resultInfo);

                result.Message = resultInfo.ResultDesc;
                result.Status = ResultStatus.Error;
                return result;
            }
            result.Status = ResultStatus.Success;
            return result;
        }

        /// <summary>
        /// 解析支付结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        public override ExecuteResult<PayNotifyInfo> ResolveRequest(string data, PayResultInfo resultInfo)
        {
            var result = new ExecuteResult<PayNotifyInfo>();
            try
            {
                var notifyData = new ParameterData();
                notifyData.FromXml(data);

                resultInfo.OrderId = notifyData.GetValue("out_trade_no");
                resultInfo.TradeNo = notifyData.GetValue("transaction_id");
                resultInfo.PaymentAmt = Convert.ToDecimal(notifyData.GetValue("total_fee")) / 100;//微信支付金额的单位为“分”，所以要除以100
                resultInfo.ExtTradeNo = notifyData.GetValue("out_transaction_id");
                string tradeStatus = notifyData.GetValue("pay_result");
                var tradeResult = ResultStatus.Success;//由于在校验支付结果请求时已经验证了是否支付成功，所以此处肯定是支付成功的
                resultInfo.ResultDesc = tradeStatus;
                resultInfo.ExecuteResult = (int)tradeResult;

                result.Data = this.GetPayNotifyInfo(resultInfo);
                result.Status = ResultStatus.Success;
            }
            catch
            {
                resultInfo.ResultDesc = "解析支付结果参数失败";
                resultInfo.ExecuteResult = (int)ResultStatus.Error;
                PayResultDAL.Update(resultInfo);

                result.Message = resultInfo.ResultDesc;
                result.Status = ResultStatus.Error;
            }
            return result;
        }
    }
}
