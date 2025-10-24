using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Api.Services;
using MyWebApp.ViewModels;
using System.Security.Claims;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChallanController : ControllerBase
    {
        private readonly IChallanService _challanService;
        private readonly ILogger<ChallanController> _logger;

        public ChallanController(IChallanService challanService, ILogger<ChallanController> logger)
        {
            _challanService = challanService;
            _logger = logger;
        }

        /// <summary>
        /// Generate challan for an application
        /// </summary>
        /// <param name="request">Challan generation request</param>
        /// <returns>Challan generation response</returns>
        [HttpPost("generate")]
        public async Task<ActionResult<ChallanGenerationResponse>> GenerateChallan([FromBody] ChallanGenerationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ChallanGenerationResponse
                    {
                        Success = false,
                        Message = "Invalid request data"
                    });
                }

                var result = await _challanService.GenerateChallanAsync(request);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating challan for application {ApplicationId}", request.ApplicationId);
                return StatusCode(500, new ChallanGenerationResponse
                {
                    Success = false,
                    Message = "Internal server error occurred while generating challan"
                });
            }
        }

        /// <summary>
        /// Download challan PDF for an application
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>PDF file</returns>
        [HttpGet("download/{applicationId}")]
        public async Task<IActionResult> DownloadChallan(Guid applicationId)
        {
            try
            {
                var pdfBytes = await _challanService.GetChallanPdfAsync(applicationId);

                if (pdfBytes == null)
                {
                    return NotFound("Challan not found or not generated yet");
                }

                return File(pdfBytes, "application/pdf", $"Challan_{applicationId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading challan for application {ApplicationId}", applicationId);
                return StatusCode(500, "Internal server error occurred while downloading challan");
            }
        }

        /// <summary>
        /// Check if challan is generated for an application
        /// </summary>
        /// <param name="applicationId">Application ID</param>
        /// <returns>Challan status</returns>
        [HttpGet("status/{applicationId}")]
        public async Task<IActionResult> GetChallanStatus(Guid applicationId)
        {
            try
            {
                var isGenerated = await _challanService.IsChallanGeneratedAsync(applicationId);
                var challanPath = await _challanService.GetChallanPathAsync(applicationId);

                return Ok(new
                {
                    ApplicationId = applicationId,
                    IsGenerated = isGenerated,
                    ChallanPath = challanPath,
                    DownloadUrl = isGenerated ? $"/api/challan/download/{applicationId}" : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking challan status for application {ApplicationId}", applicationId);
                return StatusCode(500, "Internal server error occurred while checking challan status");
            }
        }

        // /// <summary>
        // /// Generate challan via plugin service (for compatibility with existing workflow)
        // /// </summary>
        // /// <param name="applicationId">Application ID</param>
        // /// <returns>Challan generation response</returns>
        // [HttpPost("generate-via-plugin/{applicationId}")]
        // public async Task<IActionResult> GenerateChallanViaPlugin(Guid applicationId)
        // {
        //     try
        //     {
        //         // This method can be used to integrate with the existing plugin architecture
        //         // For now, we'll call the service directly with default values
        //         var request = new ChallanGenerationRequest
        //         {
        //             ApplicationId = applicationId.ToString(),
        //             ChallanNumber = "", // Will be auto-generated
        //             Name = "Placeholder Name", // Should be populated from application data
        //             Position = "Licensed Engineer",
        //             Amount = "5000", // Should be fetched from payment/configuration
        //             AmountInWords = "Five Thousand Rupees Only",
        //             Date = DateTime.UtcNow
        //         };

        //         var result = await _challanService.GenerateChallanAsync(request);

        //         if (result.Success)
        //         {
        //             return Ok(result);
        //         }

        //         return BadRequest(result);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error generating challan via plugin for application {ApplicationId}", applicationId);
        //         return StatusCode(500, "Internal server error occurred while generating challan");
        //     }
        // }
    }
}