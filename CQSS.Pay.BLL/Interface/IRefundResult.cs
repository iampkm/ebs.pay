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
    /// 退款结果
    /// </summary>
    public interface IRefundResult : IPayBase
    {
        /// <summary>
        /// 保存退款结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        RefundResultInfo SaveRequest(string data, AppEnum.PayType payType);

        /// <summary>
        /// 校验异步退款结果请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        ExecuteResult CheckRequest(string data, RefundResultInfo resultInfo);

        /// <summary>
        /// 解析异步退款结果
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        ExecuteResult<RefundNotifyInfo> ResolveRequest(string data, RefundResultInfo resultInfo);

        /// <summary>
        /// 执行同步退款
        /// </summary>
        /// <param name="orderInfo">退款订单</param>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        ExecuteResult<RefundNotifyInfo> ExecuteSyncRefund(RefundOrderInfo orderInfo, RefundResultInfo resultInfo);

        /// <summary>
        /// 更新退款结果记录并关联退款请求记录
        /// </summary>
        /// <param name="resultInfo">退款结果记录</param>
        /// <returns></returns>
        ExecuteResult<RefundRequestInfo> RelateRequestInfo(RefundResultInfo resultInfo);

        /// <summary>
        /// 通知业务系统退款结果
        /// </summary>
        /// <param name="resultInfo">退款结果记录</param>
        /// <param name="requestInfo">退款请求记录</param>
        /// <returns></returns>
        ExecuteResult NotifyBack(RefundResultInfo resultInfo, RefundRequestInfo requestInfo);
    }
}
