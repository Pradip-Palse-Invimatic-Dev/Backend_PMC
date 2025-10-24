namespace MyWebApp.Api.Services
{
    public interface IBillDeskConfigService
    {
        string MerchantId { get; }
        string EncryptionKey { get; }
        string SigningKey { get; }
        string KeyId { get; }
        string ClientId { get; }
        string PaymentGatewayUrl { get; }
        string ReturnUrlBase { get; }
    }
}