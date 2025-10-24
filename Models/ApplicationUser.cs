using System;
using Microsoft.AspNetCore.Identity;
using MyWebApp.Models.Common;
using MyWebApp.Models.Enums;

namespace MyWebApp.Models
{
    
    public class ApplicationUser : IdentityUser
    {
        public string EmailAddress { get; set; }
        public string Role { get; set; } = string.Empty;  // User, JuniorEngineer, AssistantEngineer, ExecutiveEngineer, CityEngineer, Clerk
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        // public DateTime? DeletedAt { get; set; }
        // public string? DeletedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? LastModifiedAt { get; set; }
        public string? LastModifiedBy { get; set; }
    }
}
