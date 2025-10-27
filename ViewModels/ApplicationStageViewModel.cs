using System.ComponentModel.DataAnnotations;
using MyWebApp.Models.Enums;

namespace MyWebApp.ViewModels
{
    public class UpdateApplicationStageViewModel
    {
        [Required]
        public Guid ApplicationId { get; set; }

        [Required]
        public ApplicationStage NewStage { get; set; }

        [MaxLength(1000)]
        public string? Comments { get; set; }

        [Required]
        public string OfficerId { get; set; } = string.Empty;

        public bool IsApproved { get; set; } = true;
    }

    public class GenerateOtpViewModel
    {
        [Required]
        public Guid ApplicationId { get; set; }

        [Required]
        public string OfficerId { get; set; } = string.Empty;
    }

    public class ApplyDigitalSignatureViewModel
    {
        [Required]
        public Guid ApplicationId { get; set; }

        [Required]
        public string Otp { get; set; } = string.Empty;

        [Required]
        public string OfficerId { get; set; } = string.Empty;
    }

    public class ApplyCertificateSignatureViewModel
    {
        [Required]
        public Guid ApplicationId { get; set; }

        [Required]
        public string Otp { get; set; } = string.Empty;

        [Required]
        public string OfficerId { get; set; } = string.Empty;
    }

    public class StageUpdateResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public ApplicationStage CurrentStage { get; set; }
        public string? NextAction { get; set; }
    }
}