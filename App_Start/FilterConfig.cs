using Cities_States.Controllers;
using System.Web;
using System.Web.Mvc;

namespace Cities_States
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleBadRequestAttribute()); // Add HandleBadRequestAttribute globally

            filters.Add(new HandleErrorAttribute());
        }
    }
}
