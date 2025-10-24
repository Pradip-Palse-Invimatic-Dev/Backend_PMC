using System;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.Models.Enums;
using MyWebApp.ViewModels;
using System.Text;
using System.Text.Json;

namespace MyWebApp.Api.Services
{
    public class ApplicationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ApplicationService> _logger;
        private readonly IMapper _mapper;
        private readonly FileService _fileService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EmailService _emailService;

        public ApplicationService(
            ApplicationDbContext context,
            ILogger<ApplicationService> logger,
            IMapper mapper,
            FileService fileService,
            IHttpClientFactory httpClientFactory,
            EmailService emailService)
        {
            _context = context;
            _logger = logger;
            _mapper = mapper;
            _fileService = fileService;
            _httpClientFactory = httpClientFactory;
            _emailService = emailService;
        }

        public async Task<Application> CreateApplicationAsync(CreateApplicationViewModel model, string userId)
        {
            try
            {
                //get the user by email 
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // Map and create addresses
                var permanentAddress = _mapper.Map<Address>(model.PermanentAddress);
                permanentAddress.CreatedAt = DateTime.UtcNow;
                permanentAddress.CreatedBy = userId;

                var currentAddress = _mapper.Map<Address>(model.CurrentAddress);
                currentAddress.CreatedAt = DateTime.UtcNow;
                currentAddress.CreatedBy = userId;

                // Map and create application
                var application = _mapper.Map<Application>(model);
                application.ApplicationNumber = GenerateApplicationNumber();
                application.ApplicantId = userId;
                application.Status = ApplicationStatus.Submitted;
                application.CurrentStage = ApplicationStage.JUNIOR_ENGINEER_PENDING;
                application.SubmissionDate = DateTime.UtcNow;
                application.CreatedAt = DateTime.UtcNow;
                application.CreatedBy = userId;

                // Save addresses first
                _context.Addresses.Add(permanentAddress);
                _context.Addresses.Add(currentAddress);
                await _context.SaveChangesAsync();

                // Set address references
                application.PermanentAddressId = permanentAddress.Id;
                application.CurrentAddressId = currentAddress.Id;

                // Save application
                _context.Applications.Add(application);
                await _context.SaveChangesAsync();

                // First save qualifications and experiences to get their IDs
                if (model.Qualifications != null)
                {
                    foreach (var qualificationModel in model.Qualifications)
                    {
                        var qualification = _mapper.Map<Qualification>(qualificationModel);
                        qualification.ApplicationId = application.Id;
                        _context.Qualifications.Add(qualification);
                        await _context.SaveChangesAsync();

                        // Link qualification documents if any
                        var qualificationDocs = model.Documents?.Where(d =>
                            (d.DocumentType == DocumentType.QualificationCertificate ||
                             d.DocumentType == DocumentType.QualificationLastYearMarksheet) &&
                            d.FileId == qualificationModel.FileId);

                        if (qualificationDocs != null)
                        {
                            foreach (var docModel in qualificationDocs)
                            {
                                var document = _mapper.Map<Document>(docModel);
                                document.ApplicationId = application.Id;
                                document.QualificationId = qualification.Id;
                                document.UploadDate = DateTime.UtcNow;
                                document.CreatedAt = DateTime.UtcNow;
                                document.CreatedBy = userId;
                                document.IsVerified = false; // Initially false
                                _context.Documents.Add(document);
                            }
                        }
                    }
                }

                // Handle experiences and their documents
                if (model.Experiences != null)
                {
                    foreach (var experienceModel in model.Experiences)
                    {
                        var experience = _mapper.Map<Experience>(experienceModel);
                        experience.ApplicationId = application.Id;
                        _context.Experiences.Add(experience);
                        await _context.SaveChangesAsync();

                        // Link experience documents if any
                        var experienceDocs = model.Documents?.Where(d =>
                            d.DocumentType == DocumentType.ExperienceCertificate &&
                            d.FileId == experienceModel.FileId);

                        if (experienceDocs != null)
                        {
                            foreach (var docModel in experienceDocs)
                            {
                                var document = _mapper.Map<Document>(docModel);
                                document.ApplicationId = application.Id;
                                document.ExperienceId = experience.Id;
                                document.UploadDate = DateTime.UtcNow;
                                document.CreatedAt = DateTime.UtcNow;
                                document.CreatedBy = userId;
                                document.IsVerified = false; // Initially false
                                _context.Documents.Add(document);
                            }
                        }
                    }
                }

                // Handle remaining documents that aren't linked to qualifications or experiences
                if (model.Documents != null)
                {
                    var remainingDocs = model.Documents.Where(d =>
                        d.DocumentType != DocumentType.QualificationCertificate &&
                        d.DocumentType != DocumentType.QualificationLastYearMarksheet &&
                        d.DocumentType != DocumentType.ExperienceCertificate);

                    foreach (var docModel in remainingDocs)
                    {
                        var document = _mapper.Map<Document>(docModel);
                        document.ApplicationId = application.Id;
                        document.UploadDate = DateTime.UtcNow;
                        document.CreatedAt = DateTime.UtcNow;
                        document.CreatedBy = userId;
                        document.IsVerified = false; // Initially false
                        _context.Documents.Add(document);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new application with ID: {ApplicationId}", application.Id);
                return application;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating application for user {UserId}", userId);
                throw;
            }
        }

        private string GenerateApplicationNumber()
        {
            // Format: PMC_APPLICATION_YYYY-XXXXX
            // where XXXXX is a sequential number
            var date = DateTime.UtcNow;
            var lastApplication = _context.Applications
                .Where(a => a.CreatedAt.Year == date.Year)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefault();

            int sequence = 1;
            if (lastApplication != null && lastApplication.ApplicationNumber != null)
            {
                var lastSequence = int.Parse(lastApplication.ApplicationNumber.Split('_').Last());
                sequence = lastSequence + 1;
            }

            return $"PMC_APPLICATION_{date:yyyy}_{sequence}";
        }

        // Helper methods to check user roles
        private static bool IsJuniorRole(string role)
        {
            return role == "JuniorArchitect" ||
                   role == "JuniorLicenceEngineer" ||
                   role == "JuniorStructuralEngineer" ||
                   role == "JuniorSupervisor1" ||
                   role == "JuniorSupervisor2";
        }

        private static bool IsAssistantRole(string role)
        {
            return role == "AssistantArchitect" ||
                   role == "AssistantLicenceEngineer" ||
                   role == "AssistantStructuralEngineer" ||
                   role == "AssistantSupervisor1" ||
                   role == "AssistantSupervisor2";
        }

        private static bool IsOfficerRole(string role)
        {
            return role == "CityEngineer" ||
                   role == "ExecutiveEngineer" ||
                   role == "Clerk" ||
                   IsAssistantRole(role) ||
                   IsJuniorRole(role);
        }

        private static bool CanOfficerViewApplication(string role, ApplicationStage currentStage, PositionType applicationPositionType)
        {
            // Check if role matches the required stage
            bool stageMatches = role switch
              {
                "CityEngineer" => currentStage == ApplicationStage.CITY_ENGINEER_PENDING || currentStage == ApplicationStage.CITY_ENGINEER_SIGN_PENDING,
                "ExecutiveEngineer" => currentStage == ApplicationStage.EXECUTIVE_ENGINEER_PENDING || currentStage == ApplicationStage.EXECUTIVE_ENGINEER_SIGN_PENDING,
                "Clerk" => currentStage == ApplicationStage.CLERK_PENDING,
                _ when IsAssistantRole(role) => currentStage == ApplicationStage.ASSISTANT_ENGINEER_PENDING,
                _ when IsJuniorRole(role) => currentStage == ApplicationStage.JUNIOR_ENGINEER_PENDING || currentStage == ApplicationStage.DOCUMENT_VERIFICATION_PENDING,
                _ => false
            };

            if (!stageMatches) return false;

            // High-level roles can see all applications
            if (role == "CityEngineer" || role == "ExecutiveEngineer" || role == "Clerk" || role == "Admin")
            {
                return true;
            }

            // Junior and Assistant roles can only see applications of their specific category
            return role switch
            {
                "JuniorArchitect" or "AssistantArchitect" => applicationPositionType == PositionType.Architect,
                "JuniorStructuralEngineer" or "AssistantStructuralEngineer" => applicationPositionType == PositionType.StructuralEngineer,
                "JuniorLicenceEngineer" or "AssistantLicenceEngineer" => applicationPositionType == PositionType.LicenceEngineer,
                "JuniorSupervisor1" or "AssistantSupervisor1" => applicationPositionType == PositionType.Supervisor1,
                "JuniorSupervisor2" or "AssistantSupervisor2" => applicationPositionType == PositionType.Supervisor2,
                _ => false
            };
        }

        //GetApplicationsByUserIdAsync based on userId and his role
        public async Task<List<GetApplicationsListViewModel>> GetApplicationsByUserIdAsync(string userId, ApplicationFilterViewModel filter)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                // Debug: Log the enum value
                _logger.LogInformation("JUNIOR_ENGINEER_PENDING enum value: {EnumValue}", (int)ApplicationStage.JUNIOR_ENGINEER_PENDING);
                _logger.LogInformation("User role: {UserRole}", user.Role);

                IQueryable<Application> query = _context.Applications
                    .Include(a => a.PermanentAddress)
                    .Include(a => a.CurrentAddress)
                    .Include(a => a.Documents)
                    .Include(a => a.Applicant);

                // Apply filters first
                if (!string.IsNullOrEmpty(filter.ApplicantName))
                {
                    query = query.Where(a => (a.FirstName + " " + a.LastName).Contains(filter.ApplicantName));
                }
                // if (filter.Status.HasValue)
                // {
                //     query = query.Where(a => a.Status == filter.Status.Value);
                // }
                if (filter.FromDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= filter.FromDate.Value);
                }
                if (filter.ToDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt <= filter.ToDate.Value);
                }

