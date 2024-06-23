using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Cities_States.Models;
using Microsoft.AspNet.SignalR;
using OfficeOpenXml;

namespace Cities_States.Controllers
{
    public class EmployeesController : Controller
    {
        private hrm_dbEntities4 db = new hrm_dbEntities4();
        private IHubContext _hubContext;
        public EmployeesController()
        {
            _hubContext = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
        }


        // GET: Employees
        public ActionResult Index()
        {
            var employees = db.Employees.Include(e => e.Client);
            List<state> states = db.states.ToList();

            // Pass states to the view
            ViewBag.States = new SelectList(states, "StateID", "StateName");
            ViewBag.Clients = new SelectList(db.Clients, "ClientID", "ClientName");
            ViewBag.BranchCodes = new SelectList(db.BranchOffices, "BranchCode", "BranchCode");
            employees = db.Employees.Include(e => e.Client);

            return View(employees.ToList());
        }

        // GET: Employees/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }
        [HttpGet]
        public ActionResult GetBranchCodes(int clientId)
        {
            var branchCodes = db.BranchOffices
                                .Where(b => b.ClientID == clientId)
                                .Select(b => new SelectListItem
                                {
                                    Value = b.BranchCode,
                                    Text = b.BranchCode
                                }).ToList();

            return Json(branchCodes, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Create()
        {
            ViewBag.ClientID = new SelectList(db.Clients, "ClientID", "ClientName");
            List<state> states = db.states.ToList();

            // Pass states to the view
            ViewBag.States = new SelectList(states, "StateID", "StateName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "FullName,FathersName,Designation,DateOfBirth,Gender,PAN,Aadhar,MobileNumber,EmergencyContactNumber,Email,DateOfJoining,UAN,PFNumber,SalaryBelow21K,ESINumber,BankAccountNumber,IFSC,ClientID,BranchCode,DateOfDeployment,MaritalStatus,Address")] Employee employee, int State, int District, HttpPostedFileBase Photograph, string GenerateLetter)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (Photograph != null && Photograph.ContentLength > 0)
                    {
                        try
                        {
                            // Generate a unique file name
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Photograph.FileName);
                            // Define the path to save the file
                            var filePath = Path.Combine(Server.MapPath("~/Images/"), fileName);
                            // Save the file to the server
                            Photograph.SaveAs(filePath);

                            // Set the image path in the employee object
                            employee.Photograph = "/Images/" + fileName;
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", "Error saving image: " + ex.Message);
                            ViewBag.ClientID = new SelectList(db.Clients, "ClientID", "ClientName", employee.ClientID);
                            ViewBag.States = new SelectList(db.states, "StateID", "StateName");
                            return View(employee);
                        }
                    }

                    // Fetch selected State and District names
                    var stateName = db.states.Where(s => s.StateID == State).Select(s => s.StateName).FirstOrDefault();
                    var districtName = db.Districts.Where(d => d.DistrictID == District).Select(d => d.DistrictName).FirstOrDefault();
                    
                    // Concatenate State and District names and save in Address field
                    employee.Address = $"{employee.Address} , {stateName} , {districtName}";

                    // Add the employee object to the database context
                    db.Employees.Add(employee);
                    db.SaveChanges();

                    LogEmployeeAddition(employee.FullName);

                    // Notify clients using SignalR
                    _hubContext.Clients.All.newEmployeeAdded(employee.FullName);


                    // Generate appointment letter if the user confirmed
                    if (GenerateLetter == "true")
                    {
                        GenerateAppointmentLetter(employee);


                    }

                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.ClientID = new SelectList(db.Clients, "ClientID", "ClientName", employee.ClientID);
                    ViewBag.States = new SelectList(db.states, "StateID", "StateName");
                    return View(employee);
                }
            }
            catch (Exception e)
            {
                ViewBag.CreateError = "An error occurred while adding the employee. Please try again later.";
                return View(employee);
            }


            // If model state is not valid, repopulate ViewBag and return the view with errors

        }

