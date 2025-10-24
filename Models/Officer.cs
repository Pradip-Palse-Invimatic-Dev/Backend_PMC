using System;
using MyWebApp.Models.Common;
using MyWebApp.Models.Enums;

namespace MyWebApp.Models
{
    public class Officer : AuditableEntity
    {
        public string UserId { get; set; } = string.Empty; // Foreign key to ApplicationUser
        public ApplicationUser? User { get; set; } // Navigation property

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        public string KeyLabel { get; set; } = string.Empty;
        public string? DigitalSignaturePath { get; set; }
        public Guid? CurrentAddressId { get; set; }
        public Address? CurrentAddress { get; set; }
    }
}
