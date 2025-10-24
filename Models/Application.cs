using System;
using System.Collections.Generic;
using MyWebApp.Models.Common;
using MyWebApp.Models.Enums;

namespace MyWebApp.Models
{
    public enum Gender
    {
        Male,
        Female,
        Other
    }
    public class Application : AuditableEntity
    {
        public string ApplicationNumber { get; set; } = string.Empty;
        public string? ApplicantId { get; set; }
        public ApplicationUser? Applicant { get; set; }
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
        public DateTime DateOfBirth { get; set; }
        public Guid? PermanentAddressId { get; set; }
        public Address? PermanentAddress { get; set; }
        public Guid? CurrentAddressId { get; set; }
        public Address? CurrentAddress { get; set; }
        public string? PANCardNumber { get; set; }
        public string? AadharCardNumber { get; set; }
        public string? COACardNumber { get; set; }
        public ApplicationStatus Status { get; set; }
        public ApplicationStage CurrentStage { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public bool IsPaymentComplete { get; set; }
        public bool IsCertificateGenerated { get; set; }
        public bool IsChallanGenerated { get; set; }
        public string? RecommendedFormPath { get; set; }
        public string? CertificatePath { get; set; }
        public string? ChallanPath { get; set; }

        // Digital signature properties
        public bool IsDigitallySigned { get; set; }
        public DateTime? DigitalSignatureDate { get; set; }
        public string? SignedBy { get; set; }

        // Certificate generation properties
        public string? CertificateNumber { get; set; }
        public DateTime? CertificateGeneratedDate { get; set; }
        public string? CertificateGeneratedBy { get; set; }

        // Navigation properties
        public List<Qualification>? Qualifications { get; set; }
        public List<Experience>? Experiences { get; set; }
        public List<Document>? Documents { get; set; }
        public List<OfficerAssignment>? OfficerAssignments { get; set; }
        public Appointment? Appointment { get; set; }
        public Payment? Payment { get; set; }
    }
}