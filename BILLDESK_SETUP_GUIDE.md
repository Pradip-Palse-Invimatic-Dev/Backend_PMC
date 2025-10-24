# BillDesk Payment Integration - Setup Guide

## Overview
This implementation provides seamless BillDesk payment gateway integration with production-ready security practices.

## Configuration

### 1. Environment Variables (Recommended for Production)
Set these environment variables in your hosting platform or CI/CD pipeline:

```bash
BILLDESK_MERCHANT_ID=PMCBLDGNV2
BILLDESK_ENCRYPTION_KEY=Emzgx5sOhNM0jLrUwfwh3HAbPq3m7BQh
BILLDESK_SIGNING_KEY=DIev9KTSQzfOW42eQ2AqCh5tvki9Dkir
BILLDESK_KEY_ID=joXzOLr441ZN
BILLDESK_CLIENT_ID=pmcbldgnv3
```

### 2. User Secrets (For Local Development)
```bash
dotnet user-secrets init
dotnet user-secrets set "BillDesk:MerchantId" "PMCBLDGNV2"
dotnet user-secrets set "BillDesk:EncryptionKey" "Emzgx5sOhNM0jLrUwfwh3HAbPq3m7BQh"
dotnet user-secrets set "BillDesk:SigningKey" "DIev9KTSQzfOW42eQ2AqCh5tvki9Dkir"
dotnet user-secrets set "BillDesk:KeyId" "joXzOLr441ZN"
dotnet user-secrets set "BillDesk:ClientId" "pmcbldgnv3"
```

### 3. Configuration Files
The configuration is already set in appsettings.json and appsettings.Development.json.

## API Endpoints

### 1. Initialize Payment (New)
```
POST /api/payment/initiate
```
**Request Body:**
```json
{
  "entityId": "guid-string"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Payment initiated successfully",
  "transactionId": "123456789012",
  "txnEntityId": "guid",
  "bdOrderId": "BD20241011123456",
  "rData": "RDATA123456789",
  "paymentGatewayUrl": "https://pay.billdesk.com/web/v1_2/embeddedsdk"
}
```

### 2. Initialize Payment (Legacy)
```
POST /api/payment/initialize
```
**Request Body:**
```json
{
  "applicationId": "guid"
}
```

### 3. Payment View (Legacy)
```
GET /api/payment/view/{applicationId}
```

### 4. Payment Callback
```
POST /api/payment/callback/{applicationId}
```

### 5. Payment Success (Legacy)
```
POST /api/payment/success/{applicationId}
```

## Frontend Integration

### Using the New API
```javascript
const initiatePayment = async (entityId) => {
  try {
    const response = await fetch('/api/payment/initiate', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ entityId })
    });
    
    const result = await response.json();
    
    if (result.success) {
      // Create and submit payment form
      const form = document.createElement('form');
      form.method = 'POST';
      form.action = result.paymentGatewayUrl;
      
      const merchantId = document.createElement('input');
      merchantId.type = 'hidden';
      merchantId.name = 'merchantid';
      merchantId.value = 'PMCBLDGNV2';
      
      const bdOrderId = document.createElement('input');
      bdOrderId.type = 'hidden';
      bdOrderId.name = 'bdorderid';
      bdOrderId.value = result.bdOrderId;
      
      const rData = document.createElement('input');
      rData.type = 'hidden';
      rData.name = 'rdata';
      rData.value = result.rData;
      
      form.appendChild(merchantId);
      form.appendChild(bdOrderId);
      form.appendChild(rData);
      
      document.body.appendChild(form);
      form.submit();
    } else {
      console.error('Payment initialization failed:', result.message);
    }
  } catch (error) {
    console.error('Payment error:', error);
  }
};
```

## Security Features

✅ **Implemented Security Measures:**
- Environment variable configuration for sensitive data
- User secrets for local development
- Proper error handling and logging
- Input validation
- Secure HTTP headers
- Production-ready encryption/decryption workflow

✅ **Best Practices:**
- No hardcoded credentials
- Proper dependency injection
- Comprehensive logging
- Error handling
- Transaction tracking
- Callback URL validation

## Services Architecture

### 1. IBillDeskConfigService
- Manages BillDesk configuration
- Validates required settings
- Provides secure access to credentials

### 2. IBillDeskPaymentService
- Handles payment initialization
- Manages encryption/decryption
- Processes API calls
- Extracts payment details

### 3. IPluginContextService
- Simulates external service dependencies
- Handles entity operations
- Manages service invocations
- Provides mock implementations for development

### 4. PaymentService (Legacy)
- Maintains backward compatibility
- Bridges old and new implementations
- Handles legacy payment processing

## Database Schema
The Transaction model includes all necessary fields for BillDesk integration:
- TransactionId
- Status
- Price
- ApplicationId
- FirstName, LastName
- Email, PhoneNumber
- EaseBuzzStatus, CardType, Mode
- CreatedAt, UpdatedAt

## Error Handling
- Comprehensive exception handling at all levels
- Detailed logging for debugging
- User-friendly error messages
- Graceful fallbacks for service failures

## Testing
- Mock implementations for development
- Configurable service endpoints
- Detailed logging for troubleshooting
- Separate development/production configurations

## Deployment Checklist

1. ✅ Set environment variables in hosting platform
2. ✅ Configure return URLs for production domain
3. ✅ Test payment flow in staging environment
4. ✅ Enable HTTPS/SSL certificates
5. ✅ Configure CORS for frontend domain
6. ✅ Set up monitoring and alerting
7. ✅ Test callback URL accessibility
8. ✅ Verify database connection and migrations

## Monitoring and Logging

The implementation includes comprehensive logging:
- Payment initialization events
- Encryption/decryption operations
- API call success/failures
- Transaction status updates
- Error conditions and exceptions

Monitor these log entries for:
- Payment success rates
- API response times
- Error patterns
- Security violations

## Support

For issues with BillDesk integration:
1. Check logs for detailed error messages
2. Verify configuration settings
3. Test callback URL accessibility
4. Validate SSL certificates
5. Contact BillDesk support if needed

## Version History

- **v1.0**: Initial BillDesk integration with production-ready security
- **v1.1**: Added legacy compatibility layer
- **v1.2**: Enhanced error handling and logging
- **v1.3**: Added comprehensive callback handling