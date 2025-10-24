namespace MyWebApp.Api.Services
{
    using System.Threading.Tasks;
    using MyWebApp.ViewModels;

    public interface ISECertificateGenerationService
    {
        Task<GenerateSECertificateResponseViewModel> GenerateSECertificateAsync(
            GenerateSECertificateRequestViewModel model,
            string userId);

        Task<CertificateInfoViewModel?> GetCertificateInfoAsync(string applicationId);

        //for GetCertificateFileName
        string GetCertificateFileName(string certificateNumber);
    }
}