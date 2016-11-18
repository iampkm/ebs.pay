using System;
using System.Collections.Generic;
using System.Web;

namespace CQSS.Pay.BLL.WeChatPay.Api
{
    public class WxPayException : Exception
    {
        public WxPayException(string msg)
            : base(msg)
        {

        }
    }
}