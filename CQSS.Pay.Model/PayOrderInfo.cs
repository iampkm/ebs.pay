using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.Model
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
        /// 跨境类型
        /// </summary>
        [JsonProperty("crossboardType")]
        public string CrossboardType { get; set; }

        /// <summary>
        /// 货款金额
        /// </summary>
        [JsonProperty("goodsPrice")]
        public string GoodsPrice { get; set; }

        /// <summary>
        /// 税费金额
        /// </summary>
        [JsonProperty("taxPrice")]
        public string TaxPrice { get; set; }

        /// <summary>
        /// 运费金额
        /// </summary>
        [JsonProperty("freightPrice")]
        public string FreightPrice { get; set; }

        /// <summary>
        /// 购买人姓名
        /// </summary>
        [JsonProperty("buyerName")]
        public string BuyerName { get; set; }

        /// <summary>
        /// 购买人手机号码
        /// </summary>
        [JsonProperty("buyerCellphone")]
        public string BuyerCellphone { get; set; }

        /// <summary>
        /// 购买人身份证号码
        /// </summary>
        [JsonProperty("buyerIdCardNo")]
        public string BuyerIdCardNo { get; set; }

        /// <summary>
        /// 支付条形码
        /// </summary>
        [JsonProperty("barcode")]
        public string Barcode { get; set; }

        /// <summary>
        /// 业务系统编号
        /// </summary>
        [JsonProperty("systemId")]
        public string SystemId { get; set; }
    }
}
