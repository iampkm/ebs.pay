using ETSClient.com.allinpay.ets.client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Allinpay.Api
{
    /// <summary>
    /// 通联支付数据
    /// </summary>
    public class AllinpayData
    {
        private Dictionary<string, string> _value = new Dictionary<string, string>();

        /// <summary>
        /// 通联支付数据
        /// </summary>
        /// <param name="type">数据类型</param>
        public AllinpayData(AllinpayDataType type)
        {
            if (type == AllinpayDataType.PayRequest)
                this.SetPayRequestParams();
            else if (type == AllinpayDataType.PayResult)
                this.SetPayResultParams();
            else if (type == AllinpayDataType.RefundRequest)
                this.SetRefundRequestParams();
            else if (type == AllinpayDataType.RefundResponse)
                this.SetRefundResponseParams();
        }

        /// <summary>
        /// 设置支付请求参数
        /// </summary>
        private void SetPayRequestParams()
        {
            //字符集 默认填1(1代表UTF-8、2代表GBK、3代表GB2312) 不可空
            this._value["inputCharset"] = "1";
            //页面跳转同步通知页面路径 不可空
            this._value["pickupUrl"] = "";
            //服务器接受支付结果的后台地址 不可空
            this._value["receiveUrl"] = "";
            //网关接收支付请求接口版本 默认填v1.0,可选值：v1.0、v2.0(注意为小写字母) 不可空
            this._value["version"] = "v1.0";
            //网关页面显示语言种类 默认填1(1代表简体中文、2代表繁体中文、3代表英文) 可空
            this._value["language"] = "1";
            //签名类型 默认填1(0表示订单上送和交易结果通知都使用MD5进行签名；1表示商户用使用MD5算法验签上送订单，通联交易结果通知使用证书签名) 不可空
            this._value["signType"] = "0";
            //商户号 数字串，商户在通联申请开户的商户号 不可空
            this._value["merchantId"] = "";
            //付款人姓名 人民币跨境商户如果使用直连模式时参数必填 可空
            this._value["payerName"] = "";
            //付款人邮件联系方式 可空
            this._value["payerEmail"] = "";
            //付款人电话联系方式 当payType为3、issuerId不为空“直连模式”时参数必填 不可空
            this._value["payerTelephone"] = "";
            //付款人类型及证件号 填写规则：证件类型+证件号码再使用通联公钥加密。（证件类型：01-身份证）人民币跨境商户如果使用直连模式时参数必填 可空
            this._value["payerIDCard"] = "";
            //合作伙伴的商户号 可空
            this._value["pid"] = "";
            //商户订单号 不可空
            this._value["orderNo"] = "";
            //商户订单金额 整型数字，单位为“分” 不可空
            this._value["orderAmount"] = "";
            //订单金额币种类型 默认填0 0和156代表人民币、840代表美元、344代表港币 不可空
            this._value["orderCurrency"] = "0";
            //商户订单提交时间 日期格式：yyyyMMddHHmmss 不可空
            this._value["orderDatetime"] = "";
            //订单过期时间 整型数字，单位为“分钟”，最大值为9999 可空
            this._value["orderExpireDatetime"] = "";
            //商品名称 可空
            this._value["productName"] = "";
            //商品价格 可空
            this._value["productPrice"] = "";
            //商品数量 整型数字，默认传1 可空
            this._value["productNum"] = "1";
            //商品代码 可空
            this._value["productId"] = "";
            //商品描述 可空
            this._value["productDesc"] = "";
            //扩展字段1 英文或中文字符串，支付完成后，按照原样返回给商户 可空
            this._value["ext1"] = "";
            //扩展字段2 接海关报关商户上送customsExt字段的MD5签名信息 可空
            this._value["ext2"] = "";
            //业务扩展字段 可空
            this._value["extTL"] = "";
            //支付方式 非直连模式设为0，直连模式设为非0的固定选择值（0代表未指定支付方式 1代表个人网银支付 4代表企业网银支付 10代表wap支付 11代表信用卡支付 12代表快捷付 21代表认证支付 23代表外卡支付） 不可空
            this._value["payType"] = "0";
            //发卡方代码 payType为0时，issuerId必须为空 可空
            this._value["issuerId"] = "";
            //付款人支付卡号 目前交行B2B直连模式才必填 可空
            this._value["pan"] = "";
            //贸易类型 可选值：GOODS、SERVICES 可空
            this._value["tradeNature"] = "";
            //加密密钥 不可空
            this._value["key"] = "";
            //签名字符串 不可空
            this._value["signMsg"] = "";
        }

        private void SetPayResultParams()
        {
            //商户号 与提交订单时的商户号保持一致 不可空
            this._value["merchantId"] = null;
            //网关返回支付结果接口版本 可选值：v1.0、v2.0 不可空
            this._value["version"] = null;
            //网页显示语言种类 固定值：1；1代表中文显示 可空
            this._value["language"] = null;
            //签名类型 固定选择值：0、1，与提交订单时的签名类型保持一致 不可空
            this._value["signType"] = null;
            //支付方式 字符串，返回用户在实际支付时所使用的支付方式 可空
            this._value["payType"] = null;
            //发卡方机构代码 字符串，返回用户在实际支付时所使用的发卡方机构代码 可空
            this._value["issuerId"] = null;
            //通联订单号 不可空
            this._value["paymentOrderId"] = null;
            //商户订单号 与提交订单时的商户订单号保持一致 不可空
            this._value["orderNo"] = null;
            //商户订单提交时间 与提交订单时的商户订单提交时间保持一致 不可空
            this._value["orderDatetime"] = null;
            //商户订单金额 整型数字 单位是分 不可空
            this._value["orderAmount"] = null;
            //支付完成时间 日期格式：yyyyMMddHHmmss，例如：20121116020101
            this._value["payDatetime"] = null;
            //订单实际支付金额 整型数字，实际支付金额，用户实际支付币种为人民币 不可空
            this._value["payAmount"] = null;
            //扩展字段1 与提交订单时的扩展字段1保持一致 可空
            this._value["ext1"] = null;
            //扩展字段2 与提交订单时的扩展字段2保持一致 可空
            this._value["ext2"] = null;
            //处理结果 1：支付成功 0：未付款 仅在支付成功时通知商户 不可空
            this._value["payResult"] = null;
            //错误代码 失败时返回的错误代码 可空
            this._value["errorCode"] = null;
            //结果返回时间 系统返回支付结果的时间，日期格式：yyyyMMddHHmmss 不可空
            this._value["returnDatetime"] = null;
            //加密密钥 与提交订单时的商户订单提交时间保持一致 可空
            this._value["key"] = null;
            //签名字符串 以上所有非空参数按上述顺序与密钥key组合，经加密后生成该值 不可空
            this._value["signMsg"] = null;
        }

        /// <summary>
        /// 设置退款请求参数
        /// </summary>
        private void SetRefundRequestParams()
        {
            //网关联机退款接口版本 固定值：v1.3 不可空
            this._value["version"] = "v1.3";
            //签名类型 固定值：0，0表示用md5签名 不可空
            this._value["signType"] = "0";
            //商户号 数字串，与提交订单时的商户号保持一致 不可空
            this._value["merchantId"] = "";
            //商户订单号 与提交订单时的商户订单号保持一致 不可空
            this._value["orderNo"] = "";
            //退款金额 整型数字，单位为“分” 不可空
            this._value["refundAmount"] = "";
            //商户订单提交时间 与提交订单时的商户订单提交时间保持一致 不可空
            this._value["orderDatetime"] = "";
            //签名字符串 不可空
            this._value["signMsg"] = "";
        }

        /// <summary>
        /// 设置退款响应参数
        /// </summary>
        private void SetRefundResponseParams()
        {
            //商户号 与提交订单时的商户号保持一致 不可空
            this._value["merchantId"] = "";
            //网关联机退款接口版本 固定值：v1.3 不可空
            this._value["version"] = "v1.3";
            //签名类型 固定选择值：0、1；与客户提交订单填写的值保持一致 不可空
            this._value["signType"] = "";
            //商户订单号 与提交订单时的商户订单号保持一致 不可空
            this._value["orderNo"] = "";
            //商户订单金额 整型数字，以十分之一厘为单位，即10元返回时金额应为100000 不可空
            this._value["orderAmount"] = "";
            //商户订单提交时间 与提交订单时的商户订单提交时间保持一致 不可空
            this._value["orderDatetime"] = "";
            //退款金额 整型数字，以十分之一厘为单位，即10元返回时金额应为100000 不可空
            this._value["refundAmount"] = "";
            //退款受理时间 数字串、退款申请受理的时间日期格式yyyyMMddHHmmss 不可空
            this._value["refundDatetime"] = "";
            //退款结果 成功：20  其他为失败 不可空
            this._value["refundResult"] = "";
            //错误编码 失败时返回的错误代码 可空
            this._value["errorCode"] = "";
            //结果返回时间 数字串、退款申请完成的时间日期格式yyyyMMddHHmmss 不可空
            this._value["returnDatetime"] = "";
            //签名字符串 不可空
            this._value["signMsg"] = "";
        }

        public void SetCustomsExt(string eshopEntCode, string eshopEntName, int goodsPrice, int taxPrice)
        {
            var str = new StringBuilder();
            str.Append("<GW_CUSTOMS>");
            str.AppendFormat("<CUSTOMS_TYPE>{0}</CUSTOMS_TYPE>", "HG001");//海关类别（通联分配的海关类别代码HG001）
            str.AppendFormat("<BIZ_TYPE_CODE>{0}</BIZ_TYPE_CODE>", "I20");//业务类型（直购进口：I10,网购保税进口：I20）
            str.AppendFormat("<ESHOP_ENT_CODE>{0}</ESHOP_ENT_CODE>", eshopEntCode);//电商企业代码（10位海关代码）
            str.AppendFormat("<ESHOP_ENT_NAME>{0}</ESHOP_ENT_NAME>", eshopEntName);//电商企业名称（企业备案的企业全称）
            str.AppendFormat("<GOODS_FEE>{0}</GOODS_FEE>", goodsPrice);//商品货款金额（单位：分）
            str.AppendFormat("<TAX_FEE>{0}</TAX_FEE>", taxPrice);//税款金额（单位：分）
            str.Append("</GW_CUSTOMS>");
            string customsExt = str.ToString();
            this._value["ext2"] = SecurityUtil.MD5Encode(customsExt);
            this._value["customsExt"] = customsExt;
        }

        /// <summary>
        /// 设置某个键的值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetValue(string key, string value)
        {
            this._value[key] = value;
        }

        /// <summary>
        /// 获取某个键的值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetValue(string key)
        {
            string o = null;
            this._value.TryGetValue(key, out o);
            return o;
        }

        /// <summary>
        /// 判断某个键是否有值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool HasValue(string key)
        {
            string o = null;
            this._value.TryGetValue(key, out o);
            if (null != o)
                return true;
            else
                return false;
        }

        /// <summary>
        /// 构造form表单
        /// </summary>
        /// <param name="serverUrl">服务器地址</param>
        /// <param name="method">提交方式 可选值：post、get</param>
        /// <returns></returns>
        public string ToForm(string serverUrl, string method)
        {
            StringBuilder sbHtml = new StringBuilder();

            sbHtml.Append("<form id='allinpaysubmit' name='allinpaysubmit' action='" + serverUrl + "' method='" + method.ToLower().Trim() + "'>");

            foreach (var item in this._value)
            {
                sbHtml.Append("<input type='hidden' name='" + item.Key + "' value='" + item.Value + "'/>");
            }

            //submit按钮控件请不要含有name属性
            sbHtml.Append("<input type='submit' value='提交' style='display:none;'></form>");

            sbHtml.Append("<script>document.forms['allinpaysubmit'].submit();</script>");

            return sbHtml.ToString();
        }

        /// <summary>
        /// 转化成url参数格式
        /// </summary>
        /// <returns></returns>
        public string ToUrl()
        {
            string buff = "";
            foreach (var item in this._value)
            {
                buff += item.Key + "=" + item.Value + "&";
            }
            buff = buff.Trim('&');
            return buff;
        }

        /// <summary>
        /// url参数格式转化成键值对
        /// </summary>
        /// <param name="url"></param>
        public void FromUrl(string url)
        {
            string[] param = url.Trim('&').Split('&');
            foreach (var item in param)
            {
                string[] data = item.Split('=');
                this.SetValue(data[0], data[1]);
            }
        }

    }

    /// <summary>
    /// 通联操作数据类型
    /// </summary>
    public enum AllinpayDataType
    {
        /// <summary>
        /// 支付请求
        /// </summary>
        PayRequest,
        /// <summary>
        /// 退款请求
        /// </summary>
        RefundRequest,
        /// <summary>
        /// 支付结果
        /// </summary>
        PayResult,
        /// <summary>
        /// 退款响应
        /// </summary>
        RefundResponse,
    }
}
