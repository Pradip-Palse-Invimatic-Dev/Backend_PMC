using System;
using MyWebApp.Models.Common;

namespace MyWebApp.Models
{
    public enum PaymentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed,
        Refunded
    }

    public enum PaymentMode
    {
        CreditCard,
        DebitCard,
        NetBanking,
        UPI,
        Wallet
    }

    public class Payment : AuditableEntity
    {
        public Guid ApplicationId { get; set; }
        public Application? Application { get; set; }
        public decimal Amount { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public PaymentStatus Status { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMode PaymentMode { get; set; }
        public string EasebuzzOrderId { get; set; } = string.Empty;
        public string EasebuzzTransactionId { get; set; } = string.Empty;
    }
}
