# BillDesk Payment Integration - Testing Guide

## Quick Test Script

You can test the payment integration using the following curl commands or Swagger UI.

### 1. Test Payment Initiation (New API)
```bash
curl -X POST "https://localhost:7001/api/payment/initiate" \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     -d '{
       "entityId": "YOUR_APPLICATION_GUID"
     }'
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Payment initiated successfully",
  "transactionId": "123456789012",
  "txnEntityId": "guid-string",
  "bdOrderId": "BD20241011123456",
  "rData": "RDATA123456789",
  "paymentGatewayUrl": "https://pay.billdesk.com/web/v1_2/embeddedsdk"
}
```

### 2. Test Payment Initiation (Legacy API)
```bash
curl -X POST "https://localhost:7001/api/payment/initialize" \
     -H "Content-Type: application/json" \
     -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     -d '{
       "applicationId": "YOUR_APPLICATION_GUID"
     }'
```

### 3. Test Payment View (Legacy)
```bash
curl -X GET "https://localhost:7001/api/payment/view/YOUR_APPLICATION_GUID"
```

### 4. Frontend Integration Test

Create a test HTML file to verify the payment flow:

```html
<!DOCTYPE html>
<html>
<head>
    <title>Payment Test</title>
</head>
<body>
    <h1>BillDesk Payment Test</h1>
    <button onclick="initiatePayment()">Test Payment</button>
    
    <script>
        async function initiatePayment() {
            try {
                const response = await fetch('/api/payment/initiate', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'Authorization': 'Bearer YOUR_JWT_TOKEN'
                    },
                    body: JSON.stringify({
                        entityId: 'YOUR_APPLICATION_GUID'
                    })
                });
                
                const result = await response.json();
                console.log('Payment Response:', result);
                
                if (result.success) {
                    // Auto-submit payment form
                    const form = document.createElement('form');
                    form.method = 'POST';
                    form.action = result.paymentGatewayUrl;
                    
                    const fields = [
                        { name: 'merchantid', value: 'PMCBLDGNV2' },
                        { name: 'bdorderid', value: result.bdOrderId },
                        { name: 'rdata', value: result.rData }
                    ];
                    
                    fields.forEach(field => {
                        const input = document.createElement('input');
                        input.type = 'hidden';
                        input.name = field.name;
                        input.value = field.value;
                        form.appendChild(input);
                    });
                    
                    document.body.appendChild(form);
                    form.submit();
                }
            } catch (error) {
                console.error('Payment error:', error);
            }
        }
    </script>
</body>
</html>
```

## Verification Checklist

### Configuration ✅
- [x] BillDesk credentials configured in appsettings
- [x] Environment variables setup (production)
- [x] User secrets configured (development)
- [x] CORS configured for frontend domain

### Services ✅
- [x] IBillDeskConfigService registered
- [x] IBillDeskPaymentService registered  
- [x] IPluginContextService registered
- [x] PaymentService updated with BillDesk integration

### API Endpoints ✅
- [x] `/api/payment/initiate` (New BillDesk API)
- [x] `/api/payment/initialize` (Legacy compatibility)
- [x] `/api/payment/view/{id}` (Legacy payment view)
- [x] `/api/payment/callback/{id}` (New callback handler)
- [x] `/api/payment/success/{id}` (Legacy success handler)

### Database ✅
- [x] Transaction model supports BillDesk fields
- [x] Application model has required payment fields
- [x] Entity relationships configured

### Security ✅
- [x] JWT authentication required
- [x] Secure credential storage
- [x] Input validation
- [x] Error handling
- [x] Logging without sensitive data

### Error Handling ✅
- [x] Comprehensive exception handling
- [x] User-friendly error messages
- [x] Detailed logging for debugging
- [x] Fallback mechanisms

## Testing Scenarios

### 1. Success Flow
1. Initialize payment with valid application ID
2. Verify transaction record created
3. Verify BillDesk parameters returned
4. Submit payment form to BillDesk
5. Handle success callback
6. Verify application status updated

### 2. Error Scenarios
1. Invalid application ID
2. Missing required fields
3. BillDesk service unavailable
4. Invalid credentials
5. Network timeout
6. Payment failure callback

### 3. Security Tests
1. Unauthorized access attempts
2. Invalid JWT tokens
3. SQL injection attempts
4. Cross-site scripting prevention
5. CORS policy validation

## Monitoring Points

Monitor these metrics in production:
- Payment success rate
- API response times
- Error frequencies
- BillDesk API availability
- Database performance
- Memory usage

## Troubleshooting

### Common Issues

1. **Configuration Error**
   - Check appsettings.json values
   - Verify environment variables
   - Validate BillDesk credentials

2. **Service Registration Error**
   - Check Program.cs service registrations
   - Verify dependency injection setup
   - Check for circular dependencies

3. **Database Connection Error**
   - Verify connection string
   - Check database server availability
   - Validate migration status

4. **BillDesk API Error**
   - Check network connectivity
   - Verify API endpoints
   - Validate request format
   - Check BillDesk service status

### Log Analysis

Key log entries to monitor:
```
[INFO] Initiating payment for EntityId: {entityId}
[INFO] Generated TransactionId: {transactionId} 
[INFO] Calling BillDesk encryption
[INFO] Calling BillDesk payment API
[INFO] Decrypting payment response
[INFO] Extracted BdOrderId: {bdOrderId}
[ERROR] Error initiating payment: {error}
```

## Performance Optimization

1. **Caching**
   - Cache BillDesk configuration
   - Cache encryption keys (securely)
   - Implement response caching where appropriate

2. **Database**
   - Index frequently queried fields
   - Optimize transaction queries
   - Use connection pooling

3. **HTTP Clients**
   - Configure connection timeout
   - Implement retry policies
   - Use HttpClientFactory

4. **Memory Management**
   - Dispose resources properly
   - Avoid memory leaks
   - Monitor garbage collection

## Production Readiness

The implementation includes:
- ✅ Production security practices
- ✅ Comprehensive error handling
- ✅ Detailed logging
- ✅ Environment-based configuration
- ✅ Legacy compatibility
- ✅ Transaction tracking
- ✅ Input validation
- ✅ Performance optimizations