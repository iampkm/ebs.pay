using CQSS.Pay.Model;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.DAL
{
    public class PayResultDAL
    {
        /// <summary>
        /// 插入支付结果回执记录
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static int Insert(PayResultInfo info)
        {
            string sql = @"
INSERT  INTO Pay_Result
        (RequestSysNo,OrderId,PaymentAmt,PayType,RequestData,ExecuteResult,ResultDesc,NotifyStatus,CreateTime)
VALUES  (@RequestSysNo,@OrderId,@PaymentAmt,@PayType,@RequestData,@ExecuteResult,@ResultDesc,@NotifyStatus,@CreateTime);
SELECT  SCOPE_IDENTITY();";
            info.SysNo = DbHelper.QueryScalar<int>(sql, info);
            return info.SysNo;
        }

        /// <summary>
        /// 更新支付结果回执记录（不更新RequestData字段）
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool Update(PayResultInfo info)
        {
            string sql = @"
UPDATE  Pay_Result
SET     RequestSysNo=@RequestSysNo,
        OrderId=@OrderId,
        TradeNo=@TradeNo,
        PaymentAmt=@PaymentAmt,
        PayType=@PayType,
        ExecuteResult=@ExecuteResult,
        ResultDesc=@ResultDesc,
        NotifyStatus=@NotifyStatus
WHERE   SysNo=@SysNo";
            int count = DbHelper.Execute(sql, info);
            return count > 0;
        }

        /// <summary>
        /// 是否存在有效的支付结果记录
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="tradeNo"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public static bool ExistValidPayResult(string orderId, string tradeNo, AppEnum.PayType payType)
        {
            string sql = "SELECT COUNT(1) FROM Pay_Result WHERE OrderId=@orderId AND TradeNo=@tradeNo AND PayType=@payType AND ExecuteResult=@result";
            int count = DbHelper.QueryScalar<int>(sql, new
            {
                orderId = orderId,
                tradeNo = tradeNo,
                payType = (int)payType,
                result = (int)ResultStatus.Success
            });
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
INSERT  INTO Pay_NotifyBack
        (ResultSysNo,Status,Msg,CreateTime,ResponseData)
VALUES  (@ResultSysNo,@Status,@Msg,@CreateTime,@ResponseData);
SELECT  SCOPE_IDENTITY();";
            info.SysNo = DbHelper.QueryScalar<int>(sql, info);
            return info.SysNo;
        }

        /// <summary>
        /// 获取有效的支付记录
        /// </summary>
        /// <param name="orderId">订单编号</param>
        /// <param name="payType">支付类型</param>
        /// <returns></returns>
        public static PayResultInfo GetValidPayResult(string orderId, AppEnum.PayType payType)
        {
            string sql = @"
SELECT  SysNo,
        RequestSysNo,
        OrderId,
        TradeNo,
        PaymentAmt,
        PayType,
        ExecuteResult,
        ResultDesc,
        NotifyStatus,
        CreateTime
FROM    Pay_Result
WHERE   OrderId=@orderId
        AND PayType=@payType
        AND ExecuteResult=@result";
            var payResult = DbHelper.QuerySingle<PayResultInfo>(sql, new
            {
                orderId = orderId,
                payType = (int)payType,
                result = (int)ResultStatus.Success
            });
            return payResult;
        }

        /// <summary>
        /// 获取支付成功的支付记录
        /// </summary>
        /// <param name="pagger">分页信息</param>
        /// <param name="type">查询类型 0.待通知 1.全部</param>
        /// <returns></returns>
        public static List<PayResultInfo> GetPayResultList(Pagger pagger, int type)
        {
            string sql = @"
SELECT  *,
        ROW_NUMBER() OVER (ORDER BY SysNo DESC) AS RowIndex
FROM    Pay_Result
WHERE   ExecuteResult=@executeResult
        AND RequestSysNo>0";
            dynamic param = new ExpandoObject();
            param.executeResult = (int)ResultStatus.Success;
            if (type == 0)
            {
                sql += " AND NotifyStatus=@notifyStatus";
                param.notifyStatus = (int)AppEnum.GlobalStatus.Invalid;
            }

            pagger.TotalCount = CommonExecutor.GetDataCount(sql, param);
            return CommonExecutor.GetData<PayResultInfo>(pagger, sql, param);
        }

        /// <summary>
        /// 查询支付结果记录
        /// </summary>
        /// <param name="sysNo"></param>
        /// <returns></returns>
        public static PayResultInfo GetPayResult(int sysNo)
        {
            string sql = @"SELECT * FROM Pay_Result WHERE SysNo=@sysNo";
            PayResultInfo payResult = DbHelper.QuerySingle<PayResultInfo>(sql, new { sysNo = sysNo });
            return payResult;
        }
    }
}
