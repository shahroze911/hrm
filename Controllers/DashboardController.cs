using System;
using System.Linq;
using System.Web.Mvc;
using Cities_States.Models; // Import your model namespace

namespace Cities_States.Controllers
{
    public class DashboardController : Controller
    {
        private hrm_dbEntities4 db = new hrm_dbEntities4();

        public ActionResult Index()
        {
            int employeeCount = db.Employees.Count();
            ViewBag.EmployeeCount = employeeCount;

            int clientCount = db.Clients.Count();
            ViewBag.ClientCount = clientCount;

            int pendingUANCount = db.Employees.Count(e => string.IsNullOrEmpty(e.UAN));
            ViewBag.PendingUANCount = pendingUANCount;

            int pendingESICount = db.Employees.Count(e => string.IsNullOrEmpty(e.ESINumber));
            ViewBag.PendingESI = pendingESICount;

            ViewBag.CLRALicenses = db.BranchOffices.Count(e => !string.IsNullOrEmpty(e.CLRALicense));

            var today = DateTime.Today;
            var endDate = today.AddDays(90);

            // Count licenses with less than 90 days of validity remaining
            var upcomingLicensesCount = db.BranchOffices
                .Where(l => l.CLRALicenseExpiry > today && l.CLRALicenseExpiry <= endDate)
                .Count();

            ViewBag.UpcomingLicensesCount = upcomingLicensesCount;

            string logFilePath = Server.MapPath("~/App_Data/employee_log.txt");

            string logContent = System.IO.File.ReadAllText(logFilePath);

            logContent = logContent.Replace(Environment.NewLine, "<br/>");

            ViewBag.LogContent = logContent;

            int maleCount = db.Employees.Count(e => e.Gender == "Male");
            int femaleCount = db.Employees.Count(e => e.Gender == "Female");
            int marriedCount = db.Employees.Count(e => e.MaritalStatus == "Married");
            int singleCount = db.Employees.Count(e => e.MaritalStatus == "Single");
            int divorcedCount = db.Employees.Count(e => e.MaritalStatus == "Divorced");
            int widowCount = db.Employees.Count(e => e.MaritalStatus == "Widowed");
            ViewBag.MaleCount = maleCount;
            ViewBag.FemaleCount = femaleCount;
            ViewBag.MarriedCount = marriedCount;
            ViewBag.SingleCount = singleCount;
            ViewBag.divorcedCount = divorcedCount;
            ViewBag.widowCount = widowCount;
            return View();
        }


        public ActionResult PendingUAN()
        {
            var employeesWithPendingUAN = db.Employees.Where(e => string.IsNullOrEmpty(e.UAN)).ToList();
            return PartialView("PendingUANPopup",employeesWithPendingUAN);
        }

        public ActionResult PendingESI()
        {
            var employeesWithPendingESI = db.Employees.Where(e => string.IsNullOrEmpty(e.ESINumber)).ToList();
            return PartialView("PendingESIPopup", employeesWithPendingESI);
        }
        //public JsonResult GetEmployeeBirthdays()
        //{
        //    var birthdays = db.Employees
        //        .Select(e => new
        //        {
        //            title = e.FullName,
        //            start = e.DateOfBirth.ToString("yyyy-MM-dd")
        //        }).ToList();

        //    return Json(birthdays, JsonRequestBehavior.AllowGet);
        //}


    }
}
