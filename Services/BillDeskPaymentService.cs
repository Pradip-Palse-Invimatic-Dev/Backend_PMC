using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.Models.Enums;
using MyWebApp.ViewModels;

namespace MyWebApp.Api.Services
{
    public class BillDeskPaymentService : IBillDeskPaymentService
    {
        private readonly IBillDeskConfigService _configService;
        private readonly IPluginContextService _pluginContextService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BillDeskPaymentService> _logger;

        public BillDeskPaymentService(
            IBillDeskConfigService configService,
            IPluginContextService pluginContextService,
            ApplicationDbContext context,
            ILogger<BillDeskPaymentService> logger)
        {
            _configService = configService;
            _pluginContextService = pluginContextService;
            _context = context;
            _logger = logger;
        }

        public async Task<PaymentResponseViewModel> InitiatePaymentAsync(
            InitiatePaymentRequestViewModel model,
            string userId,
            HttpContext httpContext)
        {
            try
            {
                _logger.LogInformation($"Initiating payment for EntityId: {model.EntityId}");

                // Step 1: Get entity fields
                var fields = await _pluginContextService.GetEntityFieldsById(model.EntityId);
                var firstName = fields.FirstName?.ToString();
                var lastName = fields.LastName?.ToString();
                var email = fields.EmailAddress?.ToString();
                var mobileNumber = fields.MobileNumber?.ToString();
                var finalFee = fields.Price?.ToString();

                if (string.IsNullOrEmpty(finalFee))
                {
                    return new PaymentResponseViewModel
                    {
                        Success = false,
                        Message = "Price not found in entity"
                    };
                }

                // Step 2: Generate unique transaction ID
                string transactionId = _pluginContextService.RandomNumber(12);
                _logger.LogInformation($"Generated TransactionId: {transactionId}");

                // Step 3: Create Transaction Entity
                dynamic txnEntity = new System.Dynamic.ExpandoObject();
                txnEntity.TransactionId = transactionId;
                txnEntity.Status = "PENDING";
                txnEntity.Price = finalFee;
                txnEntity.ApplicationId = model.EntityId;
                txnEntity.FirstName = firstName;
                txnEntity.LastName = lastName;
                txnEntity.Email = email;
                txnEntity.PhoneNumber = mobileNumber;

                var txnEntityId = await _pluginContextService.CreateEntity("Transaction", "", txnEntity);
                _logger.LogInformation($"Created Transaction Entity: {txnEntityId}");

                // Step 4: Generate Order IDs and timestamps
                string orderId = GenerateOrderId();
                string traceId = orderId.ToLower();
                string bdTimestamp = GetIstTimestamp();

                _logger.LogInformation($"Generated OrderId: {orderId}, TraceId: {traceId}, Timestamp: {bdTimestamp}");

                // Step 5: Get client IP
                string ipAddress = GetClientIpAddress(httpContext);
                string userAgent = httpContext.Request.Headers["User-Agent"].ToString() ??
                                  "Mozilla/5.0(WindowsNT10.0;WOW64;)Gecko/20100101Firefox/51.0";

                // Step 6: Encrypt payment data
                var encryptionResult = await EncryptPaymentDataAsync(
                    orderId, finalFee, model.EntityId, txnEntityId, ipAddress, userAgent);

                if (!encryptionResult.Success)
                {
                    return new PaymentResponseViewModel
                    {
                        Success = false,
                        Message = $"Encryption failed: {encryptionResult.Message}"
                    };
                }

                // Step 7: Call BillDesk payment API
                var paymentResult = await CallBillDeskPaymentApiAsync(
                    traceId, bdTimestamp, encryptionResult.EncryptedBody);

                if (!paymentResult.Success)
                {
                    return new PaymentResponseViewModel
                    {
                        Success = false,
                        Message = $"Payment API failed: {paymentResult.Message}"
                    };
                }

                // Step 8: Decrypt payment response
                var decryptResult = await DecryptPaymentResponseAsync(paymentResult.EncryptedResponse);

                if (!decryptResult.Success)
                {
                    return new PaymentResponseViewModel
                    {
                        Success = false,
                        Message = $"Decryption failed: {decryptResult.Message}"
                    };
                }

                // Step 9: Extract BdOrderId and RData
                var paymentDetails = ExtractPaymentDetails(decryptResult.DecryptedData);
                string bdOrderId = paymentDetails.Item1;
                string rData = paymentDetails.Item2;

                return new PaymentResponseViewModel
                {
                    Success = true,
                    Message = "Payment initiated successfully",
                    TransactionId = transactionId,
                    TxnEntityId = txnEntityId,
                    BdOrderId = bdOrderId,
                    RData = rData,
                    PaymentGatewayUrl = _configService.PaymentGatewayUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment");
                return new PaymentResponseViewModel
                {
                    Success = false,
                    Message = $"Error initiating payment: {ex.Message}"
                };
            }
        }

        public async Task<PaymentResponseViewModel> InitiatePaymentLegacyAsync(Guid applicationId, string clientIp, string userAgent)
        {
            try
            {
                var application = await _context.Applications.FindAsync(applicationId);
                if (application == null)
                {
                    return new PaymentResponseViewModel
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                // Use the new service but adapt for legacy interface
                var request = new InitiatePaymentRequestViewModel
                {
                    EntityId = applicationId.ToString()
                };

                var httpContext = new DefaultHttpContext();
                httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse(clientIp);
                httpContext.Request.Headers["User-Agent"] = userAgent;

                var result = await InitiatePaymentAsync(request, "", httpContext);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in legacy payment initialization");
                return new PaymentResponseViewModel
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        private async Task<EncryptionResult> EncryptPaymentDataAsync(
            string orderId, string amount, string entityId, string txnEntityId,
            string ipAddress, string userAgent)
        {
            try
            {
                var iso8601String = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");

                dynamic input = new System.Dynamic.ExpandoObject();
                input.MerchantId = _configService.MerchantId;
                input.EncryptionKey = _configService.EncryptionKey;
                input.SigningKey = _configService.SigningKey;
                input.keyId = _configService.KeyId;
                input.clientId = _configService.ClientId;
                input.orderid = orderId;
                input.Action = "Encrypt";
                input.amount = amount;
                input.currency = "356";
                input.ReturnUrl = $"{_configService.ReturnUrlBase}/{entityId}?txnEntityId={txnEntityId}";
                input.itemcode = "DIRECT";
                input.OrderDate = iso8601String;
                input.InitChannel = "internet";
                input.IpAddress = ipAddress;
                input.UserAgent = userAgent;
                input.AcceptHeader = "text/html";

                _logger.LogInformation("Calling BillDesk encryption");
                dynamic response = await _pluginContextService.Invoke("BILLDESK", input);

                if (response?.Status != "SUCCESS")
                {
                    return new EncryptionResult { Success = false, Message = response?.Message ?? "Unknown error" };
                }

                return new EncryptionResult
                {
                    Success = true,
                    EncryptedBody = response.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting payment data");
                return new EncryptionResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<PaymentApiResult> CallBillDeskPaymentApiAsync(
            string traceId, string bdTimestamp, string encryptedBody)
        {
            try
            {
                dynamic input = new System.Dynamic.ExpandoObject();
                input.Path = "orders/create";
                input.Method = "POST";
                input.Headers = $"BD-Traceid: {traceId}\r\nBD-Timestamp: {bdTimestamp}\r\nContent-Type: application/jose\r\nAccept: application/jose";
                input.Body = Encoding.UTF8.GetBytes(encryptedBody);

                _logger.LogInformation("Calling BillDesk payment API");
                dynamic output = await _pluginContextService.Invoke("HTTPPayment", input);

                if (output?.Status != "SUCCESS")
                {
                    return new PaymentApiResult { Success = false, Message = output?.Message ?? "Unknown error" };
                }

                string encryptedResponse = Encoding.UTF8.GetString(output.Content);
                return new PaymentApiResult
                {
                    Success = true,
                    EncryptedResponse = encryptedResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling payment API");
                return new PaymentApiResult { Success = false, Message = ex.Message };
            }
        }

        private async Task<DecryptionResult> DecryptPaymentResponseAsync(string encryptedResponse)
        {
            try
            {
                dynamic input = new System.Dynamic.ExpandoObject();
                input.EncryptionKey = _configService.EncryptionKey;
                input.SigningKey = _configService.SigningKey;
                input.Action = "Decrypt";
                input.responseBody = encryptedResponse;

                _logger.LogInformation("Decrypting payment response");
                dynamic response = await _pluginContextService.Invoke("BILLDESK", input);

                if (response?.Status != "SUCCESS")
                {
                    return new DecryptionResult { Success = false, Message = response?.Message ?? "Unknown error" };
                }

                return new DecryptionResult
                {
                    Success = true,
                    DecryptedData = response.Message.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrypting payment response");
                return new DecryptionResult { Success = false, Message = ex.Message };
            }
        }

        private (string bdOrderId, string rData) ExtractPaymentDetails(string jsonString)
        {
            try
            {
                string bdOrderId = "";
                string rData = "";

                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    JsonElement root = doc.RootElement;

                    if (root.TryGetProperty("links", out JsonElement links) &&
                        links.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement link in links.EnumerateArray())
                        {
                            if (link.TryGetProperty("rel", out JsonElement relProp) &&
                                relProp.GetString() == "redirect")
                            {
                                if (link.TryGetProperty("parameters", out JsonElement parameters))
                                {
                                    if (parameters.TryGetProperty("bdorderid", out JsonElement bdorderidProp))
                                        bdOrderId = bdorderidProp.GetString() ?? "";

                                    if (parameters.TryGetProperty("rdata", out JsonElement rdataProp))
                                        rData = rdataProp.GetString() ?? "";

                                    break;
                                }
                            }
                        }
                    }
                }

                _logger.LogInformation($"Extracted BdOrderId: {bdOrderId}");
                return (bdOrderId, rData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting payment details");
                throw;
            }
        }

        private string GenerateOrderId()
        {
            string prefix = "PMC";
            string timestamp = DateTime.UtcNow.ToString("yyMMddHHmm");
            Random random = new Random();
            int randomSuffix = random.Next(100, 999);
            return $"{prefix}{timestamp}{randomSuffix}".Substring(0, 13);
        }

        private string GetClientIpAddress(HttpContext httpContext)
        {
            var remoteIp = httpContext.Connection.RemoteIpAddress;

            if (remoteIp == null)
                return "127.0.0.1";

            if (remoteIp.IsIPv4MappedToIPv6)
                return remoteIp.MapToIPv4().ToString();

            return remoteIp.ToString();
        }

        private string GetIstTimestamp()
        {
            try
            {
                var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);
                return istNow.ToString("yyyyMMddHHmmss");
            }
            catch
            {
                // Fallback if IST timezone is not available
                var istNow = DateTime.UtcNow.AddHours(5).AddMinutes(30);
                return istNow.ToString("yyyyMMddHHmmss");
            }
        }

        private class EncryptionResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string EncryptedBody { get; set; } = string.Empty;
        }

        private class PaymentApiResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string EncryptedResponse { get; set; } = string.Empty;
        }

        private class DecryptionResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public string DecryptedData { get; set; } = string.Empty;
        }
    }
}