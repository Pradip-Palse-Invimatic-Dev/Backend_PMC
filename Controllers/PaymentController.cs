using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Api.Services;
using MyWebApp.ViewModels;
using System.Security.Claims;
using System.Text.Json;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentService _paymentService;
        private readonly IBillDeskPaymentService _billDeskPaymentService;
        private readonly IChallanService _challanService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            PaymentService paymentService,
            IBillDeskPaymentService billDeskPaymentService,
            IChallanService challanService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _billDeskPaymentService = billDeskPaymentService;
            _challanService = challanService;
            _logger = logger;
        }

        /// <summary>
        /// Initialize payment using new BillDesk integration
        /// </summary>
        /// <param name="model">Payment initialization request</param>
        /// <returns>Payment initialization response with BillDesk parameters</returns>
        [HttpPost("initiate")]
        [Authorize]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequestViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid request" });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                var result = await _billDeskPaymentService.InitiatePaymentAsync(model, userId, HttpContext);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        /// <summary>
        /// Initialize payment for an application (Legacy)
        /// </summary>
        /// <param name="request">Payment initialization request</param>
        /// <returns>Payment initialization response with BillDesk parameters</returns>
        [HttpPost("initialize")]
        [Authorize]
        public async Task<ActionResult<PaymentInitializationResponse>> InitializePayment([FromBody] PaymentInitializationRequest request)
        {
            try
            {
                // Get client IP address
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var result = await _paymentService.InitializePaymentAsync(request.ApplicationId, clientIp!, userAgent);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing payment for application {ApplicationId}", request.ApplicationId);
                return StatusCode(500, new PaymentInitializationResponse
                {
                    Success = false,
                    Message = "Internal server error occurred while initializing payment"
                });
            }
        }

        /// <summary>
        /// Get payment initialization view with BillDesk form
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>HTML view with payment form</returns>
        [HttpGet("view/{applicationId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPaymentView(Guid applicationId)
        {
            try
            {
                // Get client IP address
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var result = await _paymentService.InitializePaymentAsync(applicationId, clientIp!, userAgent);

                if (!result.Success)
                {
                    return BadRequest($"Error initializing payment: {result.Message}");
                }

                var html = $@"
<script src=""https://cdnjs.cloudflare.com/ajax/libs/jquery/3.6.1/jquery.min.js"" integrity=""sha512-aVKKRRi/Q/YV+4mjoKBsE4x3H+BkegoM/em46NNlCqNTmUYADjBbeNefNxYV7giUp0VxICtqdrbqU7iVaeZNXA=="" crossorigin=""anonymous"" referrerpolicy=""no-referrer""></script>
<form action=""https://pay.billdesk.com/web/v1_2/embeddedsdk"" id=""frmdata"" method=""post"">
    <input type=""hidden"" id=""merchantid"" name=""merchantid"" value=""PMCBLDGNV2"" />
    <input type=""hidden"" id=""bdorderid"" name=""bdorderid"" value=""{result.BdOrderId}"" />
    <input type=""hidden"" id=""rdata"" name=""rdata"" value=""{result.RData}"">
    <p>Please wait... </p>
</form>  
<script type=""text/javascript"">
    $(document).ready(function () {{
        $('#frmdata').submit();
    }});
</script>";

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment view for application {ApplicationId}", applicationId);
                return StatusCode(500, "Internal server error occurred while loading payment view");
            }
        }

        /// <summary>
        /// Handle payment callback from BillDesk (both success and failure)
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>Payment callback response</returns>
        [HttpPost("callback/{applicationId}")]
        [AllowAnonymous]
        public async Task<IActionResult> HandlePaymentCallback(Guid applicationId)
        {
            try
            {
                // Parse query parameters
                var queryString = Request.QueryString.ToString();
                Guid? txnEntityId = null;
                string? entityTaskId = null;

                if (queryString.Contains("txnEntityId="))
                {
                    var txnStart = queryString.IndexOf("txnEntityId=") + "txnEntityId=".Length;
                    var txnEnd = queryString.IndexOf('&', txnStart);
                    if (txnEnd == -1) txnEnd = queryString.Length;
                    if (Guid.TryParse(queryString.Substring(txnStart, txnEnd - txnStart), out var txnId))
                    {
                        txnEntityId = txnId;
                    }
                }

                // Parse callback data based on content type
                string? bdOrderId = null;
                string? status = null;
                string? errorMessage = null;

                var contentType = Request.ContentType?.ToLower() ?? "";

                if (contentType.Contains("application/x-www-form-urlencoded") && Request.HasFormContentType)
                {
                    // Parse form data
                    var form = await Request.ReadFormAsync();
                    bdOrderId = form["bdorderid"].ToString();
                    status = form["status"].ToString();
                    errorMessage = form["error_Message"].ToString();
                }
                else if (contentType.Contains("application/json"))
                {
                    // Parse JSON data
                    using var reader = new StreamReader(Request.Body);
                    var jsonContent = await reader.ReadToEndAsync();
                    _logger.LogInformation($"Received JSON callback: {jsonContent}");

                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(jsonContent);
                        var root = jsonDoc.RootElement;
                        bdOrderId = root.TryGetProperty("bdorderid", out var bdOrderProp) ? bdOrderProp.GetString() : null;
                        status = root.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : null;
                        errorMessage = root.TryGetProperty("error_Message", out var errorProp) ? errorProp.GetString() : null;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse JSON callback data");
                    }
                }
                else
                {
                    // Try to get from query parameters as fallback
                    bdOrderId = Request.Query["bdorderid"].ToString();
                    status = Request.Query["status"].ToString();
                    errorMessage = Request.Query["error_Message"].ToString();

                    _logger.LogWarning($"Unexpected content type: {contentType}. Trying query parameters.");
                }

                // Log the callback for debugging
                _logger.LogInformation($"Payment callback received for application {applicationId}: Status={status}, BdOrderId={bdOrderId}");

                if (status?.ToUpper() == "SUCCESS")
                {
                    // Handle successful payment - get additional callback data
                    var additionalData = await GetCallbackDataAsync();

                    var request = new PaymentSuccessRequest
                    {
                        ApplicationId = applicationId,
                        TxnEntityId = txnEntityId,
                        EntityTaskId = entityTaskId,
                        EasePayId = additionalData.GetValueOrDefault("easepayid", ""),
                        Status = status ?? "",
                        ErrorMessage = errorMessage,
                        Phone = additionalData.GetValueOrDefault("phone", ""),
                        Error = additionalData.GetValueOrDefault("error", ""),
                        CardType = additionalData.GetValueOrDefault("card_type", ""),
                        Mode = additionalData.GetValueOrDefault("mode", ""),
                        NameOnCard = additionalData.GetValueOrDefault("name_on_card", ""),
                        Amount = additionalData.GetValueOrDefault("amount", "")
                    };

                    var result = await _paymentService.ProcessPaymentSuccessAsync(request);

                    if (result.Success)
                    {
                        // Auto-generate challan after successful payment
                        try
                        {
                            var challanRequest = new ChallanGenerationRequest
                            {
                                ApplicationId = applicationId.ToString(),
                                Name = $"{request.NameOnCard ?? "N/A"}",
                                Position = "Licensed Engineer",
                                Amount = request.Amount ?? "0",
                                AmountInWords = ConvertAmountToWords(request.Amount ?? "0"),
                                Date = DateTime.UtcNow
                            };

                            var challanResult = await _challanService.GenerateChallanAsync(challanRequest);
                            _logger.LogInformation($"Challan generation result for application {applicationId}: {challanResult.Success}");
                        }
                        catch (Exception challanEx)
                        {
                            _logger.LogError(challanEx, $"Failed to generate challan for application {applicationId}, but payment was successful");
                            // Don't fail the payment process if challan generation fails
                        }

                        return Content(GetSuccessHtml(), "text/html");
                    }
                    else
                    {
                        return Content(GetFailureHtml($"Payment processing failed: {result.Message}"), "text/html");
                    }
                }
                else
                {
                    // Handle failed payment
                    return Content(GetFailureHtml($"Payment failed: {errorMessage}"), "text/html");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment callback for application {ApplicationId}", applicationId);
                return Content(GetFailureHtml("Internal server error occurred while processing payment"), "text/html");
            }
        }

        /// <summary>
        /// Handle payment success callback from BillDesk (Legacy)
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>Payment success response</returns>
        [HttpPost("success/{applicationId}")]
        [AllowAnonymous]
        public async Task<IActionResult> HandlePaymentSuccess(Guid applicationId)
        {
            try
            {
                // Parse query parameters
                var queryString = Request.QueryString.ToString();
                Guid? txnEntityId = null;
                string? entityTaskId = null;

                if (queryString.Contains("txnEntityId="))
                {
                    var txnStart = queryString.IndexOf("txnEntityId=") + "txnEntityId=".Length;
                    var txnEnd = queryString.IndexOf('&', txnStart);
                    if (txnEnd == -1) txnEnd = queryString.Length;
                    if (Guid.TryParse(queryString.Substring(txnStart, txnEnd - txnStart), out var txnId))
                    {
                        txnEntityId = txnId;
                    }
                }

                if (queryString.Contains("entityTaskId="))
                {
                    var taskStart = queryString.IndexOf("entityTaskId=") + "entityTaskId=".Length;
                    var taskEnd = queryString.IndexOf('&', taskStart);
                    if (taskEnd == -1) taskEnd = queryString.Length;
                    entityTaskId = queryString.Substring(taskStart, taskEnd - taskStart);
                }

                // Parse callback data based on content type (same as HandlePaymentCallback)
                var callbackData = await GetCallbackDataAsync();

                var request = new PaymentSuccessRequest
                {
                    ApplicationId = applicationId,
                    TxnEntityId = txnEntityId,
                    EntityTaskId = entityTaskId,
                    EasePayId = callbackData.GetValueOrDefault("easepayid", ""),
                    Status = callbackData.GetValueOrDefault("status", ""),
                    ErrorMessage = callbackData.GetValueOrDefault("error_Message", ""),
                    Phone = callbackData.GetValueOrDefault("phone", ""),
                    Error = callbackData.GetValueOrDefault("error", ""),
                    CardType = callbackData.GetValueOrDefault("card_type", ""),
                    Mode = callbackData.GetValueOrDefault("mode", ""),
                    NameOnCard = callbackData.GetValueOrDefault("name_on_card", ""),
                    Amount = callbackData.GetValueOrDefault("amount", "")
                };

                var result = await _paymentService.ProcessPaymentSuccessAsync(request);

                if (result.Success)
                {
                    // Auto-generate challan after successful payment
                    try
                    {
                        var challanRequest = new ChallanGenerationRequest
                        {
                            ApplicationId = applicationId.ToString(),
                            Name = request.NameOnCard ?? "N/A",
                            Position = "Licensed Engineer",
                            Amount = request.Amount ?? "0",
                            AmountInWords = ConvertAmountToWords(request.Amount ?? "0"),
                            Date = DateTime.UtcNow
                        };

                        var challanResult = await _challanService.GenerateChallanAsync(challanRequest);
                        _logger.LogInformation($"Challan generation result for application {applicationId}: {challanResult.Success}");
                    }
                    catch (Exception challanEx)
                    {
                        _logger.LogError(challanEx, $"Failed to generate challan for application {applicationId}, but payment was successful");
                        // Don't fail the payment process if challan generation fails
                    }

                    // Return success page
                    var successHtml = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Payment Successful</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; background-color: #f0f8ff; margin: 0; padding: 50px; }
        .container { background: white; max-width: 500px; margin: 0 auto; padding: 40px; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }
        .success-icon { font-size: 72px; color: #28a745; margin-bottom: 20px; }
        h1 { color: #28a745; margin-bottom: 20px; }
        p { color: #666; margin-bottom: 15px; }
        .btn { display: inline-block; background: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 20px; }
        .btn:hover { background: #0056b3; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""success-icon"">&#10004;</div>
        <h1>Payment Successful</h1>
        <p>Thank you for your payment. Your transaction has been successfully processed.</p>
        <p>An email receipt with your order details has been sent to your inbox.</p>
        <a href=""https://pmcrms.ezbricks-dev.codetools.in/#/guest-login"" class=""btn"">Return to HomePage</a>
    </div>
</body>
</html>";

                    return Content(successHtml, "text/html");
                }

                return BadRequest($"Payment processing failed: {result.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment success for application {ApplicationId}", applicationId);
                return StatusCode(500, "Internal server error occurred while processing payment");
            }
        }

        /// <summary>
        /// Get payment status for an application
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>Payment status information</returns>
        [HttpGet("status/{applicationId}")]
        public async Task<IActionResult> GetPaymentStatus(Guid applicationId)
        {
            try
            {
                // This would typically check the transaction status in the database
                // For now, return a simple response
                return Ok(new { ApplicationId = applicationId, Message = "Payment status check not implemented yet" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting payment status for application {ApplicationId}", applicationId);
                return StatusCode(500, "Internal server error occurred while checking payment status");
            }
        }

        private string GetSuccessHtml()
        {
            return @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Payment Successful</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; background-color: #f0f8ff; margin: 0; padding: 50px; }
        .container { background: white; max-width: 500px; margin: 0 auto; padding: 40px; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }
        .success-icon { font-size: 72px; color: #28a745; margin-bottom: 20px; }
        h1 { color: #28a745; margin-bottom: 20px; }
        p { color: #666; margin-bottom: 15px; }
        .btn { display: inline-block; background: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 20px; }
        .btn:hover { background: #0056b3; }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""success-icon"">&#10004;</div>
        <h1>Payment Successful</h1>
        <p>Thank you for your payment. Your transaction has been successfully processed.</p>
        <p>An email receipt with your order details has been sent to your inbox.</p>
        <a href=""https://pmcrms.ezbricks-dev.codetools.in/#/guest-login"" class=""btn"">Return to HomePage</a>
    </div>
</body>
</html>";
        }

        private string GetFailureHtml(string errorMessage)
        {
            return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Payment Failed</title>
    <style>
        body {{ font-family: Arial, sans-serif; text-align: center; background-color: #fff5f5; margin: 0; padding: 50px; }}
        .container {{ background: white; max-width: 500px; margin: 0 auto; padding: 40px; border-radius: 10px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .error-icon {{ font-size: 72px; color: #dc3545; margin-bottom: 20px; }}
        h1 {{ color: #dc3545; margin-bottom: 20px; }}
        p {{ color: #666; margin-bottom: 15px; }}
        .btn {{ display: inline-block; background: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin-top: 20px; }}
        .btn:hover {{ background: #0056b3; }}
        .error-details {{ background: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #dc3545; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""error-icon"">&#10060;</div>
        <h1>Payment Failed</h1>
        <p>Unfortunately, your payment could not be processed.</p>
        <div class=""error-details"">
            <strong>Error:</strong> {errorMessage}
        </div>
        <p>Please try again or contact support if the problem persists.</p>
        <a href=""https://pmcrms.ezbricks-dev.codetools.in/#/guest-login"" class=""btn"">Return to HomePage</a>
    </div>
</body>
</html>";
        }

        private async Task<Dictionary<string, string>> GetCallbackDataAsync()
        {
            var data = new Dictionary<string, string>();

            try
            {
                var contentType = Request.ContentType?.ToLower() ?? "";

                if (contentType.Contains("application/x-www-form-urlencoded") && Request.HasFormContentType)
                {
                    // Parse form data
                    var form = await Request.ReadFormAsync();
                    foreach (var key in form.Keys)
                    {
                        data[key] = form[key].ToString();
                    }
                }
                else if (contentType.Contains("application/json"))
                {
                    // Parse JSON data
                    Request.Body.Seek(0, SeekOrigin.Begin);
                    using var reader = new StreamReader(Request.Body);
                    var jsonContent = await reader.ReadToEndAsync();

                    if (!string.IsNullOrEmpty(jsonContent))
                    {
                        using var jsonDoc = JsonDocument.Parse(jsonContent);
                        var root = jsonDoc.RootElement;

                        foreach (var property in root.EnumerateObject())
                        {
                            data[property.Name] = property.Value.GetString() ?? "";
                        }
                    }
                }
                else
                {
                    // Try to get from query parameters as fallback
                    foreach (var key in Request.Query.Keys)
                    {
                        data[key] = Request.Query[key].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing callback data");
            }

            return data;
        }

        private string ConvertAmountToWords(string amount)
        {
            try
            {
                if (decimal.TryParse(amount, out var decimalAmount))
                {
                    // Simple number to words conversion for common amounts
                    var intAmount = (int)decimalAmount;
                    return intAmount switch
                    {
                        0 => "Zero Rupees Only",
                        1000 => "One Thousand Rupees Only",
                        2000 => "Two Thousand Rupees Only",
                        3000 => "Three Thousand Rupees Only",
                        4000 => "Four Thousand Rupees Only",
                        5000 => "Five Thousand Rupees Only",
                        10000 => "Ten Thousand Rupees Only",
                        15000 => "Fifteen Thousand Rupees Only",
                        20000 => "Twenty Thousand Rupees Only",
                        25000 => "Twenty Five Thousand Rupees Only",
                        50000 => "Fifty Thousand Rupees Only",
                        100000 => "One Lakh Rupees Only",
                        _ => $"{intAmount} Rupees Only"
                    };
                }
                return "Amount in Words";
            }
            catch
            {
                return "Amount in Words";
            }
        }
    }
}