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
    public class RefundRequestDAL
    {
        /// <summary>
        /// 插入退款请求记录
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static int Insert(RefundRequestInfo info)
        {
            string sql = @"
INSERT  INTO Refund_Request
        (OrderId,TradeNo,RefundOrderId,RefundAmt,NotifyUrl,PayType,RequestData,ExecuteResult,ResultDesc,AppId,Status,CreateTime)
VALUES  (@OrderId,@TradeNo,@RefundOrderId,@RefundAmt,@NotifyUrl,@PayType,@RequestData,@ExecuteResult,@ResultDesc,@AppId,@Status,@CreateTime);
SELECT  SCOPE_IDENTITY();";
            info.SysNo = DbHelper.QueryScalar<int>(sql, info);
            return info.SysNo;
        }

        /// <summary>
        /// 更新退款请求记录（不更新PayType、RequestData、AppId字段）
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool Update(RefundRequestInfo info)
        {
            if (info == null || info.SysNo <= 0)
                return false;

            string sql = @"
UPDATE  Refund_Request
SET     OrderId=@OrderId,
        TradeNo=@TradeNo,
        RefundOrderId=@RefundOrderId,
        RefundAmt=@RefundAmt,
        NotifyUrl=@NotifyUrl,
        ExecuteResult=@ExecuteResult,
        ResultDesc=@ResultDesc,
        Status=@Status
WHERE   SysNo=@SysNo";
            int count = DbHelper.Execute(sql, info);
            return count > 0;
        }

        /// <summary>
        /// 作废重复的退款请求记录
        /// </summary>
        /// <returns></returns>
        public static bool InvalidateRepeatRequest(RefundRequestInfo info)
        {
            string sql = @"
UPDATE  Refund_Request
SET     Status=@status
WHERE   OrderId=@orderId
        AND RefundOrderId=@refundOrderId
        AND PayType=@payType
        AND SysNo<@sysNo
        AND NOT EXISTS ( SELECT 1
                         FROM   Refund_Result
                         WHERE  RequestSysNo=Refund_Request.SysNo )";
            var param = new
            {
                status = (int)AppEnum.GlobalStatus.Invalid,
                orderId = info.OrderId,
                refundOrderId = info.RefundOrderId,
                payType = (int)info.PayType,
                sysNo = info.SysNo
            };
            int count = DbHelper.Execute(sql, param);
            return count > 0;
        }

        /// <summary>
        /// 根据订单编号、退款单编号和支付方式，查询有效的退款请求记录
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="refundOrderId"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public static RefundRequestInfo GetValidRefundRequest(string orderId, string refundOrderId, AppEnum.PayType payType)
        {
            string sql = @"
SELECT  SysNo,
        OrderId,
        TradeNo,
        RefundOrderId,
        RefundAmt,
        NotifyUrl,
        PayType,
        ExecuteResult,
        AppId
FROM    Refund_Request
WHERE   OrderId=@orderId
        AND RefundOrderId=@refundOrderId
        AND PayType=@payType
        AND Status=@status
ORDER BY SysNo";
            var requestInfo = DbHelper.QuerySingle<RefundRequestInfo>(sql, new
            {
                orderId = orderId,
                refundOrderId = refundOrderId,
                payType = (int)payType,
                status = (int)AppEnum.GlobalStatus.Valid
            });
            return requestInfo;
        }
    }
}
