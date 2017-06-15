using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model.Api
{
    /// <summary>
    /// 退款订单信息
    /// </summary>
    public class RefundOrderInfo
    {
        /// <summary>
        /// 订单编号
        /// </summary>
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        /// <summary>
        /// 订单创建时间
        /// </summary>
        [JsonProperty("orderTime")]
        public string OrderTime { get; set; }

        /// <summary>
        /// 支付交易流水号
        /// </summary>
        [JsonProperty("tradeNo")]
        public string TradeNo { get; set; }

        /// <summary>
        /// 支付金额
        /// </summary>
        [JsonProperty("paymentAmt")]
        public string PaymentAmt { get; set; }

        /// <summary>
        /// 退款单编号
        /// </summary>
        [JsonProperty("refundOrderId")]
        public string RefundOrderId { get; set; }

        /// <summary>
        /// 退款金额
        /// </summary>
        [JsonProperty("refundAmt")]
        public string RefundAmt { get; set; }

        /// <summary>
        /// 退款完成的通知地址
        /// </summary>
        [JsonProperty("notifyUrl")]
        public string NotifyUrl { get; set; }
    }
}
