using System.ComponentModel.DataAnnotations;

namespace MyWebApp.ViewModels
{
    public class InitiatePaymentRequestViewModel
    {
        [Required]
        public string EntityId { get; set; } = string.Empty;
    }

    public class PaymentResponseViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string TxnEntityId { get; set; } = string.Empty;
        public string BdOrderId { get; set; } = string.Empty;
        public string RData { get; set; } = string.Empty;
        public string PaymentGatewayUrl { get; set; } = string.Empty;
    }

    public class PaymentCallbackViewModel
    {
        public string BdOrderId { get; set; } = string.Empty;
        public string RData { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // Legacy ViewModels for backward compatibility
    public class PaymentInitializationRequest
    {
        [Required]
        public Guid ApplicationId { get; set; }
    }

    public class PaymentInitializationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? BdOrderId { get; set; }
        public string? RData { get; set; }
        public string? ErrorDetails { get; set; }
    }

    public class PaymentSuccessRequest
    {
        public Guid ApplicationId { get; set; }
        public Guid? TxnEntityId { get; set; }
        public string? EntityTaskId { get; set; }
        public string EasePayId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public string Phone { get; set; } = string.Empty;
        public string? Error { get; set; }
        public string? CardType { get; set; }
        public string? Mode { get; set; }
        public string? NameOnCard { get; set; }
        public string Amount { get; set; } = string.Empty;
    }

    public class PaymentSuccessResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RedirectUrl { get; set; }
    }

    public class BillDeskEncryptionRequest
    {
        public string MerchantId { get; set; } = string.Empty;
        public string EncryptionKey { get; set; } = string.Empty;
        public string SigningKey { get; set; } = string.Empty;
        public string KeyId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string Action { get; set; } = "Encrypt";
        public string Amount { get; set; } = string.Empty;
        public string Currency { get; set; } = "356";
        public string ReturnUrl { get; set; } = string.Empty;
        public string ItemCode { get; set; } = "DIRECT";
        public string OrderDate { get; set; } = string.Empty;
        public string InitChannel { get; set; } = "internet";
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string AcceptHeader { get; set; } = "text/html";
    }

    public class BillDeskEncryptionResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class InvoiceGenerationRequest
    {
        public decimal Price { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int InvoiceNumber { get; set; }
        public DateTime IssueDate { get; set; }
        public string Status { get; set; } = "Success";
        public string CardType { get; set; } = "debitcard";
        public string PaymentSource { get; set; } = "easebuzz";
        public string Comments { get; set; } = string.Empty;
    }

    public class ChallanGenerationRequest
    {
        public string ChallanNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string AmountInWords { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string ApplicationId { get; set; } = string.Empty;
    }

    public class ChallanGenerationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ChallanPath { get; set; }
        public string? ChallanNumber { get; set; }
        public byte[]? PdfContent { get; set; }
    }

    public class CertificateGenerationRequest
    {
        public string CertificateNumber { get; set; } = string.Empty;
        public byte[]? Logo { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public int ToYear { get; set; }
        public byte[]? ProfilePhoto { get; set; }
        public string QrCodeUrl { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string ChallanNumber { get; set; } = string.Empty;
        public string TransactionDate { get; set; } = string.Empty;
        public bool IsPayment { get; set; } = true;
        public string Position { get; set; } = string.Empty;
    }
}