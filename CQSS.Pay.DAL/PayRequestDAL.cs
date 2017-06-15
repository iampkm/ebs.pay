using CQSS.Pay.Model;
using CQSS.Pay.Model.Data;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.DAL
{
    public class PayRequestDAL
    {
        /// <summary>
        /// 插入支付请求记录
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static int Insert(PayRequestInfo info)
        {
            string sql = @"
INSERT  INTO Pay_Request
        (OrderId,PaymentAmt,PayType,NotifyUrl,ReturnUrl,RequestData,ExecuteResult,ResultDesc,AppId,Status,CreateTime)
VALUES  (@OrderId,@PaymentAmt,@PayType,@NotifyUrl,@ReturnUrl,@RequestData,@ExecuteResult,@ResultDesc,@AppId,@Status,@CreateTime);
SELECT  SCOPE_IDENTITY();";
            info.SysNo = DbHelper.QueryScalar<int>(sql, info);
            return info.SysNo;
        }

        /// <summary>
        /// 更新支付请求记录（不更新PayType、RequestData、AppId字段）
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool Update(PayRequestInfo info)
        {
            if (info == null || info.SysNo <= 0)
                return false;

            string sql = @"
UPDATE  Pay_Request
SET     OrderId=@OrderId,
        PaymentAmt=@PaymentAmt,
        NotifyUrl=@NotifyUrl,
        ReturnUrl=@ReturnUrl,
        ExecuteResult=@ExecuteResult,
        ResultDesc=@ResultDesc,
        Status=@Status
WHERE   SysNo=@SysNo";
            int count = DbHelper.Execute(sql, info);
            return count > 0;
        }

        /// <summary>
        /// 作废重复的支付请求记录
        /// </summary>
        /// <returns></returns>
        public static bool InvalidateRepeatRequest(PayRequestInfo info)
        {
            string sql = "UPDATE Pay_Request SET Status=@status WHERE OrderId=@orderId AND PayType=@payType AND SysNo<@sysNo AND NOT EXISTS (SELECT 1 FROM Pay_Result WHERE RequestSysNo=Pay_Request.SysNo)";
            var param = new { status = (int)AppEnum.GlobalStatus.Invalid, orderId = info.OrderId, payType = (int)info.PayType, sysNo = info.SysNo };
            int count = DbHelper.Execute(sql, param);
            return count > 0;
        }

        /// <summary>
        /// 根据订单编号和支付方式，查询有效的支付请求记录
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public static PayRequestInfo GetValidPayRequest(string orderId, AppEnum.PayType payType)
        {
            string sql = @"
SELECT  SysNo,
        OrderId,
        PaymentAmt,
        PayType,
        NotifyUrl,
        ReturnUrl,
        ExecuteResult,
        ResultDesc,
        AppId,
        Status,
        CreateTime
FROM    Pay_Request
WHERE   OrderId=@orderId
        AND PayType=@payType
        AND Status=@status
ORDER BY SysNo";
            PayRequestInfo requestInfo = DbHelper.QuerySingle<PayRequestInfo>(sql, new
            {
                orderId = orderId,
                payType = (int)payType,
                status = (int)AppEnum.GlobalStatus.Valid
            });
            return requestInfo;
        }

        /// <summary>
        /// 查询支付请求记录
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public static PayRequestInfo GetPayRequest(int sysNo)
        {
            string sql = @"SELECT * FROM Pay_Request WHERE SysNo=@sysNo";
            PayRequestInfo requestInfo = DbHelper.QuerySingle<PayRequestInfo>(sql, new { sysNo = sysNo });
            return requestInfo;
        }
    }
}
