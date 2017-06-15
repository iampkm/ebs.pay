using CQSS.Pay.Model;
using CQSS.Pay.Model.Api;
using CQSS.Pay.Model.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Interface
{
    /// <summary>
    /// 支付结果
    /// </summary>
    public interface IPayResult : IPayBase
    {
        /// <summary>
        /// 保存支付结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        PayResultInfo SaveRequest(string data, AppEnum.PayType payType);

        /// <summary>
        /// 校验支付结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        ExecuteResult CheckRequest(string data, PayResultInfo resultInfo);

        /// <summary>
        /// 解析支付结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        ExecuteResult<PayNotifyInfo> ResolveRequest(string data, PayResultInfo resultInfo);

        /// <summary>
        /// 执行条码支付
        /// </summary>
        /// <param name="orderInfo">支付订单</param>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        ExecuteResult<PayNotifyInfo> ExecuteBarcodePay(PayOrderInfo orderInfo, PayResultInfo resultInfo);

        /// <summary>
        /// 更新支付结果记录并关联支付请求记录
        /// </summary>
        /// <param name="resultInfo">支付结果记录</param>
        /// <returns></returns>
        PayRequestInfo RelateRequestInfo(PayResultInfo resultInfo);

        /// <summary>
        /// 通知业务系统支付结果
        /// </summary>
        /// <param name="resultInfo">支付结果记录</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        ExecuteResult NotifyBack(PayResultInfo resultInfo, PayRequestInfo requestInfo);

        /// <summary>
        /// 获取返回链接
        /// </summary>
        /// <param name="requestInfo">支付请求记录</param>
        /// <param name="notifyInfo">支付结果通知</param>
        /// <returns></returns>
        string GetReturnUrl(PayRequestInfo requestInfo, PayNotifyInfo notifyInfo);
    }
}
