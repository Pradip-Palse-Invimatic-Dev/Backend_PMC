using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models.Enums;
using MyWebApp.ViewModels;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;

namespace MyWebApp.Api.Services
{
    public class PdfService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private static bool _fontsRegistered = false;

        public PdfService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
            RegisterFonts();
        }

        private static void RegisterFonts()
        {
            if (_fontsRegistered) return;

            try
            {
                // Using system fonts available on Windows
                // Nirmala UI: Excellent Devanagari support
                // Segoe UI: Standard Windows UI font
                // These fonts are available on Windows 10+ by default
                _fontsRegistered = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to register fonts: {ex.Message}");
            }
        }

        public async Task<PdfGenerationResponse> GenerateApplicationPdfAsync(Guid applicationId)
        {
            try
            {
                var applicationData = await GetApplicationDataAsync(applicationId);
                if (applicationData == null)
                {
                    return new PdfGenerationResponse
                    {
                        IsSuccess = false,
                        Message = "Application not found"
                    };
                }

                var pdfBytes = GeneratePdf(applicationData);
                var fileName = $"Application_{applicationId}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                var filePath = await SavePdfFileAsync(pdfBytes, fileName);

                await UpdateApplicationPdfPathAsync(applicationId, filePath);

                return new PdfGenerationResponse
                {
                    IsSuccess = true,
                    Message = "PDF generated successfully",
                    FilePath = filePath,
                    FileContent = pdfBytes,
                    FileName = fileName
                };
            }
            catch (Exception ex)
            {
                return new PdfGenerationResponse
                {
                    IsSuccess = false,
                    Message = $"Error generating PDF: {ex.Message}"
                };
            }
        }

        private async Task<ApplicationPdfModel?> GetApplicationDataAsync(Guid applicationId)
        {
            var application = await _context.Applications
                .Include(a => a.PermanentAddress)
                .Include(a => a.CurrentAddress)
                .Include(a => a.Qualifications)
                .Include(a => a.Experiences)
                .Include(a => a.OfficerAssignments!)
                    .ThenInclude(oa => oa.Officer)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null) return null;

            var totalExperience = CalculateTotalExperience(application.Experiences);

            var officers = application.OfficerAssignments?.ToDictionary(
                oa => oa.Stage,
                oa => $"{oa.Officer?.FirstName} {oa.Officer?.LastName}".Trim()
            ) ?? new Dictionary<ApplicationStage, string>();

            return new ApplicationPdfModel
            {
                Name = $"{application.FirstName} {application.MiddleName} {application.LastName}".Trim(),
                Address1 = GetFormattedAddress(application.PermanentAddress),
                Address2 = GetFormattedAddress(application.CurrentAddress),
                Position = GetPositionInMarathi(application.PositionType),
                Date = application.SubmissionDate,
                Qualification = application.Qualifications?.Select(q => q.DegreeName).ToList() ?? new List<string>(),
                MobileNumber = application.MobileNumber,
                MonthDifference = totalExperience.Months.ToString(),
                YearDifference = totalExperience.Years.ToString(),
                IsBothAddressSame = application.PermanentAddress?.Id == application.CurrentAddress?.Id,
                JrEnggName = officers.GetValueOrDefault(ApplicationStage.JUNIOR_ENGINEER_PENDING),
                AssEnggName = officers.GetValueOrDefault(ApplicationStage.ASSISTANT_ENGINEER_PENDING),
                ExeEnggName = officers.GetValueOrDefault(ApplicationStage.EXECUTIVE_ENGINEER_PENDING),
                CityEnggName = officers.GetValueOrDefault(ApplicationStage.CITY_ENGINEER_PENDING)
            };
        }

        private (int Years, int Months) CalculateTotalExperience(List<Models.Experience>? experiences)
        {
            if (experiences == null || !experiences.Any())
                return (0, 0);

            var totalMonths = 0;
            foreach (var exp in experiences)
            {
                var months = ((exp.ToDate.Year - exp.FromDate.Year) * 12) + exp.ToDate.Month - exp.FromDate.Month;
                totalMonths += months;
            }

            var years = totalMonths / 12;
            var remainingMonths = totalMonths % 12;

            return (years, remainingMonths);
        }

        private string GetFormattedAddress(Models.Address? address)
        {
            if (address == null) return string.Empty;

            var addressParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(address.AddressLine1))
                addressParts.Add(address.AddressLine1);

            if (!string.IsNullOrWhiteSpace(address.AddressLine2))
                addressParts.Add(address.AddressLine2);

            if (!string.IsNullOrWhiteSpace(address.AddressLine3))
                addressParts.Add(address.AddressLine3);

            if (!string.IsNullOrWhiteSpace(address.City))
                addressParts.Add(address.City);

            if (!string.IsNullOrWhiteSpace(address.State))
                addressParts.Add(address.State);

            if (!string.IsNullOrWhiteSpace(address.PinCode))
                addressParts.Add(address.PinCode);

            return string.Join(", ", addressParts);
        }

        private string GetPositionInMarathi(PositionType positionType)
        {
            return positionType switch
            {
                PositionType.Architect => "आर्किटेक्ट",
                PositionType.StructuralEngineer => "स्ट्रक्चरल इंजिनिअर",
                PositionType.LicenceEngineer => "लायसन्स इंजिनिअर",
                PositionType.Supervisor1 => "सुपरवायझर1",
                PositionType.Supervisor2 => "सुपरवायझर2",
                _ => "अज्ञात"
            };
        }

        private byte[] GeneratePdf(ApplicationPdfModel model)
        {
            try
            {
                var document = new ApplicationPdfDocument(model);
                return document.GeneratePdf();
            }
            catch (Exception ex) when (ex.Message.Contains("typeface") || ex.Message.Contains("font"))
            {
                throw new Exception($"Font rendering error: {ex.Message}. Please ensure your system has Unicode-capable fonts installed.");
            }
        }

        private async Task<string> SavePdfFileAsync(byte[] pdfBytes, string fileName)
        {
            var _folderPath = Path.Combine(Directory.GetCurrentDirectory(), "MediaStorage");
            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }

            var filePath = Path.Combine(_folderPath, fileName);
            await File.WriteAllBytesAsync(filePath, pdfBytes);

            return fileName;
        }

        private async Task UpdateApplicationPdfPathAsync(Guid applicationId, string filePath)
        {
            var application = await _context.Applications.FindAsync(applicationId);
            if (application != null)
            {
                application.RecommendedFormPath = filePath;
                await _context.SaveChangesAsync();
            }
        }
    }

    public class ApplicationPdfDocument : IDocument
    {
        private readonly ApplicationPdfModel _model;

        private const string MarathiFont = "Nirmala UI";
        private const string EnglishFont = "Segoe UI";

        public ApplicationPdfDocument(ApplicationPdfModel model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Legal);
                page.Margin(0.5f, Unit.Inch);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(8);

                // Header title
                column.Item().Row(row =>
                {
                    row.RelativeItem()
                        .AlignCenter()
                        .Text("मा. शहर अभियंता")
                        .Bold()
                        .FontFamily(MarathiFont)
                        .FontSize(12);

                    row.RelativeItem()
                        .AlignCenter()
                        .Text("पुणे महानगरपालिका")
                        .Bold()
                        .FontFamily(MarathiFont)
                        .FontSize(12);
                });

                // Main header text
                column.Item().Column(col =>
                {
                    col.Item().Text(text =>
                    {
                        text.Span("यांजकडे सादर....")
                            .FontSize(13)
                            .FontFamily(MarathiFont);
                    });

                    DateTime date = _model.Date;
                    int year = date.Year;
                    int toYear = year + 2;

                    col.Item().PaddingTop(6).Text(text =>
                    {
                        text.Span($"विषय:- जानेवारी {year} ते डिसेंबर {toYear} करीता {_model.Position} नवीन परवान्याबाबत.")
                            .FontSize(11)
                            .FontFamily(MarathiFont)
                            .LineHeight(1.2f);
                    });

                    col.Item().PaddingTop(6).Text(text =>
                    {
                        text.Span($"विषयांकित प्रकरणी खाली निर्देशित व्यक्तीने जानेवारी {year} ते डिसेंबर {toYear} या कालावधीकरीता पुणे महानगरपालिकेच्या मा. शहर अभियंता कार्यालयाकडे {_model.Position} (नवीन) परवान्याकरिता अर्ज केला आहे.")
                            .FontSize(11)
                            .FontFamily(MarathiFont)
                            .LineHeight(1.15f);
                    });
                });

                // Applicant details
                column.Item().PaddingTop(8).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.ConstantItem(100).Text("अर्जदाराचे नाव -")
                            .FontSize(11)
                            .FontFamily(MarathiFont);
                        row.RelativeItem().Text(_model.Name)
                            .FontSize(11)
                            .FontFamily(EnglishFont)
                            .Bold();
                    });

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(100).Text("अर्जदाराचे शिक्षण -")
                            .FontSize(11)
                            .FontFamily(MarathiFont);
                        row.RelativeItem().Column(colQual =>
                        {
                            if (_model.Qualification.Count > 0)
                            {
                                colQual.Item().Text($"1) {_model.Qualification[0]}")
                                    .FontSize(11)
                                    .FontFamily(EnglishFont)
                                    .Bold();
                            }

                            if (_model.Qualification.Count > 1 && !string.IsNullOrWhiteSpace(_model.Qualification[1]))
                            {
                                colQual.Item().Text($"2) {_model.Qualification[1]}")
                                    .FontSize(11)
                                    .FontFamily(EnglishFont)
                                    .Bold();
                            }
                        });
                    });

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(100).Text("पत्ता :")
                            .FontSize(11)
                            .FontFamily(MarathiFont);
                        row.RelativeItem().Column(colAddr =>
                        {
                            colAddr.Item().Text($"1) {_model.Address1}")
                                .FontSize(11)
                                .FontFamily(EnglishFont)
                                .Bold();

                            if (!_model.IsBothAddressSame)
                            {
                                colAddr.Item().PaddingTop(2).Text($"2) {_model.Address2}")
                                    .FontSize(11)
                                    .FontFamily(EnglishFont)
                                    .Bold();
                            }
                        });
                    });

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(100).Text("मोबाईलनं.-")
                            .FontSize(11)
                            .FontFamily(MarathiFont);
                        row.RelativeItem().Text(_model.MobileNumber)
                            .FontSize(11)
                            .FontFamily(EnglishFont)
                            .Bold();
                    });

                    col.Item().PaddingTop(4).Text(text =>
                    {
                        text.Span("आवश्यक अनुभव - २ वर्षे (युडीसीपीआर २०२० मधील अपेंडिक्स 'सी', सी-४.१")
                            .FontFamily(MarathiFont)
                            .FontSize(11);

                        text.Span("(ii)")
                            .FontFamily(EnglishFont)
                            .FontSize(11);

                        text.Span(" नुसार)")
                            .FontFamily(MarathiFont)
                            .FontSize(11);
                    });

                    col.Item().PaddingTop(4).Row(row =>
                    {
                        row.ConstantItem(100).Text("अनुभव-")
                            .FontSize(11)
                            .FontFamily(MarathiFont);
                        row.RelativeItem().Text(text =>
                        {
                            text.Span(_model.YearDifference ?? "0")
                                .FontSize(11)
                                .FontFamily(EnglishFont)
                                .Bold();

                            text.Span(" वर्षे ")
                                .FontSize(11)
                                .FontFamily(MarathiFont);

                            text.Span(_model.MonthDifference ?? "0")
                                .FontSize(11)
                                .FontFamily(EnglishFont)
                                .Bold();

                            text.Span(" महिने")
                                .FontSize(11)
                                .FontFamily(MarathiFont);
                        });
                    });
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(10);

                var num = _model.Position switch
                {
                    "स्ट्रक्चरल इंजिनिअर" => "4",
                    "लायसन्स इंजिनिअर" => "3",
                    "सुपरवायझर1" => "5.1",
                    "सुपरवायझर2" => "5.1",
                    _ => "4"
                };

                column.Item().Text(text =>
                {
                    text.Line($"    उपरोक्त नमूद केलेल्या व्यक्तीचा मागणी अर्ज, शैक्षणिक पात्रता, अनुभव व पत्त्याचा पुरावा इ. कागदपत्राची तपासणी केली ती बरोबर व नियमानुसार आहेत. त्यानुसार वरील अर्जदाराची मान्य युडीसीपीआर २०२० मधील अपेंडिक्स सी, सी-{num} नुसार पुणे महानगरपालिकेच्या {_model.Position} (नवीन) परवाना धारण करण्यास आवश्यक शैक्षणिक पात्रता व अनुभव असल्याने त्यांचा अर्ज आपले मान्यतेकरिता सादर करीत आहोत.")
                        .FontFamily(MarathiFont)
                        .FontSize(11)
                        .LineHeight(1.15f);
                });

                DateTime date = _model.Date;
                int year = date.Year;
                int toYear = year + 2;

                column.Item().Text(text =>
                {
                    text.Span("    तरी सदर प्रकरणी ")
                        .FontFamily(MarathiFont)
                        .FontSize(11);

                    text.Span(_model.Name + " ")
                        .FontSize(11)
                        .FontFamily(EnglishFont)
                        .Bold();

                    text.Span($"यांचेकडून जानेवारी {year} ते डिसेंबर {toYear} या कालावधी करिता आवश्यक ती फी भरून घेवून {_model.Position} (नवीन) परवाना देणेबाबत मान्यता मिळणेस विनंती आहे.")
                        .FontFamily(MarathiFont)
                        .FontSize(11)
                        .LineHeight(1.15f);
                });

                column.Item().PaddingTop(4).Text("मा.स.कळावे.")
                    .FontFamily(MarathiFont)
                    .FontSize(11);
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(0);

                // Spacer for signature area
                column.Item().Height(80);

                // First row of signatures
                column.Item().Row(row =>
                {
                    row.RelativeItem(1).Column(col =>
                    {
                        col.Item().Height(50);
                        col.Item().AlignCenter().Text(text =>
                        {
                            text.Line($"({_model.JrEnggName ?? ""})")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                            text.Line("शाखा अभियंता")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                            text.Line("शहर-अभियंता कार्यालय")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                            text.Line("पुणे महानगरपालिका")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                        });
                    });

                    row.RelativeItem(1).Column(col =>
                    {
                        col.Item().Height(50);
                        col.Item().AlignCenter().Text(text =>
                        {
                            text.Line($"({_model.AssEnggName ?? ""})")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                            text.Line("उपअभियंता")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                            text.Line("पुणे महानगरपालिका")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                        });
                    });
                });

                // Recommendation text
                column.Item().PaddingTop(8).Text("प्रस्तुत प्रकरणी उपरोक्त प्रमाणे छाननी झाली असल्याने मान्यतेस शिफारस आहे.")
                    .FontFamily(MarathiFont)
                    .FontSize(11);

                // Approval section
                column.Item().PaddingTop(4).AlignRight().PaddingRight(40).Text("क्ष मान्य")
                    .FontFamily(MarathiFont)
                    .FontSize(11);

                // Second row of signatures
                column.Item().PaddingTop(40).Row(row =>
                {
                    row.RelativeItem(1).Column(col =>
                    {
                        col.Item().Height(50);
                        col.Item().AlignCenter().Text(text =>
                        {
                            text.Line($"({_model.ExeEnggName ?? ""})")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                            text.Line("कार्यकारी अभियंता")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                            text.Line("पुणे महानगरपालिका")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                        });
                    });

                    row.RelativeItem(1).Column(col =>
                    {
                        col.Item().Height(50);
                        col.Item().AlignCenter().Text(text =>
                        {
                            text.Line($"({_model.CityEnggName ?? ""})")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                            text.Line("शहर अभियंता")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                            text.Line("पुणे महानगरपालिका")
                                .FontFamily(MarathiFont)
                                .FontSize(10);
                        });
                    });
                });
            });
        }
    }
}