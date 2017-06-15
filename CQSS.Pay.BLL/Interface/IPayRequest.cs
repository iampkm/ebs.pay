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
    /// 支付请求
    /// </summary>
    public interface IPayRequest : IPayBase
    {
        /// <summary>
        /// 保存支付请求
        /// </summary>
        /// <param name="appId">业务系统ID</param>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付方式</param>
        /// <returns></returns>
        PayRequestInfo SaveRequest(string appId, string data, AppEnum.PayType payType);

        /// <summary>
        /// 校验数据签名
        /// </summary>
        /// <param name="appId">业务系统ID</param>
        /// <param name="sign">数据签名</param>
        /// <param name="data">业务数据报文</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        ExecuteResult CheckSign(string appId, string sign, string data, PayRequestInfo requestInfo);

        /// <summary>
        /// 校验浏览器是否是某种类型
        /// </summary>
        /// <param name="browserType">浏览器类型</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        ExecuteResult CheckBrowserType(AppEnum.BrowserType browserType, PayRequestInfo requestInfo);

        /// <summary>
        /// 校验支付参数
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="payType">支付类型</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        ExecuteResult<PayOrderInfo> CheckParamaters(string data, AppEnum.PayType payType, PayRequestInfo requestInfo);

        /// <summary>
        /// 解析支付请求
        /// </summary>
        /// <param name="data">业务数据报文</param>
        /// <param name="requestInfo">支付请求记录</param>
        /// <returns></returns>
        ExecuteResult ResolveRequest(string data, PayRequestInfo requestInfo);

        /// <summary>
        /// 支付请求执行成功
        /// </summary>
        /// <param name="requestInfo">支付请求记录</param>
        void ExecuteSuccess(PayRequestInfo requestInfo);
    }
}
