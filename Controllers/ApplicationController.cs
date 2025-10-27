using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Api.Services;
using MyWebApp.ViewModels;

namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationController : ControllerBase
    {
        private readonly ApplicationService _applicationService;
        // private readonly ILogger<ApplicationController> _logger;

        public ApplicationController(ApplicationService applicationService)
        {
            _applicationService = applicationService;
            // _logger = logger;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var application = await _applicationService.CreateApplicationAsync(model, userId);

                return Ok(new
                {
                    message = "Application created successfully",
                    applicationId = application.Id,
                    applicationNumber = application.ApplicationNumber
                });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error creating application");
                return StatusCode(500, "An error occurred while creating the application");
            }
        }

        //Get application list endpoint, include view model for filtering and pagination if needed
        [HttpPost("list")]
        // [Authorize]
        public async Task<IActionResult> GetApplications([FromBody] ApplicationFilterViewModel filter)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var applications = await _applicationService.GetApplicationsByUserIdAsync(userId, filter);

                return Ok(new { success = true, data = applications });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error retrieving applications");
                return StatusCode(500, "An error occurred while retrieving applications");
            }
        }

        //Get application details by id
        [HttpGet("{id}")]
        // [Authorize]
        public async Task<IActionResult> GetApplicationById(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var application = await _applicationService.GetApplicationByIdAsync(id, userId);
                if (application == null)
                {
                    return NotFound("Application not found");
                }

                return Ok(new { success = true, data = application });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error retrieving application");
                return StatusCode(500, "An error occurred while retrieving the application");
            }
        }

        [HttpPost("update-stage")]
        [Authorize]
        public async Task<IActionResult> UpdateApplicationStage([FromBody] UpdateApplicationStageViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Set officer ID to current user
                model.OfficerId = userId;

                var result = await _applicationService.UpdateApplicationStageAsync(model);
                if (!result)
                {
                    return NotFound("Application not found");
                }

                return Ok(new
                {
                    success = true,
                    message = "Application stage updated successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating application stage");
            }
        }



        [HttpPost("generate-otp")]
        [Authorize]
        public async Task<IActionResult> GenerateOtpForSignature([FromBody] GenerateOtpViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Set officer ID to current user
                model.OfficerId = userId;

                var otp = await _applicationService.GenerateOtpForSignatureAsync(model);

                return Ok(new
                {
                    success = true,
                    message = "OTP generated successfully",
                    otp = otp // In production, don't return OTP in response - send via SMS/Email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while generating OTP");
            }
        }

        [HttpPost("apply-digital-signature")]
        [Authorize]
        public async Task<IActionResult> ApplyDigitalSignature([FromBody] ApplyDigitalSignatureViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Set officer ID to current user
                model.OfficerId = userId;

                var result = await _applicationService.ApplyDigitalSignatureAsync(model);
                if (!result)
                {
                    return NotFound("Application not found");
                }

                return Ok(new
                {
                    success = true,
                    message = "Digital signature applied successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while applying digital signature");
            }
        }

        [HttpPost("apply-certificate-signature")]
        [Authorize]
        public async Task<IActionResult> ApplyCertificateSignature([FromBody] ApplyCertificateSignatureViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                // Set officer ID to current user
                model.OfficerId = userId;

                // Get current user role to customize response message
                var currentUser = await _applicationService.GetApplicationByIdAsync(model.ApplicationId.ToString(), userId);
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                var result = await _applicationService.ApplyCertificateSignatureAsync(model);
                if (!result)
                {
                    return BadRequest("Failed to apply digital signature on certificate");
                }

                // Customize message based on user role
                var message = userRole switch
                {
                    "ExecutiveEngineer" => "Executive Engineer digital signature applied successfully on certificate. Certificate forwarded to City Engineer for final signature.",
                    "CityEngineer" => "City Engineer digital signature applied successfully on certificate. Application is now APPROVED and complete.",
                    _ => "Digital signature applied successfully on certificate"
                };

                return Ok(new
                {
                    success = true,
                    message = message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while applying digital signature on certificate",
                    error = ex.Message
                });
            }
        }

        [HttpPost("schedule-appointment")]
        [Authorize]
        public async Task<IActionResult> ScheduleAppointment([FromBody] ScheduleAppointmentRequest model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var result = await _applicationService.ScheduleAppointmentAsync(model, userId);
                if (!result)
                {
                    return BadRequest("Failed to schedule appointment");
                }

                return Ok(new
                {
                    success = true,
                    message = "Appointment scheduled successfully. The applicant has been notified via email and the application stage has been updated to Document Verification Pending."
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error scheduling appointment");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while scheduling the appointment",
                    error = ex.Message
                });
            }
        }


    }
}
