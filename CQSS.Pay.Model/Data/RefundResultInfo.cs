using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model.Data
{
    public class RefundResultInfo
    {
        /// <summary>
        /// 系统自增主键
        /// </summary>
        public int SysNo { get; set; }

        /// <summary>
        /// 退款请求记录主键
        /// </summary>
        public int RequestSysNo { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// 退款单编号
        /// </summary>
        public string RefundOrderId { get; set; }

        /// <summary>
        /// 退款金额
        /// </summary>
        public decimal RefundAmt { get; set; }

        /// <summary>
        /// 退款交易流水号
        /// </summary>
        public string RefundNo { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        public int PayType { get; set; }

        /// <summary>
        /// 退款结果报文
        /// </summary>
        public string RequestData { get; set; }

        /// <summary>
        /// 退款结果
        /// </summary>
        public int ExecuteResult { get; set; }

        /// <summary>
        /// 退款结果描述
        /// </summary>
        public string ResultDesc { get; set; }

        /// <summary>
        /// 通知业务系统结果
        /// </summary>
        public int NotifyStatus { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
