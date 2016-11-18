using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model
{
    /// <summary>
    /// 支付结果通知
    /// </summary>
    public class PayNotifyInfo
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
        /// 支付金额
        /// </summary>
        [JsonProperty("paymentAmt")]
        public string PaymentAmt { get; set; }

        /// <summary>
        /// 支付结果
        /// </summary>
        [JsonProperty("result")]
        public string Result { get; set; }
    }
}
