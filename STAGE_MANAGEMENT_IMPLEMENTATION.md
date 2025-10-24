# Application Stage Management System Implementation

## Overview
I have successfully implemented a comprehensive application stage management system for the PMC project. This system handles multi-stage approvals, appointment scheduling, digital signatures, and certificate generation.

## New Features Added

### 1. Application Stage Management
- **Update Application Stage**: Officers can move applications through different approval stages
- **Role-based Stage Access**: Different officer roles can only update applications at their respective stages
- **Stage Validation**: Proper validation ensures applications can only move through valid stage transitions

### 2. Appointment Scheduling
- **Schedule Appointments**: Officers can schedule appointments for applicants
- **Appointment Details**: Includes date, location, room number, and contact person
- **Status Tracking**: Appointments have their own status system (Scheduled, Completed, Cancelled, Rescheduled)

### 3. Digital Signature System
- **OTP Generation**: Secure OTP generation for digital signature authentication
- **OTP Verification**: Verify OTP before applying digital signatures
- **Digital Signature Application**: Officers can digitally sign documents after OTP verification
- **Audit Trail**: Complete tracking of who signed what and when

### 4. Certificate Generation
- **Certificate Creation**: Generate certificates for approved applications
- **Certificate Numbering**: Automatic sequential certificate number generation (PMC/CERT/YYYY/XXXXX format)
- **Validation**: Only approved and digitally signed applications can get certificates

## API Endpoints Implemented

### Application Stage Management
- `POST /api/application/update-stage` - Update application stage
- `POST /api/application/schedule-appointment` - Schedule appointment
- `POST /api/application/generate-otp` - Generate OTP for digital signature
- `POST /api/application/apply-digital-signature` - Apply digital signature
- `POST /api/application/generate-certificate` - Generate certificate

## New Models Added

### ApplicationStageViewModel.cs
```csharp
- UpdateApplicationStageViewModel
- ScheduleAppointmentViewModel  
- GenerateOtpViewModel
- ApplyDigitalSignatureViewModel
- GenerateCertificateViewModel
- StageUpdateResponse
```

### Enhanced Application Model
Added properties for digital signatures and certificates:
```csharp
- IsDigitallySigned
- DigitalSignatureDate
- SignedBy
- CertificateNumber
- CertificateGeneratedDate
- CertificateGeneratedBy
```

### Enhanced OtpVerification Model
Extended for digital signature support:
```csharp
- ApplicationId
- Purpose
- IsUsed
- UsedAt
```

## Service Methods Added

### ApplicationService.cs
```csharp
- UpdateApplicationStageAsync()
- ScheduleAppointmentAsync()
- GenerateOtpForSignatureAsync()
- ApplyDigitalSignatureAsync()
- GenerateCertificateAsync()
- IsValidStageTransition()
- GenerateCertificateNumber()
```

## Stage Workflow

### 1. Application Stages
```
JUNIOR_ENGINEER_PENDING → ASSISTANT_ENGINEER_PENDING → 
EXECUTIVE_ENGINEER_PENDING → CITY_ENGINEER_PENDING → 
CLERK_PENDING → APPOINTMENT_SCHEDULED → APPROVED → 
DIGITALLY_SIGNED → CERTIFICATE_GENERATED
```

### 2. Officer Permissions
- **Junior Engineers**: Can approve to Assistant Engineer stage (position-specific)
- **Assistant Engineers**: Can approve to Executive Engineer stage (position-specific)
- **Executive Engineer**: Can approve to City Engineer stage (all applications)
- **City Engineer**: Can approve to Clerk stage (all applications)
- **Clerk**: Can schedule appointments (all applications)
- **Any Officer**: Can digitally sign and generate certificates (with proper validation)

### 3. Rejection Handling
Any officer can reject an application at their stage, setting status to `REJECTED`

## Digital Signature Workflow

1. **Generate OTP**: Officer requests OTP for specific application
2. **OTP Validation**: System generates 6-digit OTP valid for 10 minutes
3. **Apply Signature**: Officer provides OTP to apply digital signature
4. **Verification**: System validates OTP and marks application as digitally signed
5. **Certificate Ready**: Application becomes eligible for certificate generation

## Security Features

### Role-Based Access Control
- Officers can only see applications at their current stage
- Position-specific filtering for Junior/Assistant roles
- Higher-level roles can see all applications at their stage

### OTP Security
- 6-digit random OTP generation
- 10-minute expiration time
- One-time use only
- Purpose-specific (DIGITAL_SIGNATURE)
- Complete audit trail

### Stage Transition Validation
- Prevents invalid stage jumps
- Ensures proper workflow sequence
- Validates officer permissions

## Sample Usage

### Update Application Stage
```json
POST /api/application/update-stage
{
    "applicationId": "guid-here",
    "newStage": "ASSISTANT_ENGINEER_PENDING",
    "comments": "Documents verified and approved",
    "isApproved": true
}
```

### Schedule Appointment
```json
POST /api/application/schedule-appointment
{
    "applicationId": "guid-here",
    "comments": "Please bring original documents",
    "appointmentDate": "2024-01-15T10:00:00Z",
    "contactPerson": "John Doe",
    "place": "PMC Office Building A",
    "roomNumber": "101"
}
```

### Generate OTP for Digital Signature
```json
POST /api/application/generate-otp
{
    "applicationId": "guid-here"
}
```

### Apply Digital Signature
```json
POST /api/application/apply-digital-signature
{
    "applicationId": "guid-here",
    "otp": "123456"
}
```

### Generate Certificate
```json
POST /api/application/generate-certificate
{
    "applicationId": "guid-here"
}
```

## Database Changes

### New Enums Added
- `APPOINTMENT_SCHEDULED` stage
- `DIGITALLY_SIGNED` stage
- `CERTIFICATE_GENERATED` stage

### New Properties
- Application model extended for digital signature and certificate tracking
- OtpVerification model enhanced for multi-purpose usage
- Appointment model already had required properties

## Build Status
✅ **Build Successful** - All code compiles without errors
⚠️ **Warnings Only** - 31 warnings related to nullable reference types (non-breaking)

## Next Steps for Full Implementation

1. **Database Migration**: Run `dotnet ef migrations add ApplicationStageManagement` to create database migration
2. **Email Service Integration**: Connect OTP generation with actual SMS/Email sending
3. **PDF Certificate Generation**: Extend PdfService to generate certificates
4. **UI Integration**: Connect frontend to new API endpoints
5. **Testing**: Add unit and integration tests for new functionality

## Benefits

1. **Complete Workflow**: End-to-end application processing from submission to certificate
2. **Security**: Multi-factor authentication with OTP for digital signatures
3. **Audit Trail**: Complete tracking of all stage changes and actions
4. **Role-based Access**: Proper permission management based on officer roles
5. **Scalable**: Easy to add new stages or modify workflow as needed