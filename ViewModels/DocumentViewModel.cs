using MyWebApp.Models;

namespace MyWebApp.ViewModels
{
    public class DocumentViewModel
    {
        public DocumentType DocumentType { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;  // FileId to match with QN_* or EXP_* pattern
    }

    //get document by id
    public class GetDocumentViewModel
    {
        public Guid Id { get; set; }
        public DocumentType DocumentType { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileId { get; set; } = string.Empty;
        // public DateTime UploadDate { get; set; }
        // public bool IsVerified { get; set; }
        // public string? VerificationComments { get; set; }
        // public string? VerifiedByOfficerId { get; set; }
        // public Officer? VerifiedByOfficer { get; set; }
    }
}
