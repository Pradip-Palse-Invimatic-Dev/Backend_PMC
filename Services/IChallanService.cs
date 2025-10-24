using MyWebApp.ViewModels;

namespace MyWebApp.Api.Services
{
    public interface IChallanService
    {
        Task<ChallanGenerationResponse> GenerateChallanAsync(ChallanGenerationRequest request);
        Task<byte[]?> GetChallanPdfAsync(Guid applicationId);
        Task<string?> GetChallanPathAsync(Guid applicationId);
        Task<bool> IsChallanGeneratedAsync(Guid applicationId);
    }
}