using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CQSS.Pay.Web.Controllers
{
    public class DocController : Controller
    {
        /// <summary>
        /// 接口列表
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 签名算法说明
        /// </summary>
        /// <returns></returns>
        public ActionResult SignDescription()
        {
            return View();
        }

        /// <summary>
        /// 在线支付
        /// </summary>
        /// <returns></returns>
        public ActionResult OnlinePay()
        {
            return View();
        }

        /// <summary>
        /// 条码支付
        /// </summary>
        /// <returns></returns>
        public ActionResult BarcodePay()
        {
            return View();
        }

        /// <summary>
        /// 同步申请退款
        /// </summary>
        /// <returns></returns>
        public ActionResult SyncRefund()
        {
            return View();
        }

        /// <summary>
        /// 支付宝支付接口列表
        /// </summary>
        /// <returns></returns>
        public ActionResult Alipay()
        {
            return View();
        }

        /// <summary>
        /// 微信支付接口列表
        /// </summary>
        /// <returns></returns>
        public ActionResult WeChatPay()
        {
            return View();
        }

        /// <summary>
        /// 通联支付接口列表
        /// </summary>
        /// <returns></returns>
        public ActionResult Allinpay()
        {
            return View();
        }

        /// <summary>
        /// 威富通支付接口列表
        /// </summary>
        /// <returns></returns>
        public ActionResult SwiftPass()
        {
            return View();
        }

        /// <summary>
        /// 暂不支持
        /// </summary>
        /// <returns></returns>
        public ActionResult NotSupport(string id)
        {
            ViewBag.Title = id;
            return View();
        }
    }
}