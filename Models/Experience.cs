//Experience added by applicant in application form and stored in Experience table
using System;
using MyWebApp.Models.Common;

namespace MyWebApp.Models
{
    public class Experience
    {
        public Guid Id { get; set; } 
        public Guid ApplicationId { get; set; }
        public Application? Application { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}