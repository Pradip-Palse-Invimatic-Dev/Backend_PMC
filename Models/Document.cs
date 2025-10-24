using System;
using MyWebApp.Models.Common;

namespace MyWebApp.Models
{
    public enum DocumentType
    {
        //
        AddressProof,
        PANCard,
        AadhaarCard,
        QualificationCertificate,
        QualificationLastYearMarksheet,
        ExperienceCertificate,
        AdditionalDocument,
        SelfDeclarationForm,
        ProfilePicture,
        PropertyDocument,
        COADocument, //Council of Architecture Document
        ArchitecturalPlan,
        StructuralPlan,
        NOC,
        RecommendedForm,
        Certificate,
        Challan,
        Other
    }

    public class Document : AuditableEntity
    {
        public Guid ApplicationId { get; set; }
        public Application? Application { get; set; }
        public Guid? QualificationId { get; set; }
        public Guid? ExperienceId { get; set; }
        public DocumentType DocumentType { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public bool IsVerified { get; set; }
        public string? VerificationComments { get; set; }
        public Guid? VerifiedByOfficerId { get; set; }
        public Officer? VerifiedByOfficer { get; set; }
    }
}
