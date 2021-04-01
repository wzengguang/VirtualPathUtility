using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using WebApplicationCoreTest.Models;

namespace WebApplicationCoreTest.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// This is test path.
        /// Our target is testing in core HttpRuntime.AppDomainAppVirtualPath is same to in framework GetAppDomainString(".appVPath")
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            // physic path.
            var contentRootPath = HttpContext
                    .RequestServices
                    .GetRequiredService<IWebHostEnvironment>()
                    .ContentRootPath;


            var appDomainAppVirtualPath = AppDomainAppVirtualPath;

            //default appDomainAppVirtualPath is "/"
            Assert.AreEqual(appDomainAppVirtualPath, "/");

            //now change it.
            HttpContext.Request.PathBase = "/eee";

            Assert.AreEqual(HttpContext.Request.PathBase.HasValue, true);

            Assert.AreEqual(HttpContext.Request.PathBase.Value, "/eee");

            var hasValue = HttpContext.Request.PathBase.HasValue;

            //test VirtualPath.CreateNonRelativeTrailingSlash
            VirtualPath vPath = VirtualPath.CreateNonRelativeTrailingSlash(HttpContext.Request.PathBase.Value);
            Assert.AreEqual(vPath.VirtualPathString, "/eee/");
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        /// <summary>
        /// this is HttpRuntime.AppDomainAppVirtualPath
        /// we know:  HttpContextProvider.Instance.GetContext()  is  HttpContext 
        /// </summary>
        public string AppDomainAppVirtualPath
        {
            get
            {
                if (HttpContext.Request.PathBase.HasValue)
                {
                    return HttpContext.Request.PathBase.Value;
                }
                else
                {
                    // This is when virtual path is the site root
                    return "/";
                }
            }
        }


        //public static string AppDomainAppVirtualPath
        //{
        //    get
        //    {
        //        if (HttpContextProvider.Instance.GetContext().Request.PathBase.HasValue)
        //        {
        //            return HttpContextProvider.Instance.GetContext().Request.PathBase.Value;
        //        }
        //        else
        //        {
        //            // This is when virtual path is the site root
        //            return "/";
        //        }
        //    }
        //}
    }
}
