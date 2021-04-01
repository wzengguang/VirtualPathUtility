using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace WebApplicationTest.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            // this is a physic path of this app.
            var appPath = GetAppDomainString(".appPath");

            //
            var appVPath = GetAppDomainString(".appVPath");

            Assert.AreEqual(appVPath, "/");

            // change appVPath
            SetAppDomainString(".appVPath", "/eee");

            appVPath = GetAppDomainString(".appVPath");

            //When set this value, just set, change nothing.
            Assert.AreEqual(appVPath, "/eee");


            var nonRelativeTrailingSlash = VirtualPath.CreateNonRelativeTrailingSlash(appVPath);

            Assert.AreEqual(nonRelativeTrailingSlash.VirtualPathString, "/eee/");

            return View();
        }

        public ActionResult About()
        {


            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        private static String GetAppDomainString(String key)
        {
            Object x = Thread.GetDomain().GetData(key);

            return x as String;
        }

        private static void SetAppDomainString(String key, object value)
        {
            Thread.GetDomain().SetData(key, value);

        }
    }
}