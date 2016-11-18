using CQSS.Pay.DAL;
using CQSS.Pay.Model;
using CQSS.Pay.Util.Extension;
using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL.Basic
{
    public class PayResultManager
    {
        /// <summary>
        /// 保存支付结果异步回执记录
        /// </summary>
        /// <param name="data"></param>
        /// <param name="payType"></param>
        /// <returns></returns>
        public static PayResultInfo SavePayResult(string data, AppEnum.PayType payType)
        {
            var payResult = new PayResultInfo()
            {
                PayType = (int)payType,
                RequestData = data,
                ExecuteResult = (int)ResultStatus.None,
                NotifyStatus = (int)AppEnum.GlobalStatus.Invalid,
                CreateTime = DateTime.Now,
            };
            payResult.SysNo = PayResultDAL.Insert(payResult);
            return payResult;
        }

        /// <summary>
        /// 通知业务系统支付成功
        /// </summary>
        /// <param name="payResult"></param>
        /// <param name="payRequest"></param>
        /// <returns></returns>
        public static bool NotifyBack(PayResultInfo payResult, PayRequestInfo payRequest)
        {
            //支付结果记录对象无效，则不执行
            if (payResult == null || payResult.SysNo <= 0)
                return false;

            //支付请求记录对象无效，则不执行
            if (payRequest == null || payRequest.SysNo <= 0)
                return false;

            //支付结果记录与支付请求记录不对应，则不执行
            if (payRequest.SysNo != payResult.RequestSysNo)
                return false;

            //支付结果记录未成功执行，或者已通知，则不执行
            if (payResult.ExecuteResult != (int)ResultStatus.Success || payResult.NotifyStatus != (int)AppEnum.GlobalStatus.Invalid)
                return false;

            //支付请求记录中不存在有效的通知地址，则不执行
            if (!payRequest.NotifyUrl.IsUrl())
                return false;

            var payNotify = new PayNotifyInfo()
            {
                OrderId = payResult.OrderId,
                TradeNo = payResult.TradeNo,
                PaymentAmt = payResult.PaymentAmt.ToString(),
                Result = ((int)ResultStatus.Success).ToString(),
            };
            string data = JsonHelper.Serialize(payNotify);
            string sign = SignManager.CreateSign(data).Data;
            string param = "sign=" + sign;
            param += "&data=" + data;
            var response = HttpHelper.HttpRequest("POST", payRequest.NotifyUrl, param, 10000);
            string status = JsonHelper.GetJsonNode(response, "status");
            string msg = JsonHelper.GetJsonNode(response, "msg");

            var notifyBack = new NotifyBackInfo()
            {
                ResultSysNo = payResult.SysNo,
                Status = (int)AppEnum.GlobalStatus.Valid,
                CreateTime = DateTime.Now,
                ResponseData = response,
            };
            if (status != "1")
            {
                notifyBack.Status = (int)AppEnum.GlobalStatus.Invalid;
                notifyBack.Msg = msg;
            }
            else
            {
                payResult.NotifyStatus = (int)AppEnum.GlobalStatus.Valid;
                PayResultDAL.Update(payResult);
            }
            PayResultDAL.InsertNotifyBack(notifyBack);

            bool result = notifyBack.Status == (int)AppEnum.GlobalStatus.Valid;
            return result;
        }
    }
}
