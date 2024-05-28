using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Cities_States.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Error
        public ActionResult Index()
        {
            return View("Error");
        }

        public ActionResult Error404()
        {
            return View("Error404");
        }
        public ActionResult Error400()
        {
            return View("Error400");
        }
    }
}