using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model.Data
{
    /// <summary>
    /// 支付请求信息
    /// </summary>
    public class PayRequestInfo
    {
        /// <summary>
        /// 系统自增主键
        /// </summary>
        public int SysNo { get; set; }

        /// <summary>
        /// 订单编号
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// 支付金额
        /// </summary>
        public decimal PaymentAmt { get; set; }

        /// <summary>
        /// 支付方式
        /// </summary>
        public int PayType { get; set; }

        /// <summary>
        /// 支付完成的通知地址
        /// </summary>
        public string NotifyUrl { get; set; }

        /// <summary>
        /// 支付完成的返回页面
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        /// 支付请求报文
        /// </summary>
        public string RequestData { get; set; }

        /// <summary>
        /// 执行结果
        /// </summary>
        public int ExecuteResult { get; set; }

        /// <summary>
        /// 执行结果描述
        /// </summary>
        public string ResultDesc { get; set; }

        /// <summary>
        /// 业务系统ID
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// 有效状态
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
