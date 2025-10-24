using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Models;
using MyWebApp.ViewModels;
using MyWebApp.Data;


namespace MyWebApp.Api.Services
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JWTService _jwtService;
        private readonly EmailService _emailService;
        private readonly OtpAttemptService _otpService;
        private readonly ILogger<AuthService> _logger;
        private readonly ApplicationDbContext _context;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            JWTService jwtService,
            EmailService emailService,
            OtpAttemptService otpService,
            ILogger<AuthService> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _emailService = emailService;
            _otpService = otpService;
            _logger = logger;
            _context = context;
        }

        public async Task<AuthResponseViewModel> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return new AuthResponseViewModel
                    {
                        Success = false,
                        Message = "User with this email already exists"
                    };
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailAddress = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign default role
                    await _userManager.AddToRoleAsync(user, "User");

                    // Generate and send OTP
                    var otp = MyWebApp.Api.Common.RandomGenerator.RandomOTP(4);
                    await _otpService.CreateOTPAttempt(model.Email, otp);
                    await _emailService.SendEmailAsync(model.Email, "Your Secure Login OTP for PMC",
                        $"Your verification code is: {otp}");

                    return new AuthResponseViewModel
                    {
                        Success = true,
                        Message = "Registration successful. Please verify your email.",
                        Email = user.Email
                    };
                }

                return new AuthResponseViewModel
                {
                    Success = false,
                    Message = "Registration failed: " + string.Join(", ", result.Errors.Select(e => e.Description))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email {Email}", model.Email);
                throw;
            }
        }

        public async Task<AuthResponseViewModel> LoginAsync(LoginViewModel model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return new AuthResponseViewModel
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                if (!user.EmailConfirmed)
                {
                    return new AuthResponseViewModel
                    {
                        Success = false,
                        Message = "Please verify your email first"
                    };
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, true);
                if (result.Succeeded)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault() ?? "User";
                    var token = GenerateJwtToken(user, role);

                    return new AuthResponseViewModel
                    {
                        Success = true,
                        Message = "Login successful",
                        Token = token,
                        Email = user.Email,
                        Role = role
                    };
                }

                if (result.IsLockedOut)
                {
                    return new AuthResponseViewModel
                    {
                        Success = false,
                        Message = "Account is locked. Please try again later."
                    };
                }

                return new AuthResponseViewModel
                {
                    Success = false,
                    Message = "Invalid email or password"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email {Email}", model.Email);
                throw;
            }
        }

        //GenerateJwtToken method logic
        private string GenerateJwtToken(ApplicationUser user, string role)
        {
            return _jwtService.GenerateApplicationToken(user.Id, role);
        }

        public async Task<AuthResponseViewModel> SetPasswordAsync(SetPasswordViewModel model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return new AuthResponseViewModel
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // URL decode the token
                var decodedToken = Uri.UnescapeDataString(model.Token);

                // Reset password using the decoded token
                var resetResult = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
                if (!resetResult.Succeeded)
                {
                    _logger.LogError("Password reset failed for user {Email}. Errors: {Errors}",
                        model.Email,
                        string.Join(", ", resetResult.Errors.Select(e => e.Description)));

                    return new AuthResponseViewModel
                    {
                        Success = false,
                        Message = "Failed to set password: " + string.Join(", ", resetResult.Errors.Select(e => e.Description))
                    };
                }

                // Activate the user
                user.IsActive = true;
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                // Get user role and generate token
                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";

                // Create Officer entry if user has officer role and doesn't exist
                if (role != "User" && !string.IsNullOrEmpty(user.Role))
                {
                    var existingOfficer = await _context.Officers.FirstOrDefaultAsync(o => o.UserId == user.Id);
                    if (existingOfficer == null)
                    {
                        var officer = new Officer
                        {
                            UserId = user.Id,
                            FirstName = model.FirstName ?? string.Empty,
                            LastName = model.LastName ?? string.Empty,
                            KeyLabel = model.KeyLabel ?? string.Empty,
                            PhoneNumber = user.PhoneNumber,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = user.Id
                        };

                        _context.Officers.Add(officer);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Created Officer entry for user {Email} with role {Role}", user.Email, role);
                    }
                }

                var token = GenerateJwtToken(user, role);

                return new AuthResponseViewModel
                {
                    Success = true,
                    Message = "Password set successfully",
                    Token = token,
                    Email = user.Email,
                    Role = role
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting password for email {Email}", model.Email);
                throw;
            }
        }

        public async Task<InviteOfficerResponseViewModel> InviteOfficerAsync(InviteOfficerViewModel model)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return new InviteOfficerResponseViewModel
                    {
                        Success = false,
                        Email = model.Email,
                        Message = "User with this email already exists"
                    };
                }

                // Create user without password
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    EmailAddress = model.Email,
                    Role = model.Role,
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = false // Will be set to true after password setup
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return new InviteOfficerResponseViewModel
                    {
                        Success = false,
                        Email = model.Email,
                        Message = "Failed to create user account: " + string.Join(", ", result.Errors.Select(e => e.Description))
                    };
                }

                // Assign role
                await _userManager.AddToRoleAsync(user, model.Role);

                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                _logger.LogInformation("Generated reset token for user {Email}: {Token}", model.Email, token);

                // Send invitation email with password setup link
                var encodedToken = Uri.EscapeDataString(token);
                var passwordSetupLink = $"https://pmcrms.justservices.in/set-password?email={Uri.EscapeDataString(model.Email)}&token={encodedToken}";
                var emailBody = $@"
                    <h2>Welcome to PMC</h2>
                    <p>You have been invited as an {model.Role}.</p>
                    <p>Please click the link below to set up your password and activate your account:</p>
                    <p><a href='{passwordSetupLink}'>Set up your password</a></p>
                    <p>This link will expire in 24 hours.</p>
                    <p>If you did not request this invitation, please ignore this email.</p>";

                await _emailService.SendEmailAsync(model.Email, "Welcome to PMC - Account Setup", emailBody);

                return new InviteOfficerResponseViewModel
                {
                    Success = true,
                    Email = model.Email,
                    Message = "Invitation sent successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inviting officer with email {Email}", model.Email);
                throw;
            }
        }

        public async Task<AuthResponseViewModel> VerifyOtpAsync(VerifyOtpViewModel model)
        {
            try
            {
                var isValid = await _otpService.VerifyOTPAttempt(model.Email, model.Otp);
                if (!isValid)
                {
                    return new AuthResponseViewModel
                    {
                        Success = false,
                        Message = "Invalid or expired OTP"
                    };
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // return new AuthResponseViewModel
                    // {
                    //     Success = false,
                    //     Message = "User not found"
                    // };

                    //create new user here
                    var newUser = new ApplicationUser
                    {
                        Email = model.Email,
                        EmailAddress = model.Email,
                        UserName = model.Email,
                        EmailConfirmed = true
                    };
                    await _userManager.CreateAsync(newUser);
                    user = newUser;
                }

                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "User";
                var token = GenerateJwtToken(user, role);

                return new AuthResponseViewModel
                {
                    Success = true,
                    Message = "Email verified successfully",
                    Token = token,
                    Email = user.Email,
                    Role = role
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OTP verification for email {Email}", model.Email);
                throw;
            }
        }
    }
}
