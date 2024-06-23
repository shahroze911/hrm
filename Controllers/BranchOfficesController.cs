using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using Cities_States.Models;

namespace Cities_States.Controllers
{
    public class BranchOfficesController : Controller
    {
        private hrm_dbEntities4 db = new hrm_dbEntities4();

        // GET: BranchOffices
        public ActionResult Index(int? clientId)
        {
            var clients = db.Clients.ToList();
            ViewBag.Clients = new SelectList(clients, "ClientID", "ClientName");

            var branchOffices = db.BranchOffices.Include(b => b.Client);

            if (clientId.HasValue)
            {
                branchOffices = branchOffices.Where(b => b.ClientID == clientId.Value);
            }

            return View(branchOffices.ToList());
        }

        public ActionResult FilterBranchOffices(int clientId)
        {
            var branchOffices = db.BranchOffices.Include(b => b.Client).Where(b => b.ClientID == clientId).ToList();
            return PartialView("_BranchOfficeList", branchOffices);
        }
        public ActionResult DownloadFilteredClientList(int? clientId)
        {
            var branchOffices = db.BranchOffices.Include(b => b.Client);

            if (clientId.HasValue)
            {
                branchOffices = branchOffices.Where(b => b.ClientID == clientId.Value);
            }

            var csv = new StringBuilder();
            csv.AppendLine("BranchID,ClientName,District,BranchAddress,CLRALicenseExpiry");

            foreach (var branch in branchOffices)
            {
                csv.AppendLine($"{branch.BranchID},{branch.Client.ClientName},{branch.District},{branch.BranchAddress},{branch.CLRALicenseExpiry}");
            }

            return File(new System.Text.UTF8Encoding().GetBytes(csv.ToString()), "text/csv", "FilteredClientList.csv");
        }
        public ActionResult DownloadCompleteClientList()
        {
            var clients = db.BranchOffices.ToList();
            var csv = new StringBuilder();
            csv.AppendLine("BranchID,ClientID,BranchCode,State,District,BranchAddress,CLRALicenseExpiry");

            foreach (var client in clients)
            {
                csv.AppendLine($"{client.BranchID},{client.Client.ClientName},{client.BranchCode},{client.State},{client.District},{client.BranchAddress},{client.CLRALicenseExpiry}");
            }

            return File(new System.Text.UTF8Encoding().GetBytes(csv.ToString()), "text/csv", "CompleteClientList.csv");
        }


        // GET: BranchOffices/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BranchOffice branchOffice = db.BranchOffices.Find(id);
            if (branchOffice == null)
            {
                return HttpNotFound();
            }
            return View(branchOffice);
        }

