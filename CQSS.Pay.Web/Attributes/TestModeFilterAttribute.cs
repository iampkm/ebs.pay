using CQSS.Pay.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CQSS.Pay.Web
{
    /// <summary>
    /// 测试模式过滤器
    /// </summary>
    public class TestModeFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var content = new ContentResult();
            if (!AppConfig.Global.IsTestMode)
            {
                content.Content = "未开启测试模式，测试功能不可用，请联系开发人员";
                filterContext.Result = content;
            }
        }
    }
}