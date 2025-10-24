using MyWebApp.ViewModels;

namespace MyWebApp.Api.Services
{
    public interface IBillDeskPaymentService
    {
        Task<PaymentResponseViewModel> InitiatePaymentAsync(InitiatePaymentRequestViewModel model, string userId, HttpContext httpContext);
        Task<PaymentResponseViewModel> InitiatePaymentLegacyAsync(Guid applicationId, string clientIp, string userAgent);
    }
}