        private void LogEmployeeAddition(string employeeName)
        {
            // Log the addition of the new employee to a file or database
            string logMessage = $"New employee added: {employeeName} at {DateTime.Now}";
            string logFilePath = Server.MapPath("~/App_Data/employee_log.txt");

            try
            {
                // Write log message to a file
                using (StreamWriter sw = System.IO.File.AppendText(logFilePath))
                {
                    sw.WriteLine(logMessage);
                }
            }
            catch (Exception ex)
            {
                // Handle any errors while logging (e.g., log to another location or display error)
                // For demonstration purposes, we're just logging the error to the console
                Console.WriteLine("Error logging employee addition: " + ex.Message);
            }
        }

        // Controller action to handle displaying the log
        public ActionResult DisplayLog()
        {
            string logFilePath = Server.MapPath("~/App_Data/employee_log.txt");

            // Read the contents of the log file
            string logContent = System.IO.File.ReadAllText(logFilePath);

            // Replace new line characters with HTML line breaks
            logContent = logContent.Replace(Environment.NewLine, "<br/>");

            // Pass the formatted log content to the view
            ViewBag.LogContent = logContent;

            return View();
        }


        public ActionResult GenerateAppointmentLetter(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    return HttpNotFound();
                }

                AppointmentLetterGenerator generator = new AppointmentLetterGenerator();
                string outputPath = generator.GenerateAppointmentLetter(employee);

                if (outputPath != null)
                {
                    string fileName = "AppointmentLetter_" + employee.FullName + ".docx";
                    generator.ServeGeneratedLetter(outputPath, fileName);
                }
                else
                {
                    // Handle generation failure (optional)
                    return new HttpStatusCodeResult(500, "Error generating appointment letter");
                }

                return new EmptyResult(); // This line may never be reached because ServeGeneratedLetter ends the response
            }
            catch (Exception e)
            {
                ViewBag.ErrorMessage1 = "An error occurred while downloading  the letter. Please try again later.";
                return View(employee);
            }

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateAppointmentOrder(string selectedIds)
        {
            var ids = ParseSelectedIds(selectedIds);
            if (ids == null || !ids.Any())
            {
                // Handle invalid or empty IDs scenario
                TempData["ErrorMessage"] = "Invalid or no employees selected.";
                return RedirectToAction("Index");
            }

            var selectedEmployees = db.Employees.Where(e => ids.Contains(e.EmployeeID)).ToList();

            foreach (var employee in selectedEmployees)
            {
                GenerateAppointmentLetter(employee);

            }

            TempData["SuccessMessage"] = "Appointment letters generated successfully.";
            return RedirectToAction("Index");
        }

        public ActionResult GenerateNominationLetter(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    return HttpNotFound();
                }

                NominationLetterGenerator generator = new NominationLetterGenerator();
                string outputPath = generator.GenerateNominationLetter(employee);

                if (outputPath != null)
                {
                    string fileName = "NominationLetter_" + employee.FullName + ".docx";
                    generator.ServeGeneratedLetter(outputPath, fileName);
                }
                else
                {
                    // Handle generation failure (optional)
                    return new HttpStatusCodeResult(500, "Error generating appointment letter");
                }

