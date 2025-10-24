namespace MyWebApp.Api.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using MyWebApp.Api.Common;
    using MyWebApp.Data;
    using MyWebApp.Models;
    using MyWebApp.Models.Enums;
    using MyWebApp.ViewModels;
    using QuestPDF.Drawing;
    using QuestPDF.Fluent;
    using QuestPDF.Helpers;
    using QuestPDF.Infrastructure;

    public class SECertificateGenerationService : ISECertificateGenerationService
    {
        private readonly ApplicationDbContext _context;
        private readonly FileService _uploadFileService;
        private readonly ILogger<SECertificateGenerationService> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private readonly string _certificateImagePath;
        private const string MarathiFont = "Nirmala UI";
        private const string EnglishFont = "Segoe UI";

        public SECertificateGenerationService(
            ApplicationDbContext context,
            FileService uploadFileService,
            ILogger<SECertificateGenerationService> logger,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _context = context;
            _uploadFileService = uploadFileService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _certificateImagePath = Path.Combine(webHostEnvironment.WebRootPath, "Images", "Certificate");

            Directory.CreateDirectory(_certificateImagePath);
        }

        public async Task<GenerateSECertificateResponseViewModel> GenerateSECertificateAsync(
            GenerateSECertificateRequestViewModel model,
            string userId)
        {
            try
            {
                _logger.LogInformation($"Generating SE Certificate for ApplicationId: {model.ApplicationId}");

                var application = await _context.Applications
                    .Include(a => a.CurrentAddress)
                    .FirstOrDefaultAsync(a => a.Id.ToString() == model.ApplicationId);

                if (application == null)
                {
                    return new GenerateSECertificateResponseViewModel
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                // Check if certificate is already generated for this application
                if (application.IsCertificateGenerated && !string.IsNullOrEmpty(application.CertificateNumber))
                {
                    _logger.LogInformation($"Certificate already exists for ApplicationId: {model.ApplicationId}, CertificateNumber: {application.CertificateNumber}");

                    return new GenerateSECertificateResponseViewModel
                    {
                        Success = false,
                        Message = "Certificate has already been generated for this application.",
                        CertificateId = application.Id.ToString(),
                        CertificateNumber = application.CertificateNumber,
                        FileKey = application.CertificatePath ?? "",
                        FileName = GetCertificateFileName(application.CertificateNumber),
                        GeneratedDate = application.CertificateGeneratedDate ?? DateTime.UtcNow
                    };
                }

                string certificateNumber = GenerateCertificateNumber(application.PositionType);

                var logoBytes = GetImageBytes("logo.png");
                var profilePhotoBytes = GetImageBytes("profile.png");

                if (logoBytes == null || profilePhotoBytes == null)
                {
                    return new GenerateSECertificateResponseViewModel
                    {
                        Success = false,
                        Message = "Required images not found. Please ensure logo.png and profile.png exist in wwwroot/Images/Certificate"
                    };
                }

                var sanitizedCertNumber = certificateNumber.Replace("/", "_").Replace("\\", "_");
                var fileName = $"SE_Certificate_{sanitizedCertNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";

                // Generate a unique file key that we'll use consistently
                var fileKey = $"{Guid.NewGuid()}_{fileName.Replace("/", "_").Replace("\\", "_")}";

                // Generate QR code with download URL using the pre-generated file key
                var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://pmcrms.ezbricks-dev.codetools.in";
                string qrData = $"{baseUrl}/api/Certificate/download/{fileKey}";
                var qrCodeBytes = QrCodeGenerator.GenerateQrCode(qrData);

                var certificateModel = new SECertificateModel
                {
                    CertificateNumber = certificateNumber,
                    Name = $"{application.FirstName} {application.MiddleName} {application.LastName}".Trim(),
                    Address = GetFullAddress(application),
                    Position = GetMarathiPosition(application.PositionType),
                    FromDate = DateTime.UtcNow,
                    ToYear = DateTime.UtcNow.Year + 3,
                    Logo = logoBytes,
                    ProfilePhoto = profilePhotoBytes,
                    QrCodeBytes = qrCodeBytes, // Use actual QR code with correct download URL
                    IsPayment = model.IsPayment,
                    TransactionDate = model.TransactionDate == default(DateTime)
                        ? DateTime.UtcNow.ToString("dd/MM/yyyy")
                        : model.TransactionDate.ToString("dd/MM/yyyy"),
                    ChallanNumber = model.ChallanNumber ?? "",
                    Amount = model.Amount.ToString()
                };

                // Generate PDF only once with the correct QR code
                var pdfBytes = GenerateSECertificatePdf(certificateModel);

                // Save the PDF with the pre-generated file key
                var finalFileKey = await SaveFileWithCustomKeyAsync(fileName, pdfBytes, fileKey);

                application.CertificateNumber = certificateNumber;
                application.CertificateGeneratedDate = DateTime.UtcNow;
                application.CertificateGeneratedBy = userId;
                application.IsCertificateGenerated = true;
                application.CertificatePath = finalFileKey;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"SE Certificate generated successfully. CertificateNumber: {certificateNumber}");

                return new GenerateSECertificateResponseViewModel
                {
                    Success = true,
                    Message = "SE Certificate generated successfully",
                    CertificateId = application.Id.ToString(),
                    CertificateNumber = certificateNumber,
                    FileKey = finalFileKey,
                    FileName = fileName,
                    GeneratedDate = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SE certificate");
                return new GenerateSECertificateResponseViewModel
                {
                    Success = false,
                    Message = $"Error generating certificate: {ex.Message}"
                };
            }
        }

        private byte[] GenerateSECertificatePdf(SECertificateModel model)
        {
            try
            {
                var document = new SECertificateDocument(model);
                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF document");
                throw;
            }
        }

        private string GenerateCertificateNumber(PositionType positionType)
        {
            string prefix = GetCertificatePrefix(positionType);
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            int count = _context.Applications
                .Where(a => a.CertificateNumber != null &&
                            a.CertificateNumber.Contains(prefix) &&
                            a.CertificateGeneratedDate.HasValue &&
                            a.CertificateGeneratedDate.Value >= today &&
                            a.CertificateGeneratedDate.Value < tomorrow)
                .Count() + 1;

            return $"PMC/{prefix}/{count}/{DateTime.UtcNow.Year}-{DateTime.UtcNow.Year + 3}";
        }

        private string GetCertificatePrefix(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => "ARCH",
                PositionType.StructuralEngineer => "STR.ENGG",
                PositionType.LicenceEngineer => "LIC.ENGG",
                PositionType.Supervisor1 => "SUPER1",
                PositionType.Supervisor2 => "SUPER2",
                _ => "ENGG"
            };
        }

        private string GetMarathiPosition(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => "आर्किटेक्ट",
                PositionType.StructuralEngineer => "स्ट्रक्चरल इंजिनिअर",
                PositionType.LicenceEngineer => "लायसन्स इंजिनिअर",
                PositionType.Supervisor1 => "सुपरवायझर१",
                PositionType.Supervisor2 => "सुपरवायझर२",
                _ => "इंजिनिअर"
            };
        }

        private string GetEnglishPosition(string marathiPosition)
        {
            return marathiPosition switch
            {
                "आर्किटेक्ट" => "Architect",
                "स्ट्रक्चरल इंजिनिअर" => "Structural Engineer",
                "लायसन्स इंजिनिअर" => "Licence Engineer",
                "सुपरवायझर१" => "Supervisor1",
                "सुपरवायझर२" => "Supervisor2",
                _ => "Engineer"
            };
        }

        private string GetFullAddress(Application application)
        {
            if (application.CurrentAddress == null)
                return "";

            return $"{application.CurrentAddress.AddressLine1}, {application.CurrentAddress.City} {application.CurrentAddress.State} {application.CurrentAddress.PinCode}";
        }

        private byte[] GetImageBytes(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_certificateImagePath, fileName);
                if (File.Exists(filePath))
                {
                    return File.ReadAllBytes(filePath);
                }
                _logger.LogWarning($"Image file not found: {filePath}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading image file: {fileName}");
                return null;
            }
        }

        public async Task<CertificateInfoViewModel?> GetCertificateInfoAsync(string applicationId)
        {
            try
            {
                _logger.LogInformation($"Getting certificate info for ApplicationId: {applicationId}");

                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.Id.ToString() == applicationId);

                if (application == null)
                {
                    _logger.LogWarning($"Application not found for ApplicationId: {applicationId}");
                    return null;
                }

                if (!application.IsCertificateGenerated || string.IsNullOrEmpty(application.CertificateNumber))
                {
                    _logger.LogWarning($"Certificate not generated for ApplicationId: {applicationId}");
                    return null;
                }

                return new CertificateInfoViewModel
                {
                    ApplicationId = applicationId,
                    CertificateNumber = application.CertificateNumber,
                    FileKey = application.CertificatePath,
                    FileName = GetCertificateFileName(application.CertificateNumber),
                    GeneratedDate = application.CertificateGeneratedDate,
                    GeneratedBy = application.CertificateGeneratedBy,
                    IsCertificateGenerated = application.IsCertificateGenerated,
                    ApplicantName = $"{application.FirstName} {application.MiddleName} {application.LastName}".Trim(),
                    Position = GetMarathiPosition(application.PositionType)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certificate info for ApplicationId: {ApplicationId}", applicationId);
                return null;
            }
        }

        public string GetCertificateFileName(string certificateNumber)
        {
            try
            {
                // Replace any invalid characters in certificate number for filename
                var sanitized = certificateNumber.Replace("/", "_").Replace("\\", "_");
                return $"SE_Certificate_{sanitized}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            }
            catch
            {
                return $"SE_Certificate_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            }
        }

        private async Task<string> SaveFileWithCustomKeyAsync(string fileName, byte[] fileBytes, string customFileKey)
        {
            try
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "MediaStorage");

                // Ensure the directory exists
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, customFileKey);

                // Ensure the directory path exists (including any subdirectories)
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                await File.WriteAllBytesAsync(filePath, fileBytes);

                return customFileKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving file {FileName} with custom key {CustomKey}", fileName, customFileKey);
                throw;
            }
        }
    }

    public class SECertificateDocument : IDocument
    {
        private readonly SECertificateModel _model;
        private const string MarathiFont = "Nirmala UI";
        private const string EnglishFont = "Segoe UI";

        public SECertificateDocument(SECertificateModel model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Legal);
                page.Margin(30);

                page.Content().Column(column =>
                {
                    // Header with department info
                    column.Item().Row(row =>
                    {
                        row.RelativeItem();
                        row.RelativeItem().AlignRight().Text("बांधकाम विकास विभाग\nपुणे महानगरपालिका")
                            .FontFamily(MarathiFont)
                            .FontSize(10)
                            .Bold();
                    });

                    column.Item().Height(20);

                    // Images row: QR Code, Logo, Profile Photo
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Width(80).Image(_model.QrCodeBytes);
                        row.RelativeItem().AlignCenter().Width(80).Image(_model.Logo);
                        row.RelativeItem().AlignCenter().Width(76).Border(1).Padding(2).Image(_model.ProfilePhoto);
                    });

                    column.Item().Height(15);

                    // Organization name
                    column.Item().AlignCenter().Text("पुणे महानगरपालिका")
                        .FontFamily(MarathiFont)
                        .FontSize(16)
                        .FontColor(Colors.Black)
                        .Bold();

                    // Certificate title
                    column.Item().AlignCenter().Text($"{_model.Position} च्या कामासाठी परवाना")
                        .FontFamily(MarathiFont)
                        .FontSize(12)
                        .FontColor(Colors.Black);

                    column.Item().Height(15);

                    // Legal reference
                    column.Item().Text($"महाराष्ट्र प्रादेशिक अधिनियम १९६६ चे कलम ३७ (१ कक )(ग) कलम २० (४)/ नवि-१३ दि.२/१२/२०२० अन्वये पुणे शहरासाठी मान्य झालेल्या एकत्रिकृत विकास नियंत्रण व प्रोत्साहन नियमावली (यूडीसीपीआर -२०२०) नियम क्र.अपेंडिक्स 'सी' अन्वये आणि महाराष्ट्र महानगरपालिका अधिनियम १९४९ चे कलम ३७२ अन्वये {_model.Position} काम करण्यास परवाना देण्यात येत आहे.")
                        .FontFamily(MarathiFont)
                        .FontSize(10)
                        .LineHeight(1.2f);

                    column.Item().Height(10);

                    // Certificate number
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text(text =>
                        {
                            text.Span("परवाना क्र. :- ").FontFamily(MarathiFont).FontSize(10);
                            if (_model.IsPayment)
                            {
                                text.Span($"{_model.CertificateNumber}     From {_model.FromDate:dd/MM/yyyy} to 31/12/{_model.ToYear}     ({GetEnglishPosition(_model.Position)})")
                                    .FontFamily(EnglishFont)
                                    .FontSize(10)
                                    .Bold();
                            }
                        });
                    });

                    column.Item().Height(8);

                    // Name
                    column.Item().Text(text =>
                    {
                        text.Span("नाव :- ").FontFamily(MarathiFont).FontSize(10);
                        text.Span(_model.Name).FontFamily(EnglishFont).FontSize(10).Bold();
                    });

                    // Address and Date
                    column.Item().Row(row =>
                    {
                        row.RelativeItem(2).Text(text =>
                        {
                            text.Span("पत्ता :- ").FontFamily(MarathiFont).FontSize(10);
                            text.Span(_model.Address).FontFamily(EnglishFont).FontSize(10).Bold();
                        });

                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("दिनांक :- ").FontFamily(MarathiFont).FontSize(10);
                            text.Span($"{_model.FromDate:dd/MM/yyyy}").FontFamily(EnglishFont).FontSize(10);
                        });
                    });

                    column.Item().Height(10);

                    // Terms and conditions paragraphs
                    column.Item().Text($"महाराष्ट्र प्रादेशिक अधिनियम १९६६ चे कलम ३७ (१ कक )(ग) कलम २० (४)/ नवि-१३ दि.२/१२/२००० अन्वये पुणे शहरासाठी मान्य झालेल्या एकत्रिकृत विकास नियंत्रण व प्रोत्साहन नियमावली (यूडीसीपीआर -२००) नियम क्र.अपेंडिक्स 'सी' अन्वये आणि महाराष्ट्र महानगरपालिका अधिनियम, १९४९ चे कलम ३७२ अन्वये मी तुम्हांस वर निर्देश केलेल्या कायदा व नियमानुसार ३ वर्षांकरीता दि. {_model.FromDate:dd/MM/yyyy} ते 31/12/{_model.ToYear} अखेर {_model.Position} म्हणून 'खालील मर्यादा व अटी यांचे पालन करणार' या अटीवर परवाना देत आहे.")
                        .FontFamily(MarathiFont)
                        .FontSize(10)
                        .LineHeight(1.2f);

                    column.Item().Height(8);

                    column.Item().Text("'मा. महापालिका आयुक्त, यांनी वेळोवेळी स्थायी समितीच्या संमतीने वरील कायद्याचे कलम ३७३ परवानाधारण करणार यांच्या माहितीसाठी काढण्यात आलेल्या आज्ञेचे आणि विकास (बांधकाम) नियंत्रण व प्रोत्साहन नियमावलीतील अपेंडिक्स 'सी' मधील कर्तव्ये व जबाबदारी यांचे पालन करणार' ही परवानगीची अट राहील आणि धंद्याच्या प्रत्येक बाबतीत परवान्याच्या मुदतीत ज्यावेळी तुमचा सल्ला घेण्यात येईल त्यावेळी तुम्ही आतापावेतो निघालेल्या आज्ञांचे पालन करून त्याप्रमाणे काम करावयाचे आहे.")
                        .FontFamily(MarathiFont)
                        .FontSize(10)
                        .LineHeight(1.2f);

                    column.Item().Height(8);

                    column.Item().Text("जी आज्ञापत्रक वेळोवेळी काढण्यात आलेली आहेत, ती मुख्य कार्यालयाकडे माहितीसाठी ठेवण्यात आलेली असून, जरूर त्यावेळी कार्यालयाच्या वेळेमध्ये त्यांची पाहणी करता येईल.")
                        .FontFamily(MarathiFont)
                        .FontSize(10)
                        .LineHeight(1.2f);

                    column.Item().Height(8);

                    column.Item().Text("मात्र हे लक्षात घेणे जरूर आहे की, मा. महापालिका आयुक्त सदरचा परवाना महाराष्ट्र महानगरपालिका अधिनियम, कलम ३८६ अनुसार जरूर तेव्हा तात्पुरता बंद अगर रद्द करू शकतात जर वर निर्दिष्ट केलेली बंधने अगर शर्थी यांचा भंग झाला अगर टाळल्या गेल्या अथवा तुम्ही सदर कायद्याच्या नियमांचे अगर वेळोवेळी काढण्यात आलेल्या आज्ञापत्रकाचे उल्लंघन केल्याचे दृष्टोपतीस आल्यास आणि जर सदरचा परवाना तात्पुरता तहकूब अगर रद्द झाल्यास अथवा सदरच्या परवान्याची मुदत संपल्यावर तुम्हास परवाना नसल्याचे समजले जाईल आणि महानगरपालिका अधिनियमाचे कलम ६९ अन्वये मा.महापालिका आयुक्त, अगर त्यांनी अधिकार दिलेल्या अधिका-यांनी सदर परवान्याची मागणी केल्यास सदरचा परवाना तुम्हास त्या त्या वेळी हजर करावा लागेल.")
                        .FontFamily(MarathiFont)
                        .FontSize(10)
                        .LineHeight(1.2f);

                    column.Item().Height(8);

                    // Payment information
                    column.Item().Text(text =>
                    {
                        text.Span("महाराष्ट्र शासनाने पुणे शहरासाठी मान्य केलेल्या विकास (बांधकाम) नियंत्रण व प्रोत्साहन नियमावलीनुसार परवाना शुल्क म्हणून रु. ")
                            .FontFamily(MarathiFont)
                            .FontSize(10);
                        text.Span(_model.Amount)
                            .FontFamily(EnglishFont)
                            .FontSize(10)
                            .Bold();
                        text.Span(" चलन क्र. ")
                            .FontFamily(MarathiFont)
                            .FontSize(10);
                        text.Span(_model.ChallanNumber)
                            .FontFamily(EnglishFont)
                            .FontSize(10)
                            .Bold();
                        text.Span(" दि. ")
                            .FontFamily(MarathiFont)
                            .FontSize(10);
                        text.Span(_model.TransactionDate)
                            .FontFamily(EnglishFont)
                            .FontSize(10)
                            .Bold();
                        text.Span(" अन्वये भरले आहे.")
                            .FontFamily(MarathiFont)
                            .FontSize(10);
                    });

                    column.Item().Height(40);

                    // Signatures
                    column.Item().Row(row =>
                    {
                        var engineerType = _model.Position == "स्ट्रक्चरल इंजिनिअर" ? "कार्यकारी" : "उप";

                        row.RelativeItem().AlignCenter().Column(col =>
                        {
                            col.Item().Height(50);
                            col.Item().Text($"{engineerType} अभियंता\n(बांधकाम विकास विभाग)\nपुणे महानगरपालिका")
                                .FontFamily(MarathiFont)
                                .FontSize(10)
                                .LineHeight(1.0f);
                        });

                        row.RelativeItem().AlignCenter().Column(col =>
                        {
                            col.Item().Height(50);
                            col.Item().Text("शहर अभियंता\nपुणे महानगरपालिका")
                                .FontFamily(MarathiFont)
                                .FontSize(10)
                                .LineHeight(1.0f);
                        });
                    });

                    column.Item().Height(10);

                    // Note
                    column.Item().Text($"टीप – प्रस्तुत परवान्याची मुदत ३१ डिसेंबर रोजी संपते जर पुढील वर्षासाठी त्याचे नूतनीकरण करणे असेल तर यासाठी कमीत कमी १५ दिवस परवाना मुदत संपण्या अगोदर परवाना शुल्कासहित अर्ज सादर केला पाहिजे. परवान्याचे नूतनीकरण करून घेण्याबद्दल तुम्हास वेगळी समज दली जाणार नाही जोपर्यंत परवान्याच्या नूतनीकरणासाठी परवाना शुल्कासहित अर्ज दिलेला नाही तोपर्यंत {_model.Position} म्हणून काम करता येणार नाही. तसेच परवाना नाकारल्यासही तुम्हास {_model.Position} म्हणून काम करता येणार नाही.")
                        .FontFamily(MarathiFont)
                        .FontSize(10)
                        .LineHeight(1.2f);
                });
            });
        }

        private string GetEnglishPosition(string marathiPosition)
        {
            return marathiPosition switch
            {
                "आर्किटेक्ट" => "Architect",
                "स्ट्रक्चरल इंजिनिअर" => "Structural Engineer",
                "लायसन्स इंजिनिअर" => "Licence Engineer",
                "सुपरवायझर१" => "Supervisor1",
                "सुपरवायझर२" => "Supervisor2",
                _ => "Engineer"
            };
        }
    }
}