using CQSS.Pay.Model;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.DAL
{
    public class PayRefundDAL
    {
        /// <summary>
        /// 插入支付退款记录
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static int Insert(PayRefundInfo info)
        {
            string sql = @"
INSERT  INTO Pay_Refund
        (OrderId,TradeNo,PaymentAmt,RefundOrderId,RefundAmt,RefundNo,PayType,RequestData,ExecuteResult,ResultDesc,RequestSystemId,CreateTime)
VALUES  (@OrderId,@TradeNo,@PaymentAmt,@RefundOrderId,@RefundAmt,@RefundNo,@PayType,@RequestData,@ExecuteResult,@ResultDesc,@RequestSystemId,@CreateTime);
SELECT  SCOPE_IDENTITY();";
            info.SysNo = DbHelper.QueryScalar<int>(sql, info);
            return info.SysNo;
        }

        /// <summary>
        /// 更新支付退款记录（不更新RequestData字段）
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool Update(PayRefundInfo info)
        {
            string sql = @"
UPDATE  Pay_Refund
SET     OrderId=@OrderId,
        TradeNo=@TradeNo,
        PaymentAmt=@PaymentAmt,
        RefundOrderId=@RefundOrderId,
        RefundAmt=@RefundAmt,
        RefundNo=@RefundNo,
        PayType=@PayType,
        ExecuteResult=@ExecuteResult,
        ResultDesc=@ResultDesc,
        RequestSystemId=@RequestSystemId
WHERE   SysNo=@SysNo";
            int count = DbHelper.Execute(sql, info);
            return count > 0;
        }
    }
}
