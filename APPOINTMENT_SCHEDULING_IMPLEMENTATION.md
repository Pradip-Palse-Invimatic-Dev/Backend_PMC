# Appointment Scheduling Implementation

## Overview
This document describes the implementation of the appointment scheduling feature for the PMCRMS system. This feature allows Junior Engineers to schedule document verification appointments for applicants after their application submission.

## Implementation Details

### 1. Flow Description
1. **Application Submission**: End user submits application → Application stage becomes `JUNIOR_ENGINEER_PENDING`
2. **Appointment Scheduling**: Junior Engineer schedules appointment → Stage changes to `DOCUMENT_VERIFICATION_PENDING`
3. **Email Notification**: Applicant receives detailed appointment information via email

### 2. API Endpoint

**Endpoint**: `POST /api/Application/schedule-appointment`  
**Authorization**: Required (JWT Bearer token)

**Request Body** (`ScheduleAppointmentRequest`):
```json
{
  "applicationId": "guid",
  "comments": "string",
  "reviewDate": "2024-10-15T10:00:00Z",
  "contactPerson": "string",
  "place": "string",
  "roomNumber": "string"
}
```

**Response** (Success):
```json
{
  "success": true,
  "message": "Appointment scheduled successfully. The applicant has been notified via email and the application stage has been updated to Document Verification Pending."
}
```

**Response** (Error):
```json
{
  "success": false,
  "message": "Error message"
}
```

### 3. Business Logic

#### Authorization Rules
- **Who can schedule**: Only Junior Engineers (roles: JuniorArchitect, JuniorStructuralEngineer, JuniorLicenceEngineer, JuniorSupervisor1, JuniorSupervisor2)
- **Application stage check**: Application must be in `JUNIOR_ENGINEER_PENDING` stage
- **Position type matching**: Junior Engineer can only schedule for their specific category (e.g., JuniorArchitect can only handle Architect applications)

#### Stage Transition
- **From**: `JUNIOR_ENGINEER_PENDING`
- **To**: `DOCUMENT_VERIFICATION_PENDING`
- **Application Status**: Updated to `AppointmentScheduled`

#### Data Storage
The appointment is stored in the `Appointments` table with the following information:
- ApplicationId (foreign key)
- AppointmentDate
- Status (Scheduled)
- Comments
- ContactPerson
- Place
- RoomNumber
- ScheduledByOfficerId (who scheduled the appointment)
- Created/Modified audit fields

### 4. Email Notification

The system automatically sends a professional HTML email to the applicant containing:
- **Appointment details**: Date, time, venue, room number, contact person
- **Instructions**: What to bring, arrival time, contact information
- **Application information**: Application number, current stage
- **Branding**: PMC styling and footer

### 5. Error Handling

The implementation includes comprehensive error handling for:
- **Validation errors**: Invalid input data
- **Authorization errors**: User not authorized to schedule appointments
- **Business rule violations**: Wrong application stage, position type mismatch
- **Database errors**: Application not found, data persistence issues
- **Email errors**: Email sending failures (doesn't block appointment creation)

### 6. Security Features

- **JWT Authentication**: Required for all requests
- **Role-based Authorization**: Only Junior Engineers can access
- **Position-based Access Control**: Engineers can only handle their specific categories
- **Data Validation**: All input data is validated
- **Audit Trail**: All actions are logged with user and timestamp

### 7. Dependencies

#### Required Services
- **ApplicationService**: Core business logic
- **EmailService**: Email notifications  
- **ApplicationDbContext**: Database operations
- **ILogger**: Logging functionality
- **IMapper**: Object mapping

#### Database Tables
- **Applications**: Updated with new stage
- **Appointments**: New appointment record
- **Users/Officers**: User authentication and role verification

### 8. Configuration

#### Email Configuration
Ensure `SmtpClient` section is properly configured in appsettings.json:
```json
{
  "SmtpClient": {
    "Server": "smtp.server.com",
    "Port": "587",
    "User": "user@domain.com",
    "Password": "password",
    "FromName": "PMCRMS System",
    "SocketOptions": "3"
  }
}
```

### 9. Testing

#### Test Scenarios
1. **Success Case**: Valid Junior Engineer scheduling appointment for correct application
2. **Authorization Failures**: Non-Junior roles, wrong position type
3. **Validation Errors**: Missing required fields, invalid dates
4. **Business Rule Violations**: Wrong application stage
5. **Database Errors**: Non-existent application ID

#### Sample Request
```bash
curl -X POST "https://api.pmcrms.com/api/Application/schedule-appointment" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "applicationId": "123e4567-e89b-12d3-a456-426614174000",
    "comments": "Please bring all original documents for verification",
    "reviewDate": "2024-10-20T10:00:00Z",
    "contactPerson": "Mr. John Doe",
    "place": "PMC Office, Room 301",
    "roomNumber": "301"
  }'
```

### 10. Future Enhancements

Potential improvements could include:
- **Calendar Integration**: Sync with officer calendars
- **SMS Notifications**: Additional notification channel
- **Appointment Rescheduling**: Allow rescheduling by officers or applicants
- **Reminder System**: Automated reminders before appointments
- **Appointment History**: Track all appointment changes
- **Bulk Scheduling**: Schedule multiple appointments at once

## Files Modified/Created

### New Files
- `APPOINTMENT_SCHEDULING_IMPLEMENTATION.md` (this documentation)

### Modified Files
1. **ApplicationViewModel.cs**: Added `ScheduleAppointmentRequest` class
2. **ApplicationService.cs**: Added `ScheduleAppointmentAsync()` method and email notification logic
3. **ApplicationController.cs**: Added `schedule-appointment` endpoint

### Existing Infrastructure Used
- **Appointment.cs** model (already existed)
- **ApplicationDbContext.cs** (Appointments DbSet already configured)
- **EmailService.cs** (existing service)
- **Program.cs** (EmailService already registered)

## Conclusion

The appointment scheduling feature has been successfully implemented with comprehensive error handling, security measures, and professional email notifications. The implementation follows the existing codebase patterns and integrates seamlessly with the current system architecture.