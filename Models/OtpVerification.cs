using System;
using MyWebApp.Models.Common;

namespace MyWebApp.Models
{
    public class OtpVerification : AuditableEntity
    {
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string Otp { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public bool IsVerified { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public int RetryCount { get; set; }
        public DateTime Otp_Generated_At { get; set; }

        // Additional properties for digital signature use case
        public Guid? ApplicationId { get; set; }
        public Application? Application { get; set; }
        public string? Purpose { get; set; } // "DIGITAL_SIGNATURE", "LOGIN", etc.
        public bool IsUsed { get; set; }
        public DateTime? UsedAt { get; set; }
    }
}
