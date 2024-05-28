using Cities_States.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Cities_States.Controllers
{
    public class HomeController : Controller
    {
        private hrm_dbEntities1 db = new hrm_dbEntities1();

        public ActionResult Index()
        {
            // Fetch states from the database
            List<state> states = db.states.ToList();

            // Pass states to the view
            ViewBag.States = new SelectList(states, "StateID", "StateName");

            return View();  
        }

        public JsonResult GetDistrictsByState(int stateId)
        {
            var districts = db.Districts.Where(d => d.StateID == stateId)
                                        .Select(d => new { DistrictID = d.DistrictID, DistrictName = d.DistrictName })
                                        .ToList();
            return Json(districts, JsonRequestBehavior.AllowGet);
        }
    }
}