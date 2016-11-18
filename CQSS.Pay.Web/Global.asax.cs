using CQSS.Pay.Util.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace CQSS.Pay.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_Error(Object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError().GetBaseException();
            if (ex != null)
            {
                StringBuilder sbError = new StringBuilder();
                sbError.Append("HttpMethod = " + Request.HttpMethod + Environment.NewLine);
                sbError.Append("URL.ToString()：" + Request.Url.ToString() + Environment.NewLine);
                sbError.Append("Url OriginalString：" + Request.Url.OriginalString + Environment.NewLine);
                sbError.Append("URL AbsoluteUri：" + Request.Url.AbsoluteUri + Environment.NewLine);
                sbError.Append("URL AbsolutePath：" + Request.Url.AbsolutePath + Environment.NewLine);
                sbError.Append("Query：" + Request.Url.Query + Environment.NewLine);
                sbError.Append("URLRefer OriginalString：" + (Request.UrlReferrer != null ? Request.UrlReferrer.OriginalString : "") + Environment.NewLine);
                sbError.Append("客户端IP：" + RequestHelper.GetRequestIP() + Environment.NewLine);
                sbError.Append("客户端名称：" + Request.UserHostName + Environment.NewLine);
                sbError.Append("浏览器代理：" + Request.UserAgent + Environment.NewLine);
                sbError.Append("异常时间：" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff") + Environment.NewLine);
                sbError.Append("异常文件：" + ex.Source + Environment.NewLine);
                sbError.Append("异常信息：" + ex.Message + Environment.NewLine);
                sbError.Append("异常堆栈：" + ex.StackTrace + Environment.NewLine);
                LogWriter.WriteLog(sbError.ToString(), ExceptionHelper.ExceptionLevel.Exception);
            }
        }
    }
}
