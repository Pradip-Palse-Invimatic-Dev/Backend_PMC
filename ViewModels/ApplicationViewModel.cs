using System;
using MyWebApp.Models;
using MyWebApp.Models.Enums;

namespace MyWebApp.ViewModels
{
    public class CreateApplicationViewModel
    {
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? MotherName { get; set; }
        public string MobileNumber { get; set; } = string.Empty;
        public string EmailAddress { get; set; }
        public PositionType PositionType { get; set; }
        public string? BloodGroup { get; set; }
        public float? Height { get; set; }
        public Gender Gender { get; set; }
        public DateTime DateOfBirth { get; set; }

        // Permanent Address
        public AddressViewModel? PermanentAddress { get; set; } = new();

        // Current Address
        public AddressViewModel? CurrentAddress { get; set; } = new();

        // Document Numbers
        public string? PANCardNumber { get; set; }
        public string? AadharCardNumber { get; set; }
        public string? COACardNumber { get; set; }

        // Qualification
        public List<QualificationViewModel>? Qualifications { get; set; } = new();
        // Experience
        public List<ExperienceViewModel>? Experiences { get; set; } = new();

        // Documents
        public List<DocumentViewModel>? Documents { get; set; } = new();
    }

    public class AddressViewModel
    {
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string? AddressLine3 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
    }


    public class QualificationViewModel
    {
        public string FileId { get; set; } = string.Empty;  // Frontend will set this as "QN_1", "QN_2", etc.
        public string InstituteName { get; set; } = string.Empty;
        public string UniversityName { get; set; } = string.Empty;
        public CourseSpecialization Specialization { get; set; }
        public string DegreeName { get; set; } = string.Empty;
        public int PassingMonth { get; set; }
        public DateTime YearOfPassing { get; set; }
    }
    public class ExperienceViewModel
    {
        public string FileId { get; set; } = string.Empty;  // Frontend will set this as "EXP_1", "EXP_2", etc.
        public string CompanyName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }

    // ApplicationFilterViewModel
    public class ApplicationFilterViewModel
    {
        public string? ApplicantName { get; set; }
        public ApplicationStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    //GetApplicationsListViewModel
    public class GetApplicationsListViewModel
    {
        public string Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public PositionType PositionType { get; set; }
        public DateTime SubmissionDate { get; set; }
        public ApplicationStatus Status { get; set; }
        public ApplicationStage CurrentStage { get; set; }
    }



    //GetApplicationViewModel
    public class GetApplicationViewModel
    {
        public string Id { get; set; }
        public string ApplicationNumber { get; set; } = string.Empty;
        public string? ApplicantId { get; set; }
        public string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string LastName { get; set; }
        public string? MotherName { get; set; }
        public string MobileNumber { get; set; }
        public string EmailAddress { get; set; }
        public PositionType PositionType { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? BloodGroup { get; set; }
        public float? Height { get; set; } // in cm
        public Gender Gender { get; set; }
        public Address? PermanentAddress { get; set; }
        public Address? CurrentAddress { get; set; }
        public string? PANCardNumber { get; set; }
        public string? AadharCardNumber { get; set; }
        public string? COACardNumber { get; set; }
        public string? HighestQualification { get; set; }
        public ApplicationStatus Status { get; set; }
        public ApplicationStage CurrentStage { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public bool IsPaymentComplete { get; set; }
        public bool IsCertificateGenerated { get; set; }
        public bool IsChallanGenerated { get; set; }
        public string? RecommendedFormPath { get; set; }
        public string? CertificatePath { get; set; }
        public string? ChallanPath { get; set; }
        public List<GetDocumentViewModel>? Documents { get; set; }
        // public List<OfficerAssignment>? OfficerAssignments { get; set; }
        // public Appointment? Appointment { get; set; }
        // public Payment? Payment { get; set; }

    }

    // Schedule Appointment Request
    public class ScheduleAppointmentRequest
    {
        public Guid ApplicationId { get; set; }
        public string Comments { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
        public string ContactPerson { get; set; } = string.Empty;
        public string Place { get; set; } = string.Empty;
        public string RoomNumber { get; set; } = string.Empty;
    }
}
