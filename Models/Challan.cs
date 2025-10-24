using System.ComponentModel.DataAnnotations;
using MyWebApp.Models.Common;

namespace MyWebApp.Models
{
    public class Challan : AuditableEntity
    {
        public string ChallanNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string AmountInWords { get; set; } = string.Empty;
        public DateTime ChallanDate { get; set; }
        public Guid ApplicationId { get; set; }
        public Application? Application { get; set; }
        public string? FilePath { get; set; }
        public bool IsGenerated { get; set; }

        // Additional fields from the original plugin
        public string? Number { get; set; }
        public string? Address { get; set; }
    }
}