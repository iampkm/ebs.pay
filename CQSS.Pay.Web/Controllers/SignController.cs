using CQSS.Pay.BLL.Basic;
using CQSS.Pay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CQSS.Pay.Web.Controllers
{
    /// <summary>
    /// 签名管理控制器
    /// </summary>
    public class SignController : Controller
    {
        /// <summary>
        /// 校验签名是否正确
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="sign"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public JsonResult Check(string appId, string sign, string data)
        {
            try
            {
                var result = SignManager.CheckSign(appId, sign, data);
                if (result.Status == ResultStatus.Success)
                    return Json(new { status = 1, data = result.Data });
                else
                    return Json(new { status = 0, msg = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { status = -1, msg = ex.Message });
            }
        }
    }
}