                // Filter applications based on user role and corresponding stages
                if (user.Role == "CityEngineer")
                {
                    query = query.Where(a => a.CurrentStage == ApplicationStage.CITY_ENGINEER_PENDING || a.CurrentStage == ApplicationStage.CITY_ENGINEER_SIGN_PENDING);
                }
                else if (user.Role == "ExecutiveEngineer")
                {
                    query = query.Where(a => a.CurrentStage == ApplicationStage.EXECUTIVE_ENGINEER_PENDING || a.CurrentStage == ApplicationStage.EXECUTIVE_ENGINEER_SIGN_PENDING);
                }
                else if (user.Role == "Clerk")
                {
                    query = query.Where(a => a.CurrentStage == ApplicationStage.CLERK_PENDING);
                }
                else if (IsAssistantRole(user.Role))
                {
                    // Debug: Count applications before stage filtering
                    var totalCount = await query.CountAsync();
                    _logger.LogInformation("Total applications before stage filtering: {Count}", totalCount);

                    query = query.Where(a => a.CurrentStage == ApplicationStage.ASSISTANT_ENGINEER_PENDING);

                    // Debug: Count applications after stage filtering
                    var stageFilteredCount = await query.CountAsync();
                    _logger.LogInformation("Applications after stage filtering (ASSISTANT_ENGINEER_PENDING): {Count}", stageFilteredCount);

                    // Assistant roles can only see applications of their specific category
                    query = user.Role switch
                    {
                        "AssistantArchitect" => query.Where(a => a.PositionType == PositionType.Architect),
                        "AssistantStructuralEngineer" => query.Where(a => a.PositionType == PositionType.StructuralEngineer),
                        "AssistantLicenceEngineer" => query.Where(a => a.PositionType == PositionType.LicenceEngineer),
                        "AssistantSupervisor1" => query.Where(a => a.PositionType == PositionType.Supervisor1),
                        "AssistantSupervisor2" => query.Where(a => a.PositionType == PositionType.Supervisor2),
                        _ => query
                    };

                    // Debug: Count applications after position type filtering
                    var finalCount = await query.CountAsync();
                    _logger.LogInformation("Applications after position type filtering for {Role}: {Count}", user.Role, finalCount);
                }
                else if (IsJuniorRole(user.Role))
                {
                    // Debug: Count applications before stage filtering
                    var totalCount = await query.CountAsync();
                    _logger.LogInformation("Total applications before stage filtering: {Count}", totalCount);

                    query = query.Where(a => a.CurrentStage == ApplicationStage.JUNIOR_ENGINEER_PENDING || a.CurrentStage == ApplicationStage.DOCUMENT_VERIFICATION_PENDING);

                    // Debug: Count applications after stage filtering
                    var stageFilteredCount = await query.CountAsync();
                    _logger.LogInformation("Applications after stage filtering (JUNIOR_ENGINEER_PENDING): {Count}", stageFilteredCount);

                    // Junior roles can only see applications of their specific category
                    query = user.Role switch
                    {
                        "JuniorArchitect" => query.Where(a => a.PositionType == PositionType.Architect),
                        "JuniorStructuralEngineer" => query.Where(a => a.PositionType == PositionType.StructuralEngineer),
                        "JuniorLicenceEngineer" => query.Where(a => a.PositionType == PositionType.LicenceEngineer),
                        "JuniorSupervisor1" => query.Where(a => a.PositionType == PositionType.Supervisor1),
                        "JuniorSupervisor2" => query.Where(a => a.PositionType == PositionType.Supervisor2),
                        _ => query
                    };

                    // Debug: Count applications after position type filtering
                    var finalCount = await query.CountAsync();
                    _logger.LogInformation("Applications after position type filtering for {Role}: {Count}", user.Role, finalCount);
                }

