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
    public class RefundResultDAL
    {
        /// <summary>
        /// 插入退款结果回执记录
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static int Insert(RefundResultInfo info)
        {
            string sql = @"
INSERT  INTO Refund_Result
        (RequestSysNo,OrderId,RefundOrderId,RefundAmt,RefundNo,PayType,RequestData,ExecuteResult,ResultDesc,NotifyStatus,CreateTime)
VALUES  (@RequestSysNo,@OrderId,@RefundOrderId,@RefundAmt,@RefundNo,@PayType,@RequestData,@ExecuteResult,@ResultDesc,@NotifyStatus,@CreateTime);
SELECT  SCOPE_IDENTITY();";
            info.SysNo = DbHelper.QueryScalar<int>(sql, info);
            return info.SysNo;
        }

        /// <summary>
        /// 更新退款结果回执记录（不更新PayType、RequestData字段）
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool Update(RefundResultInfo info)
        {
            if (info.SysNo <= 0)
                return false;

            string sql = @"
UPDATE  Refund_Result
SET     RequestSysNo=@RequestSysNo,
        OrderId=@OrderId,
        RefundOrderId=@RefundOrderId,
        RefundAmt=@RefundAmt,
        RefundNo=@RefundNo,
        ExecuteResult=@ExecuteResult,
        ResultDesc=@ResultDesc,
        NotifyStatus=@NotifyStatus
WHERE   SysNo=@SysNo";
            int count = DbHelper.Execute(sql, info);
            return count > 0;
        }

        /// <summary>
        /// 写入业务系统通知记录
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static int InsertNotifyBack(NotifyBackInfo info)
        {
            string sql = @"
INSERT  INTO Refund_NotifyBack
        (ResultSysNo,Msg,Status,ResponseData,CreateTime)
VALUES  (@ResultSysNo,@Msg,@Status,@ResponseData,@CreateTime);
SELECT  SCOPE_IDENTITY();";
            info.SysNo = DbHelper.QueryScalar<int>(sql, info);
            return info.SysNo;
        }

        /// <summary>
        /// 获取订单已退款的总金额
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public static decimal GetRefundedAmt(string orderId, AppEnum.PayType payType)
        {
            string sql = "SELECT ISNULL(SUM(RefundAmt),0) FROM Refund_Result WHERE OrderId=@orderId AND PayType=@payType AND ExecuteResult=@result";
            var refundedAmt = DbHelper.QueryScalar<decimal>(sql, new
            {
                orderId = orderId,
                payType = (int)payType,
                result = (int)ResultStatus.Success
            });
            return refundedAmt;
        }

        /// <summary>
        /// 是否存在有效的退款结果记录
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="refundOrderId"></param>
        /// <param name="refundNo"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public static bool ExistValidRefundResult(RefundResultInfo info)
        {
            string sql = @"
SELECT  COUNT(1)
FROM    Refund_Result
WHERE   OrderId=@orderId
        AND RefundOrderId=@refundOrderId
        AND RefundNo=@refundNo
        AND PayType=@payType
        AND ExecuteResult=@result;";
            int count = DbHelper.QueryScalar<int>(sql, new
            {
                orderId = info.OrderId,
                refundOrderId = info.RefundOrderId,
                refundNo = info.RefundNo,
                payType = info.PayType,
                result = (int)ResultStatus.Success
            });
            return count > 0;
        }
    }
}