        public ActionResult Create()
        {
            ViewBag.ClientID = new SelectList(db.Clients, "ClientID", "ClientName");
            List<state> states = db.states.ToList();
            ViewBag.States = new SelectList(states, "StateID", "StateName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ClientID,BranchCode,StateID,DistrictID,BranchAddress,IsCLRALicenseApplicable,CLRALicenseExpiry")] BranchOffice branchOffice, int? State, int? District, HttpPostedFileBase CLRALicenseUpload)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check for duplicate branch code for the same client
                    if (db.BranchOffices.Any(b => b.ClientID == branchOffice.ClientID && b.BranchCode == branchOffice.BranchCode))
                    {
                        ModelState.AddModelError("BranchCode", "Branch Code already exists for this client.");
                        ViewBag.ClientID = new SelectList(db.Clients, "ClientID", "ClientName", branchOffice.ClientID);
                        ViewBag.States = new SelectList(db.states, "StateID", "StateName");
                        return View(branchOffice);
                    }

                    if (State.HasValue)
                    {
                        var stateName = db.states.Where(s => s.StateID == State.Value).Select(s => s.StateName).FirstOrDefault();
                        branchOffice.State = stateName;
                    }

                    if (District.HasValue)
                    {
                        var districtName = db.Districts.Where(d => d.DistrictID == District.Value).Select(d => d.DistrictName).FirstOrDefault();
                        branchOffice.District = districtName;
                    }

                    if (branchOffice.IsCLRALicenseApplicable)
                    {
                        if (CLRALicenseUpload != null && CLRALicenseUpload.ContentLength > 0)
                        {
                            try
                            {
                                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(CLRALicenseUpload.FileName);
                                var filePath = Path.Combine(Server.MapPath("~/Images/"), fileName);
                                CLRALicenseUpload.SaveAs(filePath);
                                branchOffice.CLRALicense = "/Images/" + fileName;
                            }
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("", "Error saving image: " + ex.Message);
                                return View(branchOffice);
                            }
                        }
                    }
                    else
                    {
                        branchOffice.CLRALicense = null;
                    }

                    db.BranchOffices.Add(branchOffice);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }

                ViewBag.ClientID = new SelectList(db.Clients, "ClientID", "ClientName", branchOffice.ClientID);
                ViewBag.States = new SelectList(db.states, "StateID", "StateName");
                return View(branchOffice);
            }
            catch
            {
                ViewBag.CreateBranchError = "An error occurred while adding the branch. Please try again later.";
                return View(branchOffice);
            }
           
        }



        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BranchOffice branchOffice = db.BranchOffices.Find(id);
            if (branchOffice == null)
            {
                return HttpNotFound();
            }

            ViewBag.Clients = new SelectList(db.Clients, "ClientID", "ClientName", branchOffice.ClientID);
            List<state> states = db.states.ToList();
            ViewBag.States = new SelectList(states, "StateName", "StateName", branchOffice.State);
            List<District> districts = db.Districts.Where(d => d.state.StateName == branchOffice.State).ToList();
            ViewBag.Districts = new SelectList(districts, "DistrictName", "DistrictName", branchOffice.District);

            return View(branchOffice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "BranchID,ClientID,BranchCode,State,District,BranchAddress,IsCLRALicenseApplicable,CLRALicenseExpiry")] BranchOffice branchOffice, HttpPostedFileBase CLRALicenseUpload)
        {
            if (ModelState.IsValid)
            {
                var existingBranchOffice = db.BranchOffices.Find(branchOffice.BranchID);
                if (existingBranchOffice == null)
                {
                    return HttpNotFound();
                }

                if (db.BranchOffices.Any(b => b.ClientID == branchOffice.ClientID && b.BranchCode == branchOffice.BranchCode && b.BranchID != branchOffice.BranchID))
                {
                    ModelState.AddModelError("BranchCode", "Branch Code already exists for this client.");
                    ViewBag.Clients = new SelectList(db.Clients, "ClientID", "ClientName", branchOffice.ClientID);
                    ViewBag.States = new SelectList(db.states, "StateName", "StateName", branchOffice.State);
                    ViewBag.Districts = new SelectList(db.Districts.Where(d => d.state.StateName == branchOffice.State), "DistrictName", "DistrictName", branchOffice.District);
                    return View(branchOffice);
                }

                if (CLRALicenseUpload != null && CLRALicenseUpload.ContentLength > 0)
                {
                    try
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(CLRALicenseUpload.FileName);
                        var filePath = Path.Combine(Server.MapPath("~/Images/"), fileName);
                        CLRALicenseUpload.SaveAs(filePath);
                        branchOffice.CLRALicense = "/Images/" + fileName;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Error saving image: " + ex.Message);
                        return View(branchOffice);
                    }
                }
                else
                {
                    branchOffice.CLRALicense = existingBranchOffice.CLRALicense;
                }

                db.Entry(existingBranchOffice).CurrentValues.SetValues(branchOffice);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Clients = new SelectList(db.Clients, "ClientID", "ClientName", branchOffice.ClientID);
            ViewBag.States = new SelectList(db.states, "StateName", "StateName", branchOffice.State);
            ViewBag.Districts = new SelectList(db.Districts.Where(d => d.state.StateName == branchOffice.State), "DistrictName", "DistrictName", branchOffice.District);
            return View(branchOffice);
        }




        public JsonResult GetDistrictsByState(int stateId)
        {
            var districts = db.Districts.Where(d => d.StateID == stateId)
                                        .Select(d => new { DistrictID = d.DistrictID, DistrictName = d.DistrictName })
                                        .ToList();
            return Json(districts, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BranchOffice branchOffice = db.BranchOffices.Find(id);
            if (branchOffice == null)
            {
                return HttpNotFound();
            }
            return View(branchOffice);
        }

        // POST: BranchOffices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            BranchOffice branchOffice = db.BranchOffices.Find(id);
            db.BranchOffices.Remove(branchOffice);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
