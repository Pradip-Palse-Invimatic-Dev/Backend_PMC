namespace MyWebApp.Api.Services
{

    // public class SECertificateModel
    // {
    //     public string CertificateNumber { get; set; }
    //     public string Name { get; set; }
    //     public string Address { get; set; }
    //     public string Position { get; set; }
    //     public DateTime FromDate { get; set; }
    //     public int ToYear { get; set; }
    //     public byte[] Logo { get; set; }
    //     public byte[] ProfilePhoto { get; set; }
    //     public string QrCodeUrl { get; set; }
    //     public bool IsPayment { get; set; }
    //     public string TransactionDate { get; set; }
    //     public string ChallanNumber { get; set; }
    //     public string Amount { get; set; }
    // }

    // Update SECertificateModel to use QrCodeBytes instead of QrCodeUrl
    public class SECertificateModel
    {
        public string CertificateNumber { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Position { get; set; }
        public DateTime FromDate { get; set; }
        public int ToYear { get; set; }
        public byte[] Logo { get; set; }
        public byte[] ProfilePhoto { get; set; }
        public byte[] QrCodeBytes { get; set; }
        public bool IsPayment { get; set; }
        public string TransactionDate { get; set; }
        public string ChallanNumber { get; set; }
        public string Amount { get; set; }
    }
}