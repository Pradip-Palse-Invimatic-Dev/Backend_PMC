# PDF Generation API

This document describes the PDF generation functionality for application forms.

## Overview

The PDF generation feature creates recommendation forms in Marathi and English for submitted applications. The system generates PDFs with all application data including personal information, qualifications, experience, and officer signatures.

## API Endpoints

### 1. Generate PDF
**POST** `/api/pdf/generate`

Generates a PDF for the specified application and saves it to the server.

**Request Body:**
```json
{
  "applicationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response:**
```json
{
  "isSuccess": true,
  "message": "PDF generated successfully",
  "filePath": "generated-pdfs/Application_3fa85f64-5717-4562-b3fc-2c963f66afa6_20231009142530.pdf",
  "fileContent": null,
  "fileName": "Application_3fa85f64-5717-4562-b3fc-2c963f66afa6_20231009142530.pdf"
}
```

### 2. Download PDF
**GET** `/api/pdf/download/{applicationId}`

Downloads the PDF for the specified application.

**Parameters:**
- `applicationId`: GUID of the application

**Response:** PDF file for download

### 3. Generate and Download PDF
**GET** `/api/pdf/generate-and-download/{applicationId}`

Generates and immediately downloads the PDF for the specified application.

**Parameters:**
- `applicationId`: GUID of the application

**Response:** PDF file for immediate download

## Features

- **Multilingual Support**: Content in both Marathi (Devanagari script) and English
- **Dynamic Data**: Fetches all application data from the database
- **Officer Information**: Includes assigned officer names for different stages
- **Experience Calculation**: Automatically calculates total years and months of experience
- **Position-Specific Content**: Adapts content based on the position type (Structural Engineer, License Engineer, etc.)
- **Professional Layout**: Uses proper formatting and fonts for official documents

## Data Sources

The PDF generation system fetches data from the following database entities:
- **Application**: Main application details (name, contact, dates, etc.)
- **Address**: Permanent and current addresses
- **Qualification**: Educational qualifications
- **Experience**: Work experience records
- **OfficerAssignment**: Assigned officers for different approval stages

## Configuration

### Fonts
The system uses system fonts optimized for Unicode support:
- **Marathi Text**: Nirmala UI (excellent Devanagari support)
- **English Text**: Segoe UI

For better Devanagari rendering, you can add custom TTF fonts:
1. Place font files in the `Fonts/` directory
2. Update the `.csproj` file to include them as embedded resources
3. Modify the font registration in `PdfService.cs`

### File Storage
Generated PDFs are stored in:
- **Path**: `wwwroot/generated-pdfs/`
- **Naming**: `Application_{ApplicationId}_{Timestamp}.pdf`
- **Database**: File path is saved to `Application.RecommendedFormPath`

## Error Handling

The API includes comprehensive error handling:
- Invalid application ID validation
- Application not found scenarios
- PDF generation failures
- File system errors
- Database operation errors

## Security

- **Authentication**: All endpoints require JWT authentication
- **Authorization**: Users can only access PDFs for applications they have permissions for
- **Logging**: All operations are logged for audit purposes

## Usage Examples

### JavaScript/TypeScript
```javascript
// Generate PDF
const response = await fetch('/api/pdf/generate', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer ' + token
  },
  body: JSON.stringify({
    applicationId: '3fa85f64-5717-4562-b3fc-2c963f66afa6'
  })
});

const result = await response.json();

// Download PDF
window.open(`/api/pdf/download/3fa85f64-5717-4562-b3fc-2c963f66afa6`, '_blank');
```

### C# Client
```csharp
// Generate PDF
var request = new PdfGenerationRequest 
{ 
    ApplicationId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6") 
};

var response = await httpClient.PostAsJsonAsync("/api/pdf/generate", request);
var result = await response.Content.ReadFromJsonAsync<PdfGenerationResponse>();

// Download PDF
var pdfBytes = await httpClient.GetByteArrayAsync(
    $"/api/pdf/download/3fa85f64-5717-4562-b3fc-2c963f66afa6");
```

## Dependencies

- **QuestPDF**: PDF generation library
- **SkiaSharp**: Graphics rendering
- **Entity Framework Core**: Database access
- **ASP.NET Core**: Web framework

## Troubleshooting

### Common Issues

1. **Font Rendering Issues**
   - Ensure system has Unicode-capable fonts installed
   - Add custom Devanagari fonts if needed

2. **File Permission Errors**
   - Verify write permissions to `wwwroot/generated-pdfs/`
   - Check disk space availability

3. **Database Connection Issues**
   - Verify Entity Framework configuration
   - Check database connection string

4. **Authentication Errors**
   - Ensure JWT token is valid and not expired
   - Verify user permissions for the application

For additional support, check the application logs for detailed error information.