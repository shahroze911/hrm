using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Cities_States.Controllers
{
    public class HandleBadRequestAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.Exception is HttpException httpException && httpException.GetHttpCode() == 400)
            {
                // Redirect or display a custom error page for bad request
                filterContext.Result = new RedirectResult("~/Error/Error400");
                filterContext.ExceptionHandled = true;
            }
            else
            {
                base.OnException(filterContext);
            }
        }
    }
}