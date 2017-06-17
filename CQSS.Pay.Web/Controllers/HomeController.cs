using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CQSS.Pay.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
           /// return Redirect("http://www.sjgo365.com/");
            return Redirect("/doc");
           // return View();
        }
    }
}