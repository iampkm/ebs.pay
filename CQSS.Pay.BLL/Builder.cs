using CQSS.Pay.BLL.Interface;
using CQSS.Pay.BLL.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQSS.Pay.BLL
{
    public class Builder
    {
        #region 支付申请
        private static IPayRequest _onlinePay;
        public static IPayRequest BuildOnlinePay()
        {
            if (_onlinePay == null)
                _onlinePay = new OnlinePay();

            return _onlinePay;
        }

        private static IPayRequest _barcodePay;
        public static IPayRequest BuildBarcodePay()
        {
            if (_barcodePay == null)
                _barcodePay = new BarcodePay();

            return _barcodePay;
        }
        #endregion

        #region 支付结果
        private static IPayResult _simulatePayResult;
        public static IPayResult BuildSimulatePayResult()
        {
            if (_simulatePayResult == null)
                _simulatePayResult = new SimulatePayResult();

            return _simulatePayResult;
        }

        private static IPayResult _alipayResult;
        public static IPayResult BuildAlipayResult()
        {
            if (_alipayResult == null)
                _alipayResult = new AlipayResult();

            return _alipayResult;
        }

        private static IPayResult _wechatPayResult;
        public static IPayResult BuildWeChatPayResult()
        {
            if (_wechatPayResult == null)
                _wechatPayResult = new WeChatPayResult();

            return _wechatPayResult;
        }

        private static IPayResult _allinpayResult;
        public static IPayResult BuildAllinpayResult()
        {
            if (_allinpayResult == null)
                _allinpayResult = new AllinpayResult();

            return _allinpayResult;
        }

        private static IPayResult _swiftPassWechatPayResult;
        public static IPayResult BuildSwiftPassWeChatPayResult()
        {
            if (_swiftPassWechatPayResult == null)
                _swiftPassWechatPayResult = new SwiftPassWeChatPayResult();

            return _swiftPassWechatPayResult;
        }
        #endregion

        #region 退款申请
        private static IRefundRequest _refundRequest;
        public static IRefundRequest BuildRefundRequest()
        {
            if (_refundRequest == null)
                _refundRequest = new RefundRequest();

            return _refundRequest;
        }
        #endregion

        #region 退款结果
        private static IRefundResult _simulateRefundResult;
        public static IRefundResult BuildSimulateRefundResult()
        {
            if (_simulateRefundResult == null)
                _simulateRefundResult = new SimulateRefundResult();

            return _simulateRefundResult;
        }

        private static IRefundResult _wechatRefundResult;
        public static IRefundResult BuildWechatRefundResult()
        {
            if (_wechatRefundResult == null)
                _wechatRefundResult = new WechatPayRefundResult();

            return _wechatRefundResult;
        }

        private static IRefundResult _allinpayRefundResult;
        public static IRefundResult BuildAllinpayRefundResult()
        {
            if (_allinpayRefundResult == null)
                _allinpayRefundResult = new AllinpayRefundResult();

            return _allinpayRefundResult;
        }
        #endregion
    }
}
