using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.Models.Enums;
using MyWebApp.ViewModels;
using System.Text;

namespace MyWebApp.Api.Services
{
    public class PaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBillDeskPaymentService _billDeskPaymentService;

        public PaymentService(
            ApplicationDbContext context,
            ILogger<PaymentService> logger,
            IHttpClientFactory httpClientFactory,
            IBillDeskPaymentService billDeskPaymentService)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _billDeskPaymentService = billDeskPaymentService;
        }

        public async Task<PaymentInitializationResponse> InitializePaymentAsync(Guid applicationId, string clientIp, string userAgent)
        {
            try
            {
                // Use the new BillDesk payment service
                var result = await _billDeskPaymentService.InitiatePaymentLegacyAsync(applicationId, clientIp, userAgent);

                return new PaymentInitializationResponse
                {
                    Success = result.Success,
                    Message = result.Message,
                    BdOrderId = result.BdOrderId,
                    RData = result.RData,
                    ErrorDetails = result.Success ? null : result.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing payment");
                return new PaymentInitializationResponse
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorDetails = ex.Message
                };
            }
        }

        public async Task<PaymentSuccessResponse> ProcessPaymentSuccessAsync(PaymentSuccessRequest request)
        {
            try
            {
                var application = await _context.Applications.FindAsync(request.ApplicationId);
                if (application == null)
                {
                    return new PaymentSuccessResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                // Update transaction if provided
                if (request.TxnEntityId.HasValue)
                {
                    var transaction = await _context.Transactions.FindAsync(request.TxnEntityId.Value);
                    if (transaction != null)
                    {
                        transaction.Status = "SUCCESS";
                        transaction.EaseBuzzStatus = request.Status;
                        transaction.ErrorMessage = request.ErrorMessage;
                        transaction.CardType = request.CardType;
                        transaction.Mode = request.Mode;
                        transaction.UpdatedAt = DateTime.UtcNow;
                        if (decimal.TryParse(request.Amount, out var amount))
                        {
                            transaction.AmountPaid = amount;
                        }
                    }
                }

                // Update application status
                application.Status = ApplicationStatus.PaymentCompleted;
                application.CurrentStage = ApplicationStage.CLERK_PENDING;
                application.IsPaymentComplete = true;

                await _context.SaveChangesAsync();

                return new PaymentSuccessResponse
                {
                    Success = true,
                    Message = "Payment processed successfully",
                    RedirectUrl = "https://pmcrms.ezbricks-dev.codetools.in/#/guest-login"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment success");
                return new PaymentSuccessResponse
                {
                    Success = false,
                    Message = ex.Message
                };
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

        private string GenerateRandomNumber(int length)
        {
            Random random = new Random();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                sb.Append(random.Next(0, 10));
            }
            return sb.ToString();
        }
    }
}