                return new EmptyResult(); // This line may never be reached because ServeGeneratedLetter ends the response
            }
            catch (Exception e)
            {
                ViewBag.ErrorMessage1 = "An error occurred while downloading  the letter. Please try again later.";
                return View(employee);
            }


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateNominationOrder(string selectedIds)
        {
            var ids = ParseSelectedIds(selectedIds);
            if (ids == null || !ids.Any())
            {
                // Handle invalid or empty IDs scenario
                TempData["ErrorMessage"] = "Invalid or no employees selected.";
                return RedirectToAction("Index");
            }

            var selectedEmployees = db.Employees.Where(e => ids.Contains(e.EmployeeID)).ToList();

            foreach (var employee in selectedEmployees)
            {
                GenerateNominationLetter(employee);


            }

            TempData["SuccessMessage"] = "Nomination letters generated successfully.";
            return RedirectToAction("Index");
        }

        public ActionResult GenerateNominationLetter1(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    return HttpNotFound();
                }

                NominationLetterGenerator1 generator = new NominationLetterGenerator1();
                string outputPath = generator.GenerateNominationLetter(employee);

                if (outputPath != null)
                {
                    string fileName = "NominationLetterEPF_" + employee.FullName + ".docx";
                    generator.ServeGeneratedLetter(outputPath, fileName);
                }
                else
                {
                    // Handle generation failure (optional)
                    return new HttpStatusCodeResult(500, "Error generating appointment letter");
                }

                return new EmptyResult(); // This line may never be reached because ServeGeneratedLetter ends the response
            }
            catch (Exception e)
            {
                ViewBag.ErrorMessage1 = "An error occurred while downloading  the letter. Please try again later.";
                return View(employee);
            }


        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateNominationOrder1(string selectedIds)
        {
            var ids = ParseSelectedIds(selectedIds);
            if (ids == null || !ids.Any())
            {
                // Handle invalid or empty IDs scenario
                TempData["ErrorMessage"] = "Invalid or no employees selected.";
                return RedirectToAction("Index");
            }

            var selectedEmployees = db.Employees.Where(e => ids.Contains(e.EmployeeID)).ToList();

            foreach (var employee in selectedEmployees)
            {
                GenerateNominationLetter1(employee);


            }

            TempData["SuccessMessage"] = "Nomination letters generated successfully.";
            return RedirectToAction("Index");
        }
        public ActionResult GenerateNominationLetter2(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    return HttpNotFound();
                }

                NominationLetterGenerator2 generator = new NominationLetterGenerator2();
                string outputPath = generator.GenerateNominationLetter(employee);

                if (outputPath != null)
                {
                    string fileName = "NominationLetterCEPGAFF_" + employee.FullName + ".docx";
                    generator.ServeGeneratedLetter(outputPath, fileName);
                }
                else
                {
                    // Handle generation failure (optional)
                    return new HttpStatusCodeResult(500, "Error generating appointment letter");
                }

                return new EmptyResult(); // This line may never be reached because ServeGeneratedLetter ends the response
            }
            catch (Exception e)
            {
                ViewBag.ErrorMessage1 = "An error occurred while downloading  the letter. Please try again later.";
                return View(employee);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateNominationOrder2(string selectedIds)
        {
            var ids = ParseSelectedIds(selectedIds);
            if (ids == null || !ids.Any())
            {
                // Handle invalid or empty IDs scenario
                TempData["ErrorMessage"] = "Invalid or no employees selected.";
                return RedirectToAction("Index");
            }

            var selectedEmployees = db.Employees.Where(e => ids.Contains(e.EmployeeID)).ToList();

            foreach (var employee in selectedEmployees)
            {
                GenerateNominationLetter2(employee);


            }

            TempData["SuccessMessage"] = "Nomination letters generated successfully.";
            return RedirectToAction("Index");
        }

        public ActionResult GenerateNominationLetter3(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    return HttpNotFound();
                }

                NominationLetterGenerator3 generator = new NominationLetterGenerator3();
                string outputPath = generator.GenerateNominationLetter(employee);

                if (outputPath != null)
                {

                    string fileName = "NominationLetter_PF_FORM_" + employee.FullName + ".docx";
                    generator.ServeGeneratedLetter(outputPath, fileName);
                }
                else
                {
                    // Handle generation failure (optional)
                    return new HttpStatusCodeResult(500, "Error generating appointment letter");
                }

                return new EmptyResult(); // This line may never be reached because ServeGeneratedLetter ends the response
            }
            catch (Exception e)
            {
                ViewBag.ErrorMessage1 = "An error occurred while downloading  the letter. Please try again later.";
                return View(employee);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateNominationOrder3(string selectedIds)
        {
            var ids = ParseSelectedIds(selectedIds);
            if (ids == null || !ids.Any())
            {
                // Handle invalid or empty IDs scenario
                TempData["ErrorMessage"] = "Invalid or no employees selected.";
                return RedirectToAction("Index");
            }

            var selectedEmployees = db.Employees.Where(e => ids.Contains(e.EmployeeID)).ToList();

            foreach (var employee in selectedEmployees)
            {
                GenerateNominationLetter3(employee);


            }

            TempData["SuccessMessage"] = "Nomination letters generated successfully.";
            return RedirectToAction("Index");
        }

        public ActionResult GenerateNominationLetter4(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    return HttpNotFound();
                }

                NominationLetterGenerator4 generator = new NominationLetterGenerator4();
                string outputPath = generator.GenerateNominationLetter(employee);

                if (outputPath != null)
                {

                    string fileName = "NominationLetter_ESIC2_" + employee.FullName + ".docx";
                    generator.ServeGeneratedLetter(outputPath, fileName);
                }
                else
                {
                    // Handle generation failure (optional)
                    return new HttpStatusCodeResult(500, "Error generating appointment letter");
                }

                return new EmptyResult(); // This line may never be reached because ServeGeneratedLetter ends the response
            }
            catch (Exception e)
            {
                ViewBag.ErrorMessage1 = "An error occurred while downloading  the letter. Please try again later.";
                return View(employee);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateNominationOrder4(string selectedIds)
        {
            var ids = ParseSelectedIds(selectedIds);
            if (ids == null || !ids.Any())
            {
                // Handle invalid or empty IDs scenario
                TempData["ErrorMessage"] = "Invalid or no employees selected.";
                return RedirectToAction("Index");
            }

            var selectedEmployees = db.Employees.Where(e => ids.Contains(e.EmployeeID)).ToList();

            foreach (var employee in selectedEmployees)
            {
                GenerateNominationLetter4(employee);


            }

            TempData["SuccessMessage"] = "Nomination letters generated successfully.";
            return RedirectToAction("Index");
        }

        static EmployeesController()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Set EPPlus license context
        }

        [HttpPost]
        public ActionResult ExportToExcel(string selectedIds)
        {
            var ids = ParseSelectedIds(selectedIds);
            if (ids == null || !ids.Any())
            {
                // Handle invalid or empty IDs scenario
                TempData["ErrorMessage"] = "Invalid or no employees selected.";
                return RedirectToAction("Index");
            }

            var selectedEmployees = db.Employees.Where(e => ids.Contains(e.EmployeeID)).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Employees");

                worksheet.Cells[1, 1].Value = "Full Name";
                worksheet.Cells[1, 2].Value = "Father's Name";
                worksheet.Cells[1, 3].Value = "Designation";
                worksheet.Cells[1, 4].Value = "Date of Birth";
                worksheet.Cells[1, 5].Value = "Gender";
                worksheet.Cells[1, 6].Value = "PAN";
                worksheet.Cells[1, 7].Value = "Aadhar";
                worksheet.Cells[1, 8].Value = "Mobile Number";
                worksheet.Cells[1, 9].Value = "Emergency Contact Number";
                worksheet.Cells[1, 10].Value = "Email";
                worksheet.Cells[1, 11].Value = "Date of Joining";
                worksheet.Cells[1, 12].Value = "UAN";
                worksheet.Cells[1, 13].Value = "PF Number";
                worksheet.Cells[1, 14].Value = "Salary Below 21K";
                worksheet.Cells[1, 15].Value = "ESI Number";
                worksheet.Cells[1, 16].Value = "Bank Account Number";
                worksheet.Cells[1, 17].Value = "IFSC";
                worksheet.Cells[1, 18].Value = "Client ID";
                worksheet.Cells[1, 19].Value = "Branch Code";
                worksheet.Cells[1, 20].Value = "Date of Deployment";
                worksheet.Cells[1, 21].Value = "Marital Status";
                worksheet.Cells[1, 22].Value = "Address";

                for (int i = 0; i < selectedEmployees.Count; i++)
                {
                    var employee = selectedEmployees[i];
                    worksheet.Cells[i + 2, 1].Value = employee.FullName;
                    worksheet.Cells[i + 2, 2].Value = employee.FathersName;
                    worksheet.Cells[i + 2, 3].Value = employee.Designation;
                    worksheet.Cells[i + 2, 4].Value = employee.DateOfBirth.ToString("dd-MM-yyyy");
                    worksheet.Cells[i + 2, 5].Value = employee.Gender;
                    worksheet.Cells[i + 2, 6].Value = employee.PAN;
                    worksheet.Cells[i + 2, 7].Value = employee.Aadhar;
                    worksheet.Cells[i + 2, 8].Value = employee.MobileNumber;
                    worksheet.Cells[i + 2, 9].Value = employee.EmergencyContactNumber;
                    worksheet.Cells[i + 2, 10].Value = employee.Email;
                    worksheet.Cells[i + 2, 11].Value = employee.DateOfJoining.ToString("dd-MM-yyyy");
                    worksheet.Cells[i + 2, 12].Value = employee.UAN;
                    worksheet.Cells[i + 2, 13].Value = employee.PFNumber;
                    worksheet.Cells[i + 2, 14].Value = employee.SalaryBelow21K ? "Yes" : "No";
                    worksheet.Cells[i + 2, 15].Value = employee.ESINumber;
                    worksheet.Cells[i + 2, 16].Value = employee.BankAccountNumber;
                    worksheet.Cells[i + 2, 17].Value = employee.IFSC;
                    worksheet.Cells[i + 2, 18].Value = employee.ClientID;
                    worksheet.Cells[i + 2, 19].Value = employee.BranchCode;
                    worksheet.Cells[i + 2, 20].Value = employee.DateOfDeployment.ToString("dd-MM-yyyy");
                    worksheet.Cells[i + 2, 21].Value = employee.MaritalStatus;
                    worksheet.Cells[i + 2, 22].Value = employee.Address;
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"~/GeneratedLetters/SelectedEmployeeList.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }





        public JsonResult GetDistrictsByState(int stateId)
        {
            var districts = db.Districts.Where(d => d.StateID == stateId)
                                        .Select(d => new { DistrictID = d.DistrictID, DistrictName = d.DistrictName })
                                        .ToList();
            return Json(districts, JsonRequestBehavior.AllowGet);
        }
        private List<int> ParseSelectedIds(string selectedIds)
        {
            if (string.IsNullOrEmpty(selectedIds))
            {
                return null;
            }

            return selectedIds.Split(',').Select(int.Parse).ToList();
        }
        private Employee GetEmployeeById(int id)
        {
            // Fetch employee details from the database
            return db.Employees.Find(id);
        }

        // GET: Employees/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }

            ViewBag.ClientID = new SelectList(db.Clients, "ClientID", "ClientName", employee.ClientID);
            ViewBag.Clients = new SelectList(db.Clients, "ClientName", "ClientName", employee.ClientName);
            ViewBag.Locations = new SelectList(db.BranchOffices, "BranchAddress", "BranchAddress", employee.ClientLocation);

            var branchCodesList = db.BranchOffices
                          .Where(b => b.ClientID == employee.ClientID)
                          .Select(b => new SelectListItem
                          {
                              Value = b.BranchCode,
                              Text = b.BranchCode
                          }).ToList();
            ViewBag.BranchCode = new SelectList(branchCodesList, "Value", "Text", employee.BranchCode);

            return View(employee);
        }
        public JsonResult GetEmployeeBirthdays()
        {
            var birthdays = db.Employees
                .Select(e => new
                {
                    title = e.FullName,
                    start = e.DateOfBirth.ToString("yyyy-MM-dd")
                }).ToList();

            return Json(birthdays, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "EmployeeID,FullName,FathersName,Designation,DateOfBirth,Gender,PAN,Aadhar,MobileNumber,EmergencyContactNumber,Email,Address,DateOfJoining,UAN,PFNumber,SalaryBelow21K,ESINumber,BankAccountNumber,IFSC,ClientID,BranchCode,DateOfDeployment,Photograph,MaritalStatus,ClientName,ClientLocation,DateOfExit,Transferred,InactiveStatus")] Employee employee, HttpPostedFileBase Photograph)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Handle the photograph upload
                    if (Photograph != null && Photograph.ContentLength > 0)
                    {
                        try
                        {
                            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(Photograph.FileName);
                            var filePath = Path.Combine(Server.MapPath("~/Images/"), fileName);
                            Photograph.SaveAs(filePath);
                            employee.Photograph = "/Images/" + fileName;
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", "Error saving image: " + ex.Message);
                            PopulateDropdowns(employee);
                            return View(employee);
                        }
                    }
                    else
                    {
                        var existingEmployee = db.Employees.AsNoTracking().FirstOrDefault(e => e.EmployeeID == employee.EmployeeID);
                        if (existingEmployee != null)
                        {
                            employee.Photograph = existingEmployee.Photograph;
                        }
                    }

                    // Set the Transferred and InactiveStatus fields based on the dropdown selection
                    var status = Request.Form["StatusSelect"];
                    if (status == "Transferred")
                    {
                        employee.Transferred = true;
                        employee.InactiveStatus = false;
                    }
                    else if (status == "Inactive")
                    {
                        employee.Transferred = false;
                        employee.InactiveStatus = true;
                    }
                    else
                    {
                        employee.Transferred = null;
                        employee.InactiveStatus = null;
                    }

                    // Debugging output to check assigned values
                    System.Diagnostics.Debug.WriteLine("Transferred: " + employee.Transferred);
                    System.Diagnostics.Debug.WriteLine("InactiveStatus: " + employee.InactiveStatus);

                    // Update the employee in the database
                    db.Entry(employee).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {
                    PopulateDropdowns(employee);
                    return View(employee);
                }
            }
            catch (Exception e)
            {
                ViewBag.ErrorMessage = "An error occurred while updating the employee. Please try again later.";
                return View(employee);
            }
        }

        //[HttpPost]
        //public ActionResult SaveDateOfExit(int employeeId, bool inactiveStatus, DateTime dateOfExit)
        //{
        //    try
        //    {
        //        // Retrieve the employee from the database
        //        var employee = db.Employees.Find(employeeId);
        //        if (employee == null)
        //        {
        //            // Employee not found
        //            return HttpNotFound();
        //        }

        //        // Update the employee's data
        //        employee.InactiveStatus = inactiveStatus;
        //        employee.DateOfExit = dateOfExit;

        //        // Save changes to the database
        //        db.SaveChanges();

        //        // Return a success message (optional)
        //        return Json(new { success = true, message = "Date of exit saved successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error (optional)
        //        Console.WriteLine("Error saving date of exit: " + ex.Message);

        //        // Return an error message
        //        return Json(new { success = false, message = "Error saving date of exit" });
        //    }
        //}
        private void PopulateDropdowns(Employee employee)
        {
            ViewBag.ClientID = new SelectList(db.Clients, "ClientID", "ClientName", employee.ClientID);
            ViewBag.Locations = new SelectList(db.BranchOffices, "BranchAddress", "BranchAddress", employee.ClientLocation);
            var branchCodesList = db.BranchOffices
                              .Where(b => b.ClientID == employee.ClientID)
                              .Select(b => new SelectListItem
                              {
                                  Value = b.BranchCode,
                                  Text = b.BranchCode
                              }).ToList();
            ViewBag.BranchCode = new SelectList(branchCodesList, "Value", "Text", employee.BranchCode);
        }


        // GET: Employees/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Employee employee = db.Employees.Find(id);
            if (employee == null)
            {
                return HttpNotFound();
            }
            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Employee employee = db.Employees.Find(id);
            db.Employees.Remove(employee);
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


