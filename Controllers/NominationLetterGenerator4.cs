﻿using Cities_States.Models;
using Serilog;
using System;
using System.IO;
using System.Web;
using Xceed.Words.NET;

public class NominationLetterGenerator4
{
    public string GenerateNominationLetter(Employee employee)
    {
        // Define the path to the template and the output file
        string templatePath = HttpContext.Current.Server.MapPath("~/Documents/ESIC2_FINAL.docx");
        string outputDirectory = HttpContext.Current.Server.MapPath("~/GeneratedLetters/");
        string outputPath = Path.Combine(outputDirectory, "NominationLetter_ESIC2_" + employee.FullName + ".docx");

        try
        {
            // Ensure the output directory exists
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Load the template
            using (DocX document = DocX.Load(templatePath))
            {
                
                // Replace placeholders with actual values
                document.ReplaceText("[Candidate Name]", employee.FullName);
                document.ReplaceText("[FatherName]", employee.FathersName);
                document.ReplaceText("[Date of Birth]", employee.DateOfBirth.ToString("dd MMMM yyyy"));
                document.ReplaceText("[Gender]", employee.Gender);                
                document.ReplaceText("[Marital Status]", "SLIC");
                document.ReplaceText("[Account No]", employee.BankAccountNumber);
                document.ReplaceText("[IFSC]", employee.IFSC);
                document.ReplaceText("[UAN]", employee.UAN);
                document.ReplaceText("[PAN]", employee.PAN);
                document.ReplaceText("[Aadhar]", employee.PAN);
                document.ReplaceText("[Email]", employee.Email);
                document.ReplaceText("[Mobile]", employee.MobileNumber);
                document.ReplaceText("[PF]", employee.PFNumber);
                document.ReplaceText("[Name of Supervisor]", "Shahroze Kamran Sahotra");
                document.ReplaceText("[Address]", employee.Address);
                document.ReplaceText("[Supervisor Designation]", "HR");
                document.ReplaceText("[Date of Joining]", employee.DateOfJoining.ToString("dd MMMM yyyy"));
                document.ReplaceText("[Date, Month, Year]", DateTime.Now.ToString("dd MMMM yyyy"));


                // Save the generated document
                document.SaveAs(outputPath);
            }

            return outputPath;
        }
        catch (FileFormatException ex)
        {
            // Handle the exception
            Log.Error($"Error generating appointment letter for employee {employee.FullName}: {ex.Message}");
            return null; // Indicate failure
        }
        catch (Exception ex)
        {
            // Handle other potential exceptions
            Log.Error($"Unexpected error generating appointment letter for employee {employee.FullName}: {ex.Message}");
            return null; // Indicate failure
        }
    }
    public void ServeGeneratedLetter(string outputPath, string fileName)
    {
        if (!string.IsNullOrEmpty(outputPath) && File.Exists(outputPath))
        {
            HttpContext.Current.Response.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            HttpContext.Current.Response.AppendHeader("Content-Disposition", "attachment; filename=" + fileName);
            HttpContext.Current.Response.TransmitFile(outputPath);
            HttpContext.Current.Response.End();
        }
        else
        {
            // Handle file not found scenario
            Log.Error($"Generated file not found at path: {outputPath}");
            // Optionally, inform the user that the file could not be found
            HttpContext.Current.Response.StatusCode = 404;
            HttpContext.Current.Response.StatusDescription = "File not found";
            HttpContext.Current.Response.End();
        }
    }
}