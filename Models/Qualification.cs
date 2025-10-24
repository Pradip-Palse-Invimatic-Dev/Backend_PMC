using System;
using MyWebApp.Models.Common;

namespace MyWebApp.Models
{
    public class Qualification
    {
        public Guid Id { get; set; } 
        public Guid ApplicationId { get; set; }
        public Application? Application { get; set; }
        public string InstituteName { get; set; } = string.Empty;
        public string UniversityName { get; set; } = string.Empty;
        public CourseSpecialization Specialization { get; set; }
        public string DegreeName { get; set; } = string.Empty;
        public int PassingMonth { get; set; }
        public DateTime YearOfPassing { get; set; }
    }

    public enum CourseSpecialization
    {
        Diploma,
        Degree,
        SSC,
        HSC
    }
}