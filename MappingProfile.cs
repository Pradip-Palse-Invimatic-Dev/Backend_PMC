using AutoMapper;
using MyWebApp.Models;
using MyWebApp.Models.Enums;
using MyWebApp.ViewModels;

namespace MyWebApp.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AddressViewModel, Address>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedBy, opt => opt.Ignore());

            CreateMap<DocumentViewModel, Document>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ApplicationId, opt => opt.Ignore())
                .ForMember(dest => dest.Application, opt => opt.Ignore())
                .ForMember(dest => dest.VerifiedByOfficerId, opt => opt.Ignore())
                .ForMember(dest => dest.VerifiedByOfficer, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.DocumentType, opt => opt.MapFrom(src => src.DocumentType))
                .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.FilePath))
                .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
                .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.FileId))
                .ForMember(dest => dest.UploadDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsVerified, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.VerificationComments, opt => opt.Ignore());


            CreateMap<CreateApplicationViewModel, Application>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.ApplicationNumber, opt => opt.Ignore())
                .ForMember(dest => dest.ApplicantId, opt => opt.Ignore())
                .ForMember(dest => dest.Applicant, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ApplicationStatus.Draft))
                .ForMember(dest => dest.CurrentStage, opt => opt.MapFrom(src => ApplicationStage.JUNIOR_ENGINEER_PENDING))
                .ForMember(dest => dest.SubmissionDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
                .ForMember(dest => dest.LastModifiedBy, opt => opt.Ignore())
                .ForMember(dest => dest.Documents, opt => opt.Ignore())
                .ForMember(dest => dest.OfficerAssignments, opt => opt.Ignore())
                .ForMember(dest => dest.Appointment, opt => opt.Ignore())
                .ForMember(dest => dest.Payment, opt => opt.Ignore());

            CreateMap<QualificationViewModel, Qualification>()
                .ForMember(dest => dest.ApplicationId, opt => opt.Ignore())
                .ForMember(dest => dest.InstituteName, opt => opt.MapFrom(src => src.InstituteName))
                .ForMember(dest => dest.UniversityName, opt => opt.MapFrom(src => src.UniversityName))
                .ForMember(dest => dest.Specialization, opt => opt.MapFrom(src => src.Specialization))
                .ForMember(dest => dest.DegreeName, opt => opt.MapFrom(src => src.DegreeName))
                .ForMember(dest => dest.PassingMonth, opt => opt.MapFrom(src => src.PassingMonth))
                .ForMember(dest => dest.YearOfPassing, opt => opt.MapFrom(src => src.YearOfPassing));

            CreateMap<ExperienceViewModel, Experience>()
                .ForMember(dest => dest.ApplicationId, opt => opt.Ignore())
                .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.CompanyName))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
                .ForMember(dest => dest.YearsOfExperience, opt => opt.MapFrom(src => src.YearsOfExperience))
                .ForMember(dest => dest.FromDate, opt => opt.MapFrom(src => src.FromDate))
                .ForMember(dest => dest.ToDate, opt => opt.MapFrom(src => src.ToDate));

            //mapping for GetApplicationViewModel
            CreateMap<Application, GetApplicationViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.ApplicationNumber, opt => opt.MapFrom(src => src.ApplicationNumber))
                .ForMember(dest => dest.ApplicantId, opt => opt.MapFrom(src => src.ApplicantId))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.MotherName, opt => opt.MapFrom(src => src.MotherName))
                .ForMember(dest => dest.MobileNumber, opt => opt.MapFrom(src => src.MobileNumber))
                .ForMember(dest => dest.EmailAddress, opt => opt.MapFrom(src => src.EmailAddress))
                .ForMember(dest => dest.PositionType, opt => opt.MapFrom(src => src.PositionType))
                .ForMember(dest => dest.SubmissionDate, opt => opt.MapFrom(src => src.SubmissionDate))
                .ForMember(dest => dest.BloodGroup, opt => opt.MapFrom(src => src.BloodGroup))
                .ForMember(dest => dest.Height, opt => opt.MapFrom(src => src.Height))
                .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
                .ForMember(dest => dest.PermanentAddress, opt => opt.MapFrom(src => src.PermanentAddress))
                .ForMember(dest => dest.CurrentAddress, opt => opt.MapFrom(src => src.CurrentAddress))
                .ForMember(dest => dest.PANCardNumber, opt => opt.MapFrom(src => src.PANCardNumber))
                .ForMember(dest => dest.AadharCardNumber, opt => opt.MapFrom(src => src.AadharCardNumber))
                .ForMember(dest => dest.COACardNumber, opt => opt.MapFrom(src => src.COACardNumber))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.CurrentStage, opt => opt.MapFrom(src => src.CurrentStage))
                .ForMember(dest => dest.ApprovalDate, opt => opt.MapFrom(src => src.ApprovalDate))
                .ForMember(dest => dest.IsPaymentComplete, opt => opt.MapFrom(src => src.IsPaymentComplete))
                .ForMember(dest => dest.IsCertificateGenerated, opt => opt.MapFrom(src => src.IsCertificateGenerated))
                .ForMember(dest => dest.IsChallanGenerated, opt => opt.MapFrom(src => src.IsChallanGenerated))
                .ForMember(dest => dest.RecommendedFormPath, opt => opt.MapFrom(src => src.RecommendedFormPath))
                .ForMember(dest => dest.CertificatePath, opt => opt.MapFrom(src => src.CertificatePath))
                .ForMember(dest => dest.ChallanPath, opt => opt.MapFrom(src => src.ChallanPath))
                .ForMember(dest => dest.Documents, opt => opt.MapFrom(src => src.Documents));
            // .ForMember(dest => dest.OfficerAssignments, opt => opt.MapFrom(src => src.OfficerAssignments))
            // .ForMember(dest => dest.Appointment, opt => opt.MapFrom(src => src.Appointment))
            // .ForMember(dest => dest.Payment, opt => opt.MapFrom(src => src.Payment));

            CreateMap<Document, GetDocumentViewModel>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.DocumentType, opt => opt.MapFrom(src => src.DocumentType))
                .ForMember(dest => dest.FilePath, opt => opt.MapFrom(src => src.FilePath))
                .ForMember(dest => dest.FileName, opt => opt.MapFrom(src => src.FileName))
                .ForMember(dest => dest.FileId, opt => opt.MapFrom(src => src.FileId));




        }
    }
}
