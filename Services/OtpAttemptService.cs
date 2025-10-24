using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Api.Services
{
    public class OtpAttemptService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {

        public async Task CreateOTPAttempt(string emailAddress, string otp) 
        {
            var IsPhoneNumberExists = await dbContext.OtpVerifications.Where(x => x.Email == emailAddress).FirstOrDefaultAsync();
            if(IsPhoneNumberExists != null )
            {
                if(IsPhoneNumberExists.RetryCount >= 10 && IsPhoneNumberExists.Otp_Generated_At.AddDays(1) > DateTime.UtcNow)
                {
                    throw new Exception("Account blocked for a day due to mutiple request.");
                }

                if (IsPhoneNumberExists.Otp_Generated_At.AddMinutes(1) > DateTime.UtcNow)
                {

                    IsPhoneNumberExists.RetryCount += 1;
                    IsPhoneNumberExists.Otp = otp;
                }
                else
                {
                    IsPhoneNumberExists.Email = emailAddress;
                    IsPhoneNumberExists.PhoneNumber = emailAddress;
                    IsPhoneNumberExists.Otp = otp;
                    IsPhoneNumberExists.ExpiryTime = DateTime.UtcNow.AddMinutes(10);
                    IsPhoneNumberExists.Otp_Generated_At = DateTime.UtcNow;
                    IsPhoneNumberExists.RetryCount = 1;
                }
                await dbContext.SaveChangesAsync();
            }
            else
            {
                var otpAttempt = new OtpVerification() 
                {
                    Email = emailAddress,
                    PhoneNumber = emailAddress,
                    Otp = otp,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(10),
                    RetryCount = 1,
                    Otp_Generated_At = DateTime.UtcNow
                };
                dbContext.OtpVerifications.Add(otpAttempt);
                await dbContext.SaveChangesAsync();
            }
           
        }

        //VerifyOTPAttempt
        public async Task<bool> VerifyOTPAttempt(string emailAddress, string otp)
        {
            var otpAttempt = await dbContext.OtpVerifications.Where(x => x.Email == emailAddress && x.Otp == otp).FirstOrDefaultAsync();
            if(otpAttempt != null)
            {
                if(otpAttempt.ExpiryTime < DateTime.UtcNow)
                {
                    return false;
                }
                otpAttempt.IsVerified = true;
                otpAttempt.VerifiedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> IsEmailIdExist(string emailAddress)
        {
            var user = await userManager.FindByEmailAsync(emailAddress);
            return user != null;
        }
    }
}