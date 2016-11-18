﻿using System;
using System.Collections.Generic;
using System.Web;

namespace CQSS.Pay.BLL.WeChatPay.Api
{
    public class PayRefund
    {
        /***
        * 申请退款完整业务流程逻辑
        * @param transaction_id 微信订单号（优先使用）
        * @param out_trade_no 商户订单号
        * @param total_fee 订单总金额
        * @param refund_fee 退款金额
        * @return 退款结果（xml格式）
        */
        public static WxPayData Run(string transaction_id, string out_trade_no, int total_fee, int refund_fee, string out_refund_no)
        {
            WxPayLog.Info("Refund", "Refund is processing...");

            WxPayData data = new WxPayData();
            if (!string.IsNullOrEmpty(transaction_id))//微信订单号存在的条件下，则已微信订单号为准
            {
                data.SetValue("transaction_id", transaction_id);
            }
            else//微信订单号不存在，才根据商户订单号去退款
            {
                data.SetValue("out_trade_no", out_trade_no);
            }

            data.SetValue("total_fee", total_fee);//订单总金额
            data.SetValue("refund_fee", refund_fee);//退款金额
            data.SetValue("out_refund_no", out_refund_no);//商户退款单号
            data.SetValue("op_user_id", WxPayConfig.MCHID);//操作员，默认为商户号

            WxPayData result = WxPayApi.Refund(data);//提交退款申请给API，接收返回数据

            WxPayLog.Info("Refund", "Refund process complete, result : " + result.ToXml());
            return result;
        }
    }
}