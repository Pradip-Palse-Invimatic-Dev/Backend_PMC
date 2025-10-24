namespace MyWebApp.Controllers
{
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using MyWebApp.Api.Services;
    using MyWebApp.ViewModels;

    [ApiController]
    [Route("api/[controller]")]
    public class CertificateController : ControllerBase
    {
        private readonly ISECertificateGenerationService _certificateService;
        private readonly FileService _fileService;
        private readonly ILogger<CertificateController> _logger;

        public CertificateController(
            ISECertificateGenerationService certificateService,
            FileService fileService,
            ILogger<CertificateController> logger)
        {
            _certificateService = certificateService;
            _fileService = fileService;
            _logger = logger;
        }

        [HttpPost("generate-se-certificate")]
        // [Authorize(Roles = "Admin,ExecutiveEngineer,CityEngineer")]
        public async Task<IActionResult> GenerateSECertificate([FromBody] GenerateSECertificateRequestViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Validation failed",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "User not authenticated" });
                }

                _logger.LogInformation($"Generating SE Certificate for ApplicationId: {model.ApplicationId}");

                var result = await _certificateService.GenerateSECertificateAsync(model, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SE certificate");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while generating the certificate"
                });
            }
        }

        [HttpGet("info/{applicationId}")]
        // [Authorize]
        public async Task<IActionResult> GetCertificateInfo(string applicationId)
        {
            try
            {
                if (string.IsNullOrEmpty(applicationId))
                {
                    return BadRequest(new { success = false, message = "Application ID is required" });
                }

                _logger.LogInformation($"Getting certificate info for ApplicationId: {applicationId}");

                var certificateInfo = await _certificateService.GetCertificateInfoAsync(applicationId);

                if (certificateInfo == null)
                {
                    return NotFound(new { success = false, message = "Certificate not found for this application" });
                }

                return Ok(new
                {
                    success = true,
                    data = certificateInfo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certificate info for ApplicationId: {ApplicationId}", applicationId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while retrieving certificate information"
                });
            }
        }

        [HttpGet("download/{fileKey}")]
        // [Authorize]
        public async Task<IActionResult> DownloadCertificate(string fileKey)
        {
            try
            {
                if (string.IsNullOrEmpty(fileKey))
                {
                    return BadRequest(new { success = false, message = "File key is required" });
                }

                _logger.LogInformation($"Downloading certificate with file key: {fileKey}");

                // Read the file bytes from storage
                var fileBytes = await _fileService.ReadFileAsync(fileKey);

                if (fileBytes == null || fileBytes.Length == 0)
                {
                    return NotFound(new { success = false, message = "Certificate file not found" });
                }

                // Extract original filename from fileKey (remove GUID prefix)
                var originalFileName = ExtractOriginalFileName(fileKey);

                // Set the content type for PDF
                var contentType = "application/pdf";

                _logger.LogInformation($"Successfully retrieved certificate file: {originalFileName}");

                // Return file as download
                return File(fileBytes, contentType, originalFileName);
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning($"Certificate file not found: {fileKey}");
                return NotFound(new { success = false, message = "Certificate file not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate with file key: {FileKey}", fileKey);
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while downloading the certificate"
                });
            }
        }

        private string ExtractOriginalFileName(string fileKey)
        {
            try
            {
                // FileKey format is typically: "GUID_OriginalFileName"
                var parts = fileKey.Split('_', 2);
                if (parts.Length > 1)
                {
                    return parts[1]; // Return the original filename part
                }
                return fileKey; // Return as-is if no underscore found
            }
            catch
            {
                return "certificate.pdf"; // Fallback filename
            }
        }

    }
}