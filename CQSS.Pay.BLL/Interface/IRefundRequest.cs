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
    /// 退款申请
    /// </summary>
    public interface IRefundRequest : IPayBase
    {
        /// <summary>
        /// 保存退款请求
        /// </summary>
        /// <param name="appId">业务系统ID</param>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        RefundRequestInfo SaveRequest(string appId, string data, AppEnum.PayType payType);

        /// <summary>
        /// 校验数据签名
        /// </summary>
        /// <param name="appId">业务系统ID</param>
        /// <param name="sign">数据签名</param>
        /// <param name="data">业务数据报文</param>
        /// <param name="requestInfo">退款请求记录</param>
        /// <returns></returns>
        ExecuteResult CheckSign(string appId, string sign, string data, RefundRequestInfo requestInfo);

        /// <summary>
        /// 校验退款参数
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <param name="requestInfo">退款请求记录</param>
        /// <returns></returns>
        ExecuteResult<RefundOrderInfo> CheckParamaters(string data, AppEnum.PayType payType, RefundRequestInfo requestInfo);

        /// <summary>
        /// 解析退款请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="requestInfo">退款请求记录</param>
        /// <returns></returns>
        ExecuteResult ResolveRequest(string data, RefundRequestInfo requestInfo);

        /// <summary>
        /// 退款请求执行成功
        /// </summary>
        /// <param name="requestInfo">退款请求记录</param>
        void ExecuteSuccess(RefundRequestInfo requestInfo);
    }
}
