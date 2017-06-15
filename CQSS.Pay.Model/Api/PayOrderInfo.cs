using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model.Api
{
    /// <summary>
    /// 支付订单信息
    /// </summary>
    public class PayOrderInfo
    {
        /// <summary>
        /// 订单编号
        /// </summary>
        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        /// <summary>
        /// 支付金额
        /// </summary>
        [JsonProperty("paymentAmt")]
        public string PaymentAmt { get; set; }

        /// <summary>
        /// 下单时间
        /// </summary>
        [JsonProperty("orderTime")]
        public string OrderTime { get; set; }

        /// <summary>
        /// 支付完成的通知地址
        /// </summary>
        [JsonProperty("notifyUrl")]
        public string NotifyUrl { get; set; }

        /// <summary>
        /// 支付完成的返回地址
        /// </summary>
        [JsonProperty("returnUrl")]
        public string ReturnUrl { get; set; }

        /// <summary>
        /// 支付条形码
        /// </summary>
        [JsonProperty("barcode")]
        public string Barcode { get; set; }
    }
}
