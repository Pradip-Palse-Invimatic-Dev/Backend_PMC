using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;

namespace MyWebApp.Api.Services
{
    public class ChallanService : IChallanService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChallanService> _logger;
        private readonly string _challanFolderPath;

        static ChallanService()
        {
            // Configure QuestPDF to allow missing glyphs and use available system fonts
            try
            {
                // Disable strict glyph checking to allow fallback fonts
                QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

                // Available fonts on this system that support Devanagari: Nirmala UI
                // We'll rely on QuestPDF's built-in font fallback system
                Console.WriteLine("QuestPDF configured with relaxed glyph checking");
                Console.WriteLine("Available Devanagari fonts: Nirmala UI");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Font configuration error: {ex.Message}");
            }
        }
        public ChallanService(ApplicationDbContext context, ILogger<ChallanService> logger)
        {
            _context = context;
            _logger = logger;
            _challanFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "MediaStorage", "Challans");
            Directory.CreateDirectory(_challanFolderPath);
        }

        public async Task<ChallanGenerationResponse> GenerateChallanAsync(ChallanGenerationRequest request)
        {
            try
            {
                _logger.LogInformation($"Generating challan for application {request.ApplicationId}");

                // Check if application exists
                var application = await _context.Applications
                    .FirstOrDefaultAsync(a => a.Id.ToString() == request.ApplicationId);

                if (application == null)
                {
                    return new ChallanGenerationResponse
                    {
                        Success = false,
                        Message = "Application not found"
                    };
                }

                // Check if challan already exists
                var existingChallan = await _context.Challans
                    .FirstOrDefaultAsync(c => c.ApplicationId == application.Id);

                if (existingChallan != null && existingChallan.IsGenerated)
                {
                    return new ChallanGenerationResponse
                    {
                        Success = true,
                        Message = "Challan already generated",
                        ChallanPath = existingChallan.FilePath,
                        ChallanNumber = existingChallan.ChallanNumber
                    };
                }

                // Generate challan number if not provided
                var challanNumber = string.IsNullOrEmpty(request.ChallanNumber)
                    ? GenerateChallanNumber()
                    : request.ChallanNumber;

                // Create challan model
                var challanModel = new ChallanModel
                {
                    ChallanNumber = challanNumber,
                    Name = application.FirstName + " " + application.LastName,
                    Position = application.PositionType.ToString(),
                    Amount = request.Amount,
                    AmountInWords = request.AmountInWords,
                    Date = request.Date,
                    Number = application.MobileNumber,
                    Address = $"{application.CurrentAddress?.AddressLine1}, {application.CurrentAddress?.City}"
                };

                // Generate PDF
                var pdfBytes = GenerateChallanPdf(challanModel);

                // Save PDF to file
                var fileName = $"Challan_{application.Id}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var filePath = Path.Combine(_challanFolderPath, fileName);
                await File.WriteAllBytesAsync(filePath, pdfBytes);

                // Save or update challan record
                if (existingChallan != null)
                {
                    existingChallan.ChallanNumber = challanNumber;
                    existingChallan.Name = request.Name;
                    existingChallan.Position = request.Position;
                    existingChallan.Amount = request.Amount;
                    existingChallan.AmountInWords = request.AmountInWords;
                    existingChallan.ChallanDate = challanModel.Date;
                    existingChallan.FilePath = filePath;
                    existingChallan.IsGenerated = true;
                    existingChallan.LastModifiedAt = DateTime.UtcNow;
                }
                else
                {
                    var challan = new Challan
                    {
                        Id = Guid.NewGuid(),
                        ApplicationId = application.Id,
                        ChallanNumber = challanNumber,
                        Name = request.Name,
                        Position = request.Position,
                        Amount = request.Amount,
                        AmountInWords = request.AmountInWords,
                        ChallanDate = challanModel.Date,
                        Number = challanModel.Number,
                        Address = challanModel.Address,
                        FilePath = filePath,
                        IsGenerated = true,
                        CreatedAt = DateTime.UtcNow,
                        LastModifiedAt = DateTime.UtcNow
                    };

                    _context.Challans.Add(challan);
                }

                // Update application
                application.IsChallanGenerated = true;
                application.ChallanPath = filePath;
                application.LastModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Challan generated successfully for application {request.ApplicationId}");

                return new ChallanGenerationResponse
                {
                    Success = true,
                    Message = "Challan generated successfully",
                    ChallanPath = filePath,
                    ChallanNumber = challanNumber,
                    PdfContent = pdfBytes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating challan for application {request.ApplicationId}");
                return new ChallanGenerationResponse
                {
                    Success = false,
                    Message = "Error generating challan: " + ex.Message
                };
            }
        }

        public async Task<byte[]?> GetChallanPdfAsync(Guid applicationId)
        {
            try
            {
                var challan = await _context.Challans
                    .FirstOrDefaultAsync(c => c.ApplicationId == applicationId && c.IsGenerated);

                if (challan?.FilePath != null && File.Exists(challan.FilePath))
                {
                    return await File.ReadAllBytesAsync(challan.FilePath);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting challan PDF for application {applicationId}");
                return null;
            }
        }

        public async Task<string?> GetChallanPathAsync(Guid applicationId)
        {
            try
            {
                var challan = await _context.Challans
                    .FirstOrDefaultAsync(c => c.ApplicationId == applicationId && c.IsGenerated);

                return challan?.FilePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting challan path for application {applicationId}");
                return null;
            }
        }

        public async Task<bool> IsChallanGeneratedAsync(Guid applicationId)
        {
            try
            {
                return await _context.Challans
                    .AnyAsync(c => c.ApplicationId == applicationId && c.IsGenerated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking challan status for application {applicationId}");
                return false;
            }
        }

        private byte[] GenerateChallanPdf(ChallanModel model)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.Content().Element(content => ComposeContent(content, model));
                });
            });

            return document.GeneratePdf();
        }

        private void ComposeContent(IContainer container, ChallanModel model)
        {
            container.Column(column =>
            {
                // Two identical challan copies side by side
                column.Item().Row(row =>
                {
                    // Left challan copy
                    row.RelativeItem()
                        .Border(2)
                        .Padding(8)
                        .Column(leftCol => ComposeChallanContent(leftCol, model));

                    row.ConstantItem(20); // Space between challans

                    // Right challan copy (duplicate)
                    row.RelativeItem()
                        .Border(2)
                        .Padding(8)
                        .Column(rightCol => ComposeChallanContent(rightCol, model));
                });
            });
        }

        private void ComposeChallanContent(ColumnDescriptor column, ChallanModel model)
        {
            // Header with logo and title
            column.Item().Row(headerRow =>
            {
                // Logo section
                headerRow.ConstantItem(60).Column(logoCol =>
                {
                    try
                    {
                        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", "Certificate", "logo.png");
                        if (File.Exists(logoPath))
                        {
                            var logoBytes = File.ReadAllBytes(logoPath);
                            logoCol.Item().Width(50).Height(50).Image(logoBytes);
                        }
                        else
                        {
                            // Placeholder if logo not found
                            logoCol.Item().Width(50).Height(50).Border(1).AlignCenter().AlignMiddle()
                                .Text("LOGO").FontSize(8);
                        }
                    }
                    catch
                    {
                        // Fallback if logo loading fails
                        logoCol.Item().Width(50).Height(50).Border(1).AlignCenter().AlignMiddle()
                            .Text("PMC").FontSize(8).Bold();
                    }
                });

                headerRow.ConstantItem(10); // Space

                // Title section
                headerRow.RelativeItem().Column(titleCol =>
                {
                    titleCol.Item().AlignCenter().Text("PUNE MUNICIPAL CORPORATION")
                        .FontFamily("Arial")
                        .FontSize(12)
                        .Bold()
                        .FontColor(Colors.Black);

                    titleCol.Item().AlignCenter().Text("चलन पावती")
                        .FontFamily("Nirmala UI")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Colors.Black);
                });
            });

            column.Item().Height(10); // Space after header

            // Reference section
            column.Item().Text("फाईल/संदर्भ")
                .FontFamily("Nirmala UI")
                .FontSize(10)
                .Bold()
                .FontColor(Colors.Black);

            column.Item().Height(5);

            // Application details
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("अर्ज क्र :")
                    .FontFamily("Nirmala UI")
                    .FontSize(9)
                    .FontColor(Colors.Black);
                row.RelativeItem().Text("LIC01")
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text("चलन क्र :")
                    .FontFamily("Nirmala UI")
                    .FontSize(9)
                    .FontColor(Colors.Black);
                row.RelativeItem().Text(model.ChallanNumber)
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text("खात्याचे नाव :")
                    .FontFamily("Nirmala UI")
                    .FontSize(9)
                    .FontColor(Colors.Black);
                row.RelativeItem().Text(model.Position)
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text("आर्किटेक्ट नाव :")
                    .FontFamily("Nirmala UI")
                    .FontSize(9)
                    .FontColor(Colors.Black);
                row.RelativeItem().Text("")
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text("मालकाचे नाव :")
                    .FontFamily("Nirmala UI")
                    .FontSize(9)
                    .FontColor(Colors.Black);
                row.RelativeItem().Text(model.Name)
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Text("मिळकत :")
                    .FontFamily("Nirmala UI")
                    .FontSize(9)
                    .FontColor(Colors.Black);
                row.RelativeItem().Text("NEW LICENSE ENGG")
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
            });
            //add one line break
            column.Item().Height(5);
            // General category in Marathi
            column.Item().Text("General")
                .FontFamily("Arial")
                .FontSize(9)
                .FontColor(Colors.Black);

            column.Item().Height(5);

            // Horizontal line
            column.Item().LineHorizontal(1).LineColor(Colors.Black);

            column.Item().Height(5);

            // Table header
            column.Item().Row(row =>
            {
                row.RelativeItem(2).Text("अर्थशिर्षक")
                    .FontFamily("Nirmala UI")
                    .FontSize(9)
                    .Bold()
                    .FontColor(Colors.Black);
                row.RelativeItem(2).Text("तपशील")
                    .FontFamily("Nirmala UI")
                    .FontSize(9)
                    .Bold()
                    .FontColor(Colors.Black);
                row.RelativeItem(1).Text("रक्कम रुपये")
                    .FontFamily("Nirmala UI")
                    .FontSize(9)
                    .Bold()
                    .FontColor(Colors.Black);
            });

            column.Item().LineHorizontal(1).LineColor(Colors.Black);

            // Table content
            column.Item().Row(row =>
            {
                row.RelativeItem(2).Text("LicensedEngineer(G)")
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
                row.RelativeItem(2).Text("R123A102")
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
                row.RelativeItem(1).Text($"{model.Amount}.00")
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
            });

            column.Item().Height(10);

            // Total amount line
            column.Item().Row(row =>
            {
                row.RelativeItem(4).Text("");
                row.RelativeItem(1).Text($"{model.Amount}.00")
                    .FontFamily("Arial")
                    .FontSize(9)
                    .Bold()
                    .FontColor(Colors.Black);
            });

            column.Item().LineHorizontal(1).LineColor(Colors.Black);

            column.Item().Height(5);

            // Amount in words
            column.Item().Text("एकूण रक्कम रुपये (अक्षरी)")
                .FontFamily("Nirmala UI")
                .FontSize(9)
                .Bold()
                .FontColor(Colors.Black);

            column.Item().Text(model.AmountInWords)
                .FontFamily("Arial")
                .FontSize(9)
                .Bold()
                .FontColor(Colors.Black);

            column.Item().LineHorizontal(1).LineColor(Colors.Black);

            column.Item().Height(10);

            // Challan date
            column.Item().Row(row =>
            {
                row.RelativeItem().Text("Challan Date.")
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
                row.RelativeItem().Text(model.Date.ToString("dd/MM/yyyy"))
                    .FontFamily("Arial")
                    .FontSize(9)
                    .FontColor(Colors.Black);
            });

            column.Item().Height(10);

            // Footer information
            column.Item().Text("कृपया पैसे जमा करताना हे चलन बरोबर आणा")
                .FontFamily("Nirmala UI")
                .FontSize(8)
                .Italic()
                .FontColor(Colors.Black);
        }
        private string GenerateChallanNumber()
        {
            return $"CH{DateTime.Now:yyyyMMdd}{DateTime.Now.Ticks.ToString().Substring(10)}";
        }
    }

    // Internal model for challan generation
    public class ChallanModel
    {
        public string ChallanNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Position { get; set; } = string.Empty;
        public string Amount { get; set; } = string.Empty;
        public string AmountInWords { get; set; } = string.Empty;
    }
}