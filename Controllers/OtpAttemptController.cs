using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Api.Services;



namespace MyWebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OtpAttemptController(OtpAttemptService otpAttemptService, EmailService emailService) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        [Route("generate")]
        public async Task<IActionResult> CreateOTPAttempt([FromQuery] string emailAddress)
        {
            if (string.IsNullOrEmpty(emailAddress))
            {
                return BadRequest("Email address is required");
            }

            //await otpAttemptService.IsEmailIdExist(emailAddress);
            var otp = MyWebApp.Api.Common.RandomGenerator.RandomOTP(6);
            await otpAttemptService.CreateOTPAttempt(emailAddress, otp);
            await emailService.SendEmailAsync(emailAddress, "Your OTP Code", $"Your OTP code is: {otp}. It is valid for 10 minutes.");
            return Ok(new { message = $"{otp} OTP sent successfully" });
        }

        // [HttpGet]
        // [Route("verify")]
        // [AllowAnonymous]
        // public async Task<IActionResult> IsOtpValid([FromQuery] string emailAddress, [FromQuery] string otp)
        // {
        //     if (string.IsNullOrEmpty(emailAddress) || string.IsNullOrEmpty(otp))
        //     {
        //         return BadRequest("Email address and OTP are required");
        //     }
        //     return Ok(await otpAttemptService.VerifyOTPAttempt(emailAddress, otp));
        // }
    }
}