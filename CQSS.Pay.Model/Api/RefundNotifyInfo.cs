using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model.Api
{
    /// <summary>
    /// 退款结果通知
    /// </summary>
    public class RefundNotifyInfo
    {
        /// <summary>
        /// 订单编号
        /// </summary>
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        /// <summary>
        /// 支付交易流水号
        /// </summary>
        [JsonProperty("tradeNo")]
        public string TradeNo { get; set; }

        /// <summary>
        /// 退款单编号
        /// </summary>
        [JsonProperty("refundOrderId")]
        public string RefundOrderId { get; set; }

        /// <summary>
        /// 退款交易流水号
        /// </summary>
        [JsonProperty("refundNo")]
        public string RefundNo { get; set; }

        /// <summary>
        /// 退款金额
        /// </summary>
        [JsonProperty("refundAmt")]
        public string RefundAmt { get; set; }

        /// <summary>
        /// 退款结果
        /// </summary>
        [JsonProperty("result")]
        public string Result { get; set; }
    }
}
