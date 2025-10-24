namespace MyWebApp.Api.Services
{
    public class BillDeskConfigService : IBillDeskConfigService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<BillDeskConfigService> _logger;

        public BillDeskConfigService(
            IConfiguration configuration,
            ILogger<BillDeskConfigService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            ValidateConfiguration();
        }

        public string MerchantId => _configuration["BillDesk:MerchantId"] ?? string.Empty;
        public string EncryptionKey => _configuration["BillDesk:EncryptionKey"] ?? string.Empty;
        public string SigningKey => _configuration["BillDesk:SigningKey"] ?? string.Empty;
        public string KeyId => _configuration["BillDesk:KeyId"] ?? string.Empty;
        public string ClientId => _configuration["BillDesk:ClientId"] ?? string.Empty;
        public string PaymentGatewayUrl => _configuration["BillDesk:PaymentGatewayUrl"] ?? "https://pay.billdesk.com/web/v1_2/embeddedsdk";
        public string ReturnUrlBase => _configuration["BillDesk:ReturnUrlBase"] ?? string.Empty;

        private void ValidateConfiguration()
        {
            var requiredKeys = new[]
            {
                "BillDesk:MerchantId",
                "BillDesk:EncryptionKey",
                "BillDesk:SigningKey",
                "BillDesk:KeyId",
                "BillDesk:ClientId",
                "BillDesk:PaymentGatewayUrl",
                "BillDesk:ReturnUrlBase"
            };

            foreach (var key in requiredKeys)
            {
                if (string.IsNullOrEmpty(_configuration[key]))
                {
                    _logger.LogError($"Missing required BillDesk configuration: {key}");
                    throw new InvalidOperationException($"Missing required configuration: {key}");
                }
            }

            _logger.LogInformation("BillDesk configuration validated successfully");
        }
    }
}