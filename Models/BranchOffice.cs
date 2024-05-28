//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Cities_States.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public partial class BranchOffice
    {
        public int BranchID { get; set; }
        public int ClientID { get; set; }
        public string BranchCode { get; set; }
        public string State { get; set; }
        public string District { get; set; }
        public string BranchAddress { get; set; }
        public bool IsCLRALicenseApplicable { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]

        public Nullable<System.DateTime> CLRALicenseExpiry { get; set; }
        public string CLRALicense { get; set; }
    
        public virtual Client Client { get; set; }
    }
}
