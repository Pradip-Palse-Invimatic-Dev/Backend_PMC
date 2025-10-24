# Challan Generation Test Script

## Prerequisites
1. Application running on localhost
2. Valid JWT token
3. Existing application ID in database

## Test Commands

### 1. Generate Challan Manually
```bash
curl -X POST "https://localhost:7001/api/challan/generate" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "applicationId": "YOUR_APPLICATION_ID",
    "name": "Test User",
    "position": "Licensed Engineer",
    "amount": "5000",
    "amountInWords": "Five Thousand Rupees Only",
    "date": "2024-10-11"
  }'
```

### 2. Check Challan Status
```bash
curl -X GET "https://localhost:7001/api/challan/status/YOUR_APPLICATION_ID" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### 3. Download Challan PDF
```bash
curl -X GET "https://localhost:7001/api/challan/download/YOUR_APPLICATION_ID" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  --output challan.pdf
```

### 4. Generate via Plugin Interface
```bash
curl -X POST "https://localhost:7001/api/challan/generate-via-plugin/YOUR_APPLICATION_ID" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## PowerShell Version

```powershell
# Set variables
$baseUrl = "https://localhost:7001"
$token = "YOUR_JWT_TOKEN"
$applicationId = "YOUR_APPLICATION_ID"

# Generate Challan
$body = @{
    applicationId = $applicationId
    name = "Test User"
    position = "Licensed Engineer"
    amount = "5000"
    amountInWords = "Five Thousand Rupees Only"
    date = "2024-10-11"
} | ConvertTo-Json

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Invoke-RestMethod -Uri "$baseUrl/api/challan/generate" -Method POST -Body $body -Headers $headers

# Check Status
Invoke-RestMethod -Uri "$baseUrl/api/challan/status/$applicationId" -Method GET -Headers $headers

# Download PDF
Invoke-WebRequest -Uri "$baseUrl/api/challan/download/$applicationId" -Headers $headers -OutFile "challan.pdf"
```

## Expected Results

### Generate Response
```json
{
  "success": true,
  "message": "Challan generated successfully",
  "challanPath": "D:\\...\\MediaStorage\\Challans\\Challan_guid_20241011_143022.pdf",
  "challanNumber": "CH202410110001"
}
```

### Status Response
```json
{
  "applicationId": "guid-here",
  "isGenerated": true,
  "challanPath": "D:\\...\\MediaStorage\\Challans\\Challan_guid_20241011_143022.pdf",
  "downloadUrl": "/api/challan/download/guid-here"
}
```

### Download Response
- Content-Type: application/pdf
- Binary PDF file with PMC challan format

## Integration Test

### Test Payment -> Challan Flow
1. Initiate payment via `/api/payment/initiate`
2. Complete payment (use BillDesk test/mock)
3. Payment callback triggers challan generation
4. Verify challan created automatically
5. Download and verify PDF format

## Verification Checklist

- [ ] Challan generates without errors
- [ ] PDF format matches PMC requirements  
- [ ] Marathi text displays correctly
- [ ] Dual copy layout (2 challans side by side)
- [ ] Database record created
- [ ] File saved to MediaStorage/Challans
- [ ] Download endpoint works
- [ ] Status endpoint returns correct info
- [ ] Auto-generation on payment success works
- [ ] Error handling works (invalid application ID)
- [ ] Plugin interface compatibility works

## Troubleshooting

### Common Issues

1. **Authorization Error (401)**
   - Check JWT token validity
   - Ensure token includes proper claims

2. **Application Not Found (400)**
   - Verify application ID exists in database
   - Check GUID format

3. **File Permission Error**
   - Ensure MediaStorage/Challans folder writable
   - Check OS permissions

4. **Font Rendering Issues** 
   - Marathi text may show as boxes if fonts missing
   - Add Mangal.ttf to Fonts folder for proper rendering

5. **PDF Generation Error**
   - Check QuestPDF version compatibility
   - Verify SkiaSharp native libraries installed

### Debug Steps

1. Check application logs for detailed error messages
2. Verify database migration applied (Challans table exists)
3. Test with simple English text first
4. Check file system permissions
5. Validate input data format (dates, numbers)