                // If user has officer/admin role, return filtered applications
                if (IsOfficerRole(user.Role))
                {
                    // Apply pagination after all filtering
                    int skip = (filter.PageNumber - 1) * filter.PageSize;
                    query = query.Skip(skip).Take(filter.PageSize);

                    return await query.Select(a => new GetApplicationsListViewModel
                    {
                        Id = a.Id.ToString(),
                        ApplicationNumber = a.ApplicationNumber,
                        FirstName = a.FirstName,
                        MiddleName = a.MiddleName,
                        LastName = a.LastName,
                        PositionType = a.PositionType,
                        SubmissionDate = a.SubmissionDate,
                        Status = a.Status,
                        CurrentStage = a.CurrentStage
                    }).ToListAsync();
                }

                // Otherwise, return only applications created by the user
                query = query.Where(a => a.ApplicantId == userId);

                // Apply pagination for user's own applications
                int userSkip = (filter.PageNumber - 1) * filter.PageSize;
                query = query.Skip(userSkip).Take(filter.PageSize);

                var result = await query.Select(a => new GetApplicationsListViewModel
                {
                    Id = a.Id.ToString(),
                    ApplicationNumber = a.ApplicationNumber,
                    FirstName = a.FirstName,
                    MiddleName = a.MiddleName,
                    LastName = a.LastName,
                    PositionType = a.PositionType,
                    SubmissionDate = a.SubmissionDate,
                    Status = a.Status,
                    CurrentStage = a.CurrentStage
                }).ToListAsync();
                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving applications for user {UserId}", userId);
                throw;
            }
        }

        //GetApplicationByIdAsync
        public async Task<GetApplicationViewModel?> GetApplicationByIdAsync(string applicationId, string userId)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var application = await _context.Applications
                    .Include(a => a.PermanentAddress)
                    .Include(a => a.CurrentAddress)
                    .Include(a => a.Documents)
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == new Guid(applicationId));

                if (application == null)
                {
                    return null;
                }

                // Check if user has permission to view this application
                bool canView = false;

                // Application owner can always view
                if (application.ApplicantId == userId)
                {
                    canView = true;
                }
                // Officers can view applications in their respective stages
                else if (IsOfficerRole(user.Role))
                {
                    canView = CanOfficerViewApplication(user.Role, application.CurrentStage, application.PositionType);
                }

                if (!canView)
                {
                    throw new UnauthorizedAccessException("You do not have permission to view this application");
                }

                var result = _mapper.Map<GetApplicationViewModel>(application);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving application {ApplicationId} for user {UserId}", applicationId, userId);
                throw;
            }
        }

        public async Task<bool> UpdateApplicationStageAsync(UpdateApplicationStageViewModel model)
        {
            try
            {
                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == model.ApplicationId);

                if (application == null)
                {
                    return false;
                }

                // Get the officer to verify permissions
                var officer = await _context.Officers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserId == model.OfficerId);

                if (officer?.User == null)
                {
                    throw new UnauthorizedAccessException("Officer not found");
                }

                // Verify the officer can handle this specific position type
                if (!CanOfficerViewApplication(officer.User.Role, application.CurrentStage, application.PositionType))
                {
                    throw new UnauthorizedAccessException("You do not have permission to update this application type");
                }

                // Validate stage progression
                if (!IsValidStageTransition(application.CurrentStage, model.NewStage))
                {
                    throw new InvalidOperationException($"Invalid stage transition from {application.CurrentStage} to {model.NewStage}");
                }

                var previousStage = application.CurrentStage;

                // Update application stage
                application.CurrentStage = model.NewStage;
                application.LastModifiedAt = DateTime.UtcNow;
                application.LastModifiedBy = model.OfficerId;

                // Update status based on new stage
                if (model.NewStage == ApplicationStage.REJECTED)
                {
                    application.Status = ApplicationStatus.Rejected;
                }
                else if (model.NewStage == ApplicationStage.APPROVED)
                {
                    application.Status = ApplicationStatus.Completed;
                }
                else if (model.NewStage == ApplicationStage.DOCUMENT_VERIFICATION_PENDING)
                {
                    application.Status = ApplicationStatus.DocumentVerificationPending;
                }
                else if (model.NewStage == ApplicationStage.ASSISTANT_ENGINEER_PENDING)
                {
                    application.Status = ApplicationStatus.AssistantEngineerApproved;
                }
                else if (model.NewStage == ApplicationStage.EXECUTIVE_ENGINEER_PENDING)
                {
                    application.Status = ApplicationStatus.ExecutiveEngineerApproved;
                }
                else if (model.NewStage == ApplicationStage.CITY_ENGINEER_PENDING)
                {
                    application.Status = ApplicationStatus.CityEngineerApproved;
                }
                else if (model.NewStage == ApplicationStage.PAYMENT_PENDING)
                {
                    application.Status = ApplicationStatus.PaymentPending;
                }
                else if (model.NewStage == ApplicationStage.CLERK_PENDING)
                {
                    application.Status = ApplicationStatus.ClerkApproved;
                }

                _context.Applications.Update(application);
                await _context.SaveChangesAsync();

                // Send email notification to applicant about stage update
                if (application.Applicant != null)
                {
                    await SendApplicationStageUpdateEmailAsync(application, previousStage, model.NewStage, officer.User.Role);
                }

                _logger.LogInformation("Updated application {ApplicationId} stage from {PreviousStage} to {NewStage} by officer {OfficerId}",
                    model.ApplicationId, previousStage, model.NewStage, model.OfficerId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application stage for application {ApplicationId}", model.ApplicationId);
                throw;
            }
        }



        public async Task<string> GenerateOtpForSignatureAsync(GenerateOtpViewModel model)
        {
            try
            {
                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == model.ApplicationId);

                if (application == null)
                {
                    throw new Exception("Application not found");
                }

                // // Get the officer with KeyLabel
                // var officer = await _context.Users
                //     .FirstOrDefaultAsync(o => o.Id == model.OfficerId);

                var officer = await _context.Officers
                    .FirstOrDefaultAsync(o => o.UserId == model.OfficerId);

                if (officer == null)
                {
                    throw new Exception("Officer not found");
                }

                if (string.IsNullOrEmpty(officer.KeyLabel))
                {
                    throw new Exception("Officer KeyLabel not found. Please contact administrator.");
                }

                // Determine assignee based on application position type
                string assignee = GetAssigneeByPosition(application.PositionType);

                _logger.LogInformation("Position: {PositionType}, Assignee: {Assignee}, KeyLabel: {KeyLabel}",
                    application.PositionType, assignee, officer.KeyLabel);

                // Call HSM API for OTP generation
                var hsmResponse = await CallHsmGenerateOtp(application.Id, officer.KeyLabel);

                if (string.IsNullOrEmpty(hsmResponse))
                {
                    throw new Exception("Failed to generate OTP from HSM service");
                }

                // Store OTP verification record (we don't store the actual OTP from HSM)
                var otpVerification = new OtpVerification
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = model.ApplicationId,
                    Otp = "HSM_GENERATED", // We don't store the actual OTP for security
                    Purpose = "DIGITAL_SIGNATURE",
                    ExpiryTime = DateTime.UtcNow.AddMinutes(10),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = model.OfficerId
                };

                _context.OtpVerifications.Add(otpVerification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated OTP for digital signature for application {ApplicationId} using KeyLabel {KeyLabel}",
                    model.ApplicationId, officer.KeyLabel);

                return hsmResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for application {ApplicationId}", model.ApplicationId);
                throw;
            }
        }

        private string GetAssigneeByPosition(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => "JRENGG-ARCH",
                PositionType.LicenceEngineer => "JRENGG-LICE",
                PositionType.StructuralEngineer => "JRENGG-STRU",
                PositionType.Supervisor1 => "JRENGG-SUPER1",
                PositionType.Supervisor2 => "JRENGG-SUPER2",
                _ => "JRENGG-ARCH" // Default fallback
            };
        }

        private async Task<string> CallHsmGenerateOtp(Guid applicationId, string keyLabel)
        {
            try
            {
                var requestBody = new
                {
                    otptype = "single",
                    ptno = "1",
                    txn = applicationId.ToString(),
                    klabel = keyLabel
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("HSM OTP Request: {RequestBody}", jsonContent);

                // Make HTTP call to HSM OTP service
                using var httpClient = _httpClientFactory.CreateClient("HSM_OTP");
                var response = await httpClient.PostAsync("HSM/GenOtp", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("HSM OTP Response: {Response}", responseContent);
                    return responseContent;
                }
                else
                {
                    _logger.LogError("HSM API call failed with status: {StatusCode}", response.StatusCode);
                    throw new Exception($"HSM API call failed with status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling HSM API for OTP generation");
                throw;
            }
        }

        public async Task<bool> ApplyDigitalSignatureAsync(ApplyDigitalSignatureViewModel model)
        {
            try
            {
                // Get application with related data
                var application = await _context.Applications
                    .Include(a => a.PermanentAddress)
                    .Include(a => a.CurrentAddress)
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == model.ApplicationId);

                if (application == null)
                {
                    return false;
                }

                // Get officer for KeyLabel and user role
                var officer = await _context.Officers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserId == model.OfficerId);

                if (officer == null || string.IsNullOrEmpty(officer.KeyLabel))
                {
                    throw new Exception("Officer KeyLabel not found");
                }

                if (officer.User == null || string.IsNullOrEmpty(officer.User.Role))
                {
                    throw new Exception("Officer role not found");
                }

                // Get the role/position mapping for stage progression
                string nextAssigneeRole = GetNextAssigneeRole(application.PositionType);

                // Prepare address string (using current address if available, else permanent)
                var addressToUse = application.CurrentAddress ?? application.PermanentAddress;
                var address = $"{addressToUse?.AddressLine1}, {addressToUse?.AddressLine2}, {addressToUse?.City}, {addressToUse?.State}, {addressToUse?.PinCode}, {addressToUse?.Country}";

                // Read PDF file (assuming it's stored as RecommendedFormPath)
                if (string.IsNullOrEmpty(application.RecommendedFormPath))
                {
                    throw new Exception("Recommended form PDF not found");
                }

                var pdfBytes = await _fileService.ReadFileAsync(application.RecommendedFormPath);
                var pdfBase64 = Convert.ToBase64String(pdfBytes);

                // Prepare HSM signature parameters
                string transaction = application.Id.ToString();
                string keyLabel = officer.KeyLabel;
                string coordinates = GetSignatureCoordinatesByRole(officer.User.Role); // Role-specific coordinates
                string otp = model.Otp;

                // Create SOAP envelope for HSM call
                var soapEnvelope = $@"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
<s:Body xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
<signPdf xmlns=""http://ds.ws.emas/"">
<arg0 xmlns="""">{transaction}</arg0>
<arg1 xmlns="""">{keyLabel}</arg1>
<arg2 xmlns="""">{pdfBase64}</arg2>
<arg3 xmlns=""""/>
<arg4 xmlns="""">{coordinates}</arg4>
<arg5 xmlns="""">last</arg5>
<arg6 xmlns=""""/>
<arg7 xmlns=""""/>
<arg8 xmlns="""">True</arg8>
<arg9 xmlns="""">{otp}</arg9>
<arg10 xmlns="""">single</arg10>
<arg11 xmlns=""""/><arg12 xmlns=""""/>
</signPdf>
</s:Body>
</s:Envelope>";

                // Call HSM service for digital signature
                var content = new StringContent(soapEnvelope, Encoding.UTF8, "application/xml");
                using var httpClient = _httpClientFactory.CreateClient("HSM_SIGN");
                var response = await httpClient.PostAsync("services/dsverifyWS", content);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"HSM signature API call failed with status: {response.StatusCode}");
                }

                var result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("HSM Signature Response: {Response}", result);

                // Check if signature was successful
                if (result.Contains($"{transaction}~FAILURE~failure"))
                {
                    _logger.LogError("HSM signature failed: {Result}", result);
                    throw new Exception("Digital signature failed. Please verify OTP and try again.");
                }

                // Process successful signature response
                var processedResult = result
                    .Replace("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">", "")
                    .Replace("<soap:Body><ns2:signPdfResponse xmlns:ns2=\"http://ds.ws.emas/\">", "")
                    .Replace($"<return>{transaction}~SUCCESS~", "")
                    .Replace("</return></ns2:signPdfResponse></soap:Body></soap:Envelope>", "");

                // Convert back to PDF bytes and save
                var signedPdfBytes = Convert.FromBase64String(processedResult);
                var signedFileName = $"{application.Id}.pdf";
                var signedFilePath = await _fileService.SaveFileAsync(signedFileName, signedPdfBytes);

                // Update application with signed PDF
                application.RecommendedFormPath = signedFilePath;
                application.IsDigitallySigned = true;
                application.DigitalSignatureDate = DateTime.UtcNow;
                application.SignedBy = model.OfficerId;
                application.LastModifiedAt = DateTime.UtcNow;
                application.LastModifiedBy = model.OfficerId;

                // Move to next stage and assign to next role
                application.CurrentStage = GetNextStage(application.CurrentStage);

                _context.Applications.Update(application);
                await _context.SaveChangesAsync();

                // Get next assignee officers and create tasks
                var nextAssigneeOfficers = await GetOfficersByRole(nextAssigneeRole);

                if (nextAssigneeOfficers.Any())
                {
                    // Create tasks for next stage officers
                    foreach (var nextOfficer in nextAssigneeOfficers)
                    {
                        // In a real implementation, you'd create actual tasks
                        _logger.LogInformation("Task assigned to officer {OfficerId} for application {ApplicationId}",
                            nextOfficer.Id, application.Id);
                    }

                    // Send notification email to applicant
                    await SendStageUpdateNotificationAsync(application, "Assistant Engineer");
                }

                // // Generate certificate if this is the EXECUTIVE_ENGINEER_SIGN_PENDING stage
                // if (application.CurrentStage == ApplicationStage.EXECUTIVE_ENGINEER_SIGN_PENDING)
                // {
                //     await GenerateApplicationCertificateAsync(application);
                // }

                _logger.LogInformation("Applied digital signature for application {ApplicationId} by officer {OfficerId}",
                    model.ApplicationId, model.OfficerId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying digital signature for application {ApplicationId}", model.ApplicationId);
                throw;
            }
        }

        private async Task<string> CallHsmApplySignature(Guid applicationId, string keyLabel, string otp)
        {
            try
            {
                var requestBody = new
                {
                    txn = applicationId.ToString(),
                    klabel = keyLabel,
                    otp = otp,
                    action = "apply_signature"
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("HSM Apply Signature Request: {RequestBody}", jsonContent);

                // Make HTTP call to HSM service using HSM_SIGN client
                var httpClient = _httpClientFactory.CreateClient("HSM_SIGN");
                var response = await httpClient.PostAsync("HSM/ApplySignature", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("HSM Apply Signature Response: {Response}", responseContent);
                    return responseContent;
                }
                else
                {
                    _logger.LogError("HSM Apply Signature API call failed with status: {StatusCode}", response.StatusCode);
                    throw new Exception($"HSM API call failed with status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling HSM API for signature application");
                throw;
            }
        }



        private string GetNextAssigneeRole(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => "ASSIENGG-ARCH",
                PositionType.LicenceEngineer => "ASSIENGG-LICE",
                PositionType.StructuralEngineer => "ASSIENGG-STRU",
                PositionType.Supervisor1 => "ASSIENGG-SUPER1",
                PositionType.Supervisor2 => "ASSIENGG-SUPER2",
                _ => "ASSIENGG-ARCH"
            };
        }

        private ApplicationStage GetNextStage(ApplicationStage currentStage)
        {
            return currentStage switch
            {
                ApplicationStage.JUNIOR_ENGINEER_PENDING => ApplicationStage.ASSISTANT_ENGINEER_PENDING,
                ApplicationStage.ASSISTANT_ENGINEER_PENDING => ApplicationStage.EXECUTIVE_ENGINEER_PENDING,
                ApplicationStage.EXECUTIVE_ENGINEER_PENDING => ApplicationStage.CITY_ENGINEER_PENDING,
                ApplicationStage.CITY_ENGINEER_PENDING => ApplicationStage.PAYMENT_PENDING,
                ApplicationStage.PAYMENT_PENDING => ApplicationStage.CLERK_PENDING,
                ApplicationStage.CLERK_PENDING => ApplicationStage.EXECUTIVE_ENGINEER_SIGN_PENDING,
                ApplicationStage.EXECUTIVE_ENGINEER_SIGN_PENDING => ApplicationStage.CITY_ENGINEER_SIGN_PENDING,
                ApplicationStage.CITY_ENGINEER_SIGN_PENDING => ApplicationStage.APPROVED,
                _ => currentStage
            };
        }

        private string GetSignatureCoordinatesByRole(string role)
        {
            // Define signature coordinates for each role based on the PDF form layout
            // Format: "x1,y1,x2,y2" - defining rectangle bounds for signature placement
            return role switch
            {
                // Junior Engineer roles - First signature position
                "JuniorArchitect" or "JuniorLicenceEngineer" or "JuniorStructuralEngineer" or
                "JuniorSupervisor1" or "JuniorSupervisor2" => "117,383,236,324", // Position 1

                // Assistant Engineer roles - Second signature position  
                "AssistantArchitect" or "AssistantLicenceEngineer" or "AssistantStructuralEngineer" or
                "AssistantSupervisor1" or "AssistantSupervisor2" => "300,383,419,324", // Position 2

                // Executive Engineer - Third signature position
                "ExecutiveEngineer" => "117,300,236,241", // Position 3

                // City Engineer - Fourth signature position
                "CityEngineer" => "300,300,419,241", // Position 4

                // Default fallback
                _ => "117,383,236,324"
            };
        }

        private async Task<List<Officer>> GetOfficersByRole(string role)
        {
            // Join Officers with ApplicationUser to get role information
            return await _context.Officers
                .Include(o => o.User)
                .Where(o => o.User != null && o.User.Role == role)
                .ToListAsync();
        }

        private Task SendStageUpdateNotificationAsync(Application application, string stageName)
        {
            try
            {
                if (application.Applicant == null) return Task.CompletedTask;

                var message = $"Your application is currently under review by the {stageName}. We will notify you once their review is complete.";
                var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head></head>
<body style=""font-family: 'Poppins', sans-serif; background-color: #FFF;"">
    <div style=""display: flex; padding: 30px 10px; justify-content: center;"">
        <div style=""background-color: #FCFCFC; max-width: 600px; width: 100%; box-shadow: 0px 4px 4px #0000003D;"">
            <div style=""padding: 30px;"">
                <div style=""padding: 20px 20px 10px 20px;"">
                    <p style=""color: #000; font-weight: 500; margin-bottom: 30px;"">Dear {application.FirstName} {application.LastName}</p>
                    <p style=""margin-bottom: 20px;"">{message}</p>
                    <p style=""line-height: 22px; margin-bottom: 0;"">Regards,<br />PMCRMS</p>
                </div>
            </div>
        </div>
    </div>
</body>
</html>";

                // In real implementation, you'd send email using EmailService
                _logger.LogInformation("Email notification would be sent to {Email} for application {ApplicationId}",
                    application.Applicant.EmailAddress, application.Id);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending stage update notification for application {ApplicationId}", application.Id);
                return Task.CompletedTask;
            }
        }

        // private async Task GenerateApplicationCertificateAsync(Application application)
        // {
        //     try
        //     {
        //         // Generate certificate number if not already generated
        //         if (string.IsNullOrEmpty(application.CertificateNumber))
        //         {
        //             var certificateNumber = await GenerateNewCertificateNumber(application.PositionType);
        //             application.CertificateNumber = certificateNumber;
        //         }

        //         // Update application with certificate details
        //         application.CertificateGeneratedDate = DateTime.UtcNow;
        //         application.IsCertificateGenerated = true;

        //         _context.Applications.Update(application);
        //         await _context.SaveChangesAsync();

        //         _logger.LogInformation("Generated certificate number {CertificateNumber} for application {ApplicationId}. " +
        //             "Certificate PDF can be generated using the Certificate API endpoint.",
        //             application.CertificateNumber, application.Id);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error generating certificate for application {ApplicationId}", application.Id);
        //     }
        // }
        // private async Task<string> GenerateNewCertificateNumber(PositionType positionType)
        // {
        //     var currentYear = DateTime.Now.Year;
        //     var endYear = currentYear + 2;
        //     var yearRange = $"{currentYear}-{endYear}";

        //     // Get the pattern based on position type
        //     var pattern = positionType switch
        //     {
        //         PositionType.Architect => $"PMC/ARCH./{{{{number}}}}/{yearRange}",
        //         PositionType.LicenceEngineer => $"PMC/LIC.ENGG/{{{{number}}}}/{yearRange}",
        //         PositionType.StructuralEngineer => $"PMC/STR.ENGG/{{{{number}}}}/{yearRange}",
        //         PositionType.Supervisor1 => $"PMC/SUP1/{{{{number}}}}/{yearRange}",
        //         PositionType.Supervisor2 => $"PMC/SUP2/{{{{number}}}}/{yearRange}",
        //         _ => $"PMC/CERT/{{{{number}}}}/{yearRange}"
        //     };

        //     // Get the next sequence number
        //     var lastCertificate = await _context.Applications
        //         .Where(a => a.CertificateNumber != null &&
        //                    a.CertificateGeneratedDate.HasValue &&
        //                    a.CertificateGeneratedDate.Value.Year == currentYear &&
        //                    a.PositionType == positionType)
        //         .OrderByDescending(a => a.CertificateGeneratedDate)
        //         .FirstOrDefaultAsync();

        //     int sequence = 1;
        //     if (lastCertificate != null && lastCertificate.CertificateNumber != null)
        //     {
        //         // Extract sequence number from certificate number
        //         var parts = lastCertificate.CertificateNumber.Split('/');
        //         if (parts.Length > 2 && int.TryParse(parts[2], out int lastSequence))
        //         {
        //             sequence = lastSequence + 1;
        //         }
        //     }

        //     return pattern.Replace("{{number}}", sequence.ToString("D4"));
        // }

        public async Task<bool> ScheduleAppointmentAsync(ScheduleAppointmentRequest request, string officerId)
        {
            try
            {
                // Get the application and verify it exists
                var application = await _context.Applications
                    .Include(a => a.Applicant)
                    .FirstOrDefaultAsync(a => a.Id == request.ApplicationId);

                if (application == null)
                {
                    throw new Exception("Application not found");
                }

                // Verify the application is in the correct stage for appointment scheduling
                if (application.CurrentStage != ApplicationStage.JUNIOR_ENGINEER_PENDING)
                {
                    throw new Exception($"Application is not in the correct stage for appointment scheduling. Current stage: {application.CurrentStage}");
                }

                // Verify the officer has permission to schedule appointments for this application
                var officer = await _context.Officers
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.UserId == officerId);

                if (officer?.User == null || !IsJuniorRole(officer.User.Role))
                {
                    throw new UnauthorizedAccessException("Only Junior Engineers can schedule appointments");
                }

                // Verify the officer can handle this specific position type
                if (!CanOfficerViewApplication(officer.User.Role, application.CurrentStage, application.PositionType))
                {
                    throw new UnauthorizedAccessException("You do not have permission to schedule appointments for this application type");
                }

                // Create the appointment
                var appointment = new Appointment
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = request.ApplicationId,
                    AppointmentDate = request.ReviewDate,
                    Status = AppointmentStatus.Scheduled,
                    Comments = request.Comments,
                    ContactPerson = request.ContactPerson,
                    Place = request.Place,
                    RoomNumber = request.RoomNumber,
                    ScheduledByOfficerId = officer.Id, // Use the Officer.Id, not the User.Id
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = officerId
                };

                _context.Appointments.Add(appointment);

                // Update application stage from JUNIOR_ENGINEER_PENDING to DOCUMENT_VERIFICATION_PENDING
                application.CurrentStage = ApplicationStage.DOCUMENT_VERIFICATION_PENDING;
                application.Status = ApplicationStatus.AppointmentScheduled;
                application.LastModifiedAt = DateTime.UtcNow;
                application.LastModifiedBy = officerId;

                _context.Applications.Update(application);
                await _context.SaveChangesAsync();

                // Send email notification to the applicant
                if (application.Applicant != null)
                {
                    await SendAppointmentNotificationEmailAsync(application, appointment);
                }

                _logger.LogInformation("Appointment scheduled successfully for application {ApplicationId} by officer {OfficerId}. " +
                    "Application stage updated from JUNIOR_ENGINEER_PENDING to DOCUMENT_VERIFICATION_PENDING",
                    request.ApplicationId, officerId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling appointment for application {ApplicationId} by officer {OfficerId}",
                    request.ApplicationId, officerId);
                throw;
            }
        }

        private async Task SendAppointmentNotificationEmailAsync(Application application, Appointment appointment)
        {
            try
            {
                if (application.Applicant == null) return;

                var appointmentDateFormatted = appointment.AppointmentDate.ToString("dd/MM/yyyy 'at' hh:mm tt");

                var emailSubject = $"Appointment Scheduled - Application {application.ApplicationNumber}";
                var emailBody = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Appointment Scheduled</title>
    <style>
        body {{ font-family: 'Arial', sans-serif; background-color: #f8f9fa; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; }}
        .content {{ padding: 30px; }}
        .appointment-details {{ background-color: #f8f9fa; border-left: 4px solid #007bff; padding: 15px; margin: 20px 0; }}
        .footer {{ background-color: #6c757d; color: white; padding: 15px; text-align: center; font-size: 12px; }}
        .important {{ color: #dc3545; font-weight: bold; }}
        .success {{ color: #28a745; font-weight: bold; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Appointment Scheduled</h1>
            <p>PMCRMS - Pune Municipal Corporation</p>
        </div>
        <div class=""content"">
            <p>Dear <strong>{application.FirstName} {application.LastName}</strong>,</p>
            
            <p>We are pleased to inform you that an appointment has been scheduled for the document verification of your application <strong>{application.ApplicationNumber}</strong>.</p>
            
            <div class=""appointment-details"">
                <h3>Appointment Details:</h3>
                <p><strong>Date & Time:</strong> {appointmentDateFormatted}</p>
                <p><strong>Venue:</strong> {appointment.Place}</p>
                <p><strong>Room Number:</strong> {appointment.RoomNumber}</p>
                <p><strong>Contact Person:</strong> {appointment.ContactPerson}</p>
                {(!string.IsNullOrEmpty(appointment.Comments) ? $"<p><strong>Additional Instructions:</strong> {appointment.Comments}</p>" : "")}
            </div>
            
            <div class=""important"">
                <h4>Important Instructions:</h4>
                <ul>
                    <li>Please bring all original documents along with photocopies for verification</li>
                    <li>Arrive 15 minutes before the scheduled appointment time</li>
                    <li>Carry a valid photo ID proof</li>
                    <li>In case you need to reschedule, please contact us at least 24 hours in advance</li>
                </ul>
            </div>
            
            <p>Your application is now at the <span class=""success"">Document Verification</span> stage. Once the document verification is completed successfully, your application will proceed to the next stage.</p>
            
            <p>Thank you for choosing PMCRMS services.</p>
            
            <p>Best regards,<br />
            <strong>Pune Municipal Corporation</strong><br />
            Registration Management System</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 Pune Municipal Corporation. All rights reserved.</p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";

                // Send the appointment notification email
                await _emailService.SendEmailAsync(application.Applicant.EmailAddress, emailSubject, emailBody);

                _logger.LogInformation("Appointment notification email sent successfully to {Email} for application {ApplicationId}",
                    application.Applicant.EmailAddress, application.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending appointment notification email for application {ApplicationId}",
                    application.Id);
                // Don't throw here as appointment creation should not fail due to email issues
            }
        }

        private async Task SendApplicationStageUpdateEmailAsync(Application application, ApplicationStage previousStage, ApplicationStage newStage, string officerRole)
        {
            try
            {
                if (application.Applicant == null) return;

                var (stageDisplayName, statusMessage, nextSteps) = GetStageDisplayInfo(newStage);
                var (previousStageDisplayName, _, _) = GetStageDisplayInfo(previousStage);

                var emailSubject = $"Application Status Update - {application.ApplicationNumber}";
                var emailBody = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Application Status Update</title>
    <style>
        body {{ font-family: 'Arial', sans-serif; background-color: #f8f9fa; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1); }}
        .header {{ background-color: #28a745; color: white; padding: 20px; text-align: center; }}
        .header.rejected {{ background-color: #dc3545; }}
        .content {{ padding: 30px; }}
        .status-update {{ background-color: #f8f9fa; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0; }}
        .status-update.rejected {{ border-left-color: #dc3545; }}
        .footer {{ background-color: #6c757d; color: white; padding: 15px; text-align: center; font-size: 12px; }}
        .success {{ color: #28a745; font-weight: bold; }}
        .warning {{ color: #ffc107; font-weight: bold; }}
        .danger {{ color: #dc3545; font-weight: bold; }}
        .info {{ color: #17a2b8; font-weight: bold; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header {(newStage == ApplicationStage.REJECTED ? "rejected" : "")}"">
            <h1>Application Status Update</h1>
            <p>PMCRMS - Pune Municipal Corporation</p>
        </div>
        <div class=""content"">
            <p>Dear <strong>{application.FirstName} {application.LastName}</strong>,</p>
            
            <p>We would like to inform you about an important update regarding your application <strong>{application.ApplicationNumber}</strong>.</p>
            
            <div class=""status-update {(newStage == ApplicationStage.REJECTED ? "rejected" : "")}"">
                <h3>Status Update:</h3>
                <p><strong>Previous Stage:</strong> {previousStageDisplayName}</p>
                <p><strong>Current Stage:</strong> <span class=""{GetStatusClass(newStage)}"">{stageDisplayName}</span></p>
                <p><strong>Updated By:</strong> {GetOfficerDisplayName(officerRole)}</p>
                <p><strong>Update Time:</strong> {DateTime.UtcNow:dd/MM/yyyy 'at' hh:mm tt}</p>
            </div>
            
            <div>
                <h4>Status Information:</h4>
                <p>{statusMessage}</p>
                
                {(!string.IsNullOrEmpty(nextSteps) ? $@"
                <h4>Next Steps:</h4>
                <p>{nextSteps}</p>
                " : "")}
            </div>
            
            {(newStage == ApplicationStage.REJECTED ? @"
            <div class=""status-update rejected"">
                <h4>Important Notice:</h4>
                <p>If you believe this decision was made in error or if you have additional information to provide, please contact our office within 15 days of receiving this notification.</p>
            </div>
            " : "")}
            
            <p>You can track your application progress by logging into your PMCRMS account or contacting our support team.</p>
            
            <p>Thank you for using PMCRMS services.</p>
            
            <p>Best regards,<br />
            <strong>Pune Municipal Corporation</strong><br />
            Registration Management System</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 Pune Municipal Corporation. All rights reserved.</p>
            <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";

                // Send the stage update notification email
                await _emailService.SendEmailAsync(application.Applicant.EmailAddress, emailSubject, emailBody);

                _logger.LogInformation("Application stage update email sent successfully to {Email} for application {ApplicationId}. Stage: {PreviousStage} -> {NewStage}",
                    application.Applicant.EmailAddress, application.Id, previousStage, newStage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending application stage update email for application {ApplicationId}",
                    application.Id);
                // Don't throw here as stage update should not fail due to email issues
            }
        }

        private (string displayName, string statusMessage, string nextSteps) GetStageDisplayInfo(ApplicationStage stage)
        {
            return stage switch
            {
                ApplicationStage.JUNIOR_ENGINEER_PENDING =>
                    ("Junior Engineer Review", "Your application is currently being reviewed by our Junior Engineer team.", "Please wait for the review to be completed. You may be contacted for document verification."),

                ApplicationStage.DOCUMENT_VERIFICATION_PENDING =>
                    ("Document Verification", "Your application is scheduled for document verification.", "Please attend the scheduled appointment with all required documents."),

                ApplicationStage.ASSISTANT_ENGINEER_PENDING =>
                    ("Assistant Engineer Review", "Your application has been forwarded to our Assistant Engineer for technical review.", "The technical aspects of your application are being evaluated. This process may take 3-5 business days."),

                ApplicationStage.EXECUTIVE_ENGINEER_PENDING =>
                    ("Executive Engineer Review", "Your application is now under review by our Executive Engineer.", "The application is undergoing senior-level technical evaluation. Please allow 5-7 business days for this process."),

                ApplicationStage.CITY_ENGINEER_PENDING =>
                    ("City Engineer Review", "Your application has reached the City Engineer for final technical approval.", "This is the final technical review stage. The process typically takes 5-10 business days."),

                ApplicationStage.PAYMENT_PENDING =>
                    ("Payment Required", "Your application has been approved and payment is now required.", "Please make the required payment to proceed with certificate generation. Payment details will be provided separately."),

                ApplicationStage.CLERK_PENDING =>
                    ("Administrative Processing", "Your payment has been received and the application is being processed administratively.", "The administrative formalities are being completed. Your certificate will be prepared soon."),

                ApplicationStage.EXECUTIVE_ENGINEER_SIGN_PENDING =>
                    ("Executive Engineer Signature", "Your certificate is awaiting digital signature from the Executive Engineer.", "The certificate is in the final signing process. This typically takes 2-3 business days."),

                ApplicationStage.CITY_ENGINEER_SIGN_PENDING =>
                    ("City Engineer Signature", "Your certificate is awaiting final digital signature from the City Engineer.", "This is the final step. Your certificate will be ready within 2-3 business days."),

                ApplicationStage.APPROVED =>
                    ("Application Approved", "Congratulations! Your application has been successfully approved.", "Your certificate is now ready. You can download it from your account or collect it from our office."),

                ApplicationStage.REJECTED =>
                    ("Application Rejected", "Unfortunately, your application has been rejected.", "Please review the rejection reasons and contact our office if you need clarification or wish to appeal this decision."),

                _ => ("Unknown Stage", "Your application is being processed.", "Please contact our support team for more information.")
            };
        }

        private string GetStatusClass(ApplicationStage stage)
        {
            return stage switch
            {
                ApplicationStage.APPROVED => "success",
                ApplicationStage.REJECTED => "danger",
                ApplicationStage.PAYMENT_PENDING => "warning",
                _ => "info"
            };
        }

        private string GetOfficerDisplayName(string role)
        {
            return role switch
            {
                "JuniorArchitect" => "Junior Architect",
                "JuniorLicenceEngineer" => "Junior Licence Engineer",
                "JuniorStructuralEngineer" => "Junior Structural Engineer",
                "JuniorSupervisor1" => "Junior Supervisor (Category 1)",
                "JuniorSupervisor2" => "Junior Supervisor (Category 2)",
                "AssistantArchitect" => "Assistant Architect",
                "AssistantLicenceEngineer" => "Assistant Licence Engineer",
                "AssistantStructuralEngineer" => "Assistant Structural Engineer",
                "AssistantSupervisor1" => "Assistant Supervisor (Category 1)",
                "AssistantSupervisor2" => "Assistant Supervisor (Category 2)",
                "ExecutiveEngineer" => "Executive Engineer",
                "CityEngineer" => "City Engineer",
                "Clerk" => "Administrative Officer",
                _ => "PMC Officer"
            };
        }

        private bool IsValidStageTransition(ApplicationStage currentStage, ApplicationStage newStage)
        {
            // Define valid stage transitions based on the correct sequence
            var validTransitions = new Dictionary<ApplicationStage, List<ApplicationStage>>
            {
                [ApplicationStage.JUNIOR_ENGINEER_PENDING] = new List<ApplicationStage>
                {
                    ApplicationStage.DOCUMENT_VERIFICATION_PENDING,
                    ApplicationStage.REJECTED
                },
                [ApplicationStage.DOCUMENT_VERIFICATION_PENDING] = new List<ApplicationStage>
                {
                    ApplicationStage.ASSISTANT_ENGINEER_PENDING,
                    ApplicationStage.REJECTED
                },
                [ApplicationStage.ASSISTANT_ENGINEER_PENDING] = new List<ApplicationStage>
                {
                    ApplicationStage.EXECUTIVE_ENGINEER_PENDING,
                    ApplicationStage.REJECTED
                },
                [ApplicationStage.EXECUTIVE_ENGINEER_PENDING] = new List<ApplicationStage>
                {
                    ApplicationStage.CITY_ENGINEER_PENDING,
                    ApplicationStage.EXECUTIVE_ENGINEER_SIGN_PENDING,
                    ApplicationStage.REJECTED
                },
                [ApplicationStage.CITY_ENGINEER_PENDING] = new List<ApplicationStage>
                {
                    ApplicationStage.PAYMENT_PENDING,
                    ApplicationStage.CITY_ENGINEER_SIGN_PENDING,
                    ApplicationStage.REJECTED
                },
                [ApplicationStage.PAYMENT_PENDING] = new List<ApplicationStage>
                {
                    ApplicationStage.CLERK_PENDING,
                    ApplicationStage.REJECTED
                },
                [ApplicationStage.CLERK_PENDING] = new List<ApplicationStage>
                {
                    ApplicationStage.EXECUTIVE_ENGINEER_SIGN_PENDING,
                    ApplicationStage.REJECTED
                },
                [ApplicationStage.EXECUTIVE_ENGINEER_SIGN_PENDING] = new List<ApplicationStage>
                {
                    ApplicationStage.CITY_ENGINEER_SIGN_PENDING,
                    ApplicationStage.REJECTED
                },
                [ApplicationStage.CITY_ENGINEER_SIGN_PENDING] = new List<ApplicationStage>
                {
                    ApplicationStage.APPROVED,
                    ApplicationStage.REJECTED
                }
            };

            return validTransitions.ContainsKey(currentStage) &&
                   validTransitions[currentStage].Contains(newStage);
        }


    }
}
