using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyWebApp.Models
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING";
        public decimal Price { get; set; }
        public Guid ApplicationId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? EaseBuzzStatus { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Error { get; set; }
        public string? CardType { get; set; }
        public string? Mode { get; set; }
        public string? NameOnCard { get; set; }
        public decimal? AmountPaid { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ApplicationId")]
        public virtual Application Application { get; set; } = null!;
    }
}