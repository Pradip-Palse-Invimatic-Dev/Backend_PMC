using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// ==================== VIEW MODELS ====================
namespace MyWebApp.ViewModels
{
    public class GenerateSECertificateRequestViewModel
    {
        [Required]
        public string ApplicationId { get; set; }

        public bool IsPayment { get; set; }
        public DateTime TransactionDate { get; set; }
        public string ChallanNumber { get; set; }
        public decimal Amount { get; set; }
    }

    public class GenerateSECertificateResponseViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string CertificateId { get; set; }
        public string CertificateNumber { get; set; }
        public string FileKey { get; set; }
        public string FileName { get; set; }
        public DateTime GeneratedDate { get; set; }
    }

    public class CertificateInfoViewModel
    {
        public string? ApplicationId { get; set; }
        public string? CertificateNumber { get; set; }
        public string? FileKey { get; set; }
        public string? FileName { get; set; }
        public DateTime? GeneratedDate { get; set; }
        public string? GeneratedBy { get; set; }
        public bool IsCertificateGenerated { get; set; }
        public string? ApplicantName { get; set; }
        public string? Position { get; set; }
    }
}