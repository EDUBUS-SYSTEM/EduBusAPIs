# PayOS Signature Validation Test Script
# This script tests the PayOS signature validation implementation

param(
    [string]$BaseUrl = "https://localhost:7223",
    [string]$JwtToken = "",
    [switch]$Verbose
)

Write-Host "PayOS Signature Validation Test Script" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Check if JWT token is provided
if ([string]::IsNullOrEmpty($JwtToken)) {
    Write-Host "Error: JWT token is required. Please provide it using -JwtToken parameter" -ForegroundColor Red
    Write-Host "Usage: .\TestPayOSSignature.ps1 -JwtToken 'your-jwt-token-here'" -ForegroundColor Yellow
    exit 1
}

# Test endpoints
$endpoints = @(
    @{
        Name = "Test Signature with Sample Data"
        Method = "POST"
        Url = "$BaseUrl/api/PayOSTest/test-signature"
        Body = $null
    },
    @{
        Name = "Test Signature Generation with Custom Data"
        Method = "POST"
        Url = "$BaseUrl/api/PayOSTest/test-signature-custom"
        Body = @{
            orderCode = 123
            amount = 3000
            description = "VQRIO123"
            accountNumber = "12345678"
            reference = "TF230204212323"
            transactionDateTime = "2023-02-04 18:25:00"
            currency = "VND"
            paymentLinkId = "124c33293c43417ab7879e14c8d9eb18"
            code = "00"
            desc = "Thành công"
            counterAccountBankId = ""
            counterAccountBankName = ""
            counterAccountName = ""
            counterAccountNumber = ""
            virtualAccountName = ""
            virtualAccountNumber = ""
        }
    },
    @{
        Name = "Test Signature Verification"
        Method = "POST"
        Url = "$BaseUrl/api/PayOSTest/test-signature-verify"
        Body = @{
            data = @{
                orderCode = 123
                amount = 3000
                description = "VQRIO123"
                accountNumber = "12345678"
                reference = "TF230204212323"
                transactionDateTime = "2023-02-04 18:25:00"
                currency = "VND"
                paymentLinkId = "124c33293c43417ab7879e14c8d9eb18"
                code = "00"
                desc = "Thành công"
                counterAccountBankId = ""
                counterAccountBankName = ""
                counterAccountName = ""
                counterAccountNumber = ""
                virtualAccountName = ""
                virtualAccountNumber = ""
            }
            signature = "412e915d2871504ed31be63c8f62a149a4410d34c4c42affc9006ef9917eaa03"
        }
    },
    @{
        Name = "Get PayOS Configuration"
        Method = "GET"
        Url = "$BaseUrl/api/PayOSTest/config"
        Body = $null
    }
)

# Headers
$headers = @{
    "Authorization" = "Bearer $JwtToken"
    "Content-Type" = "application/json"
}

# Test each endpoint
foreach ($endpoint in $endpoints) {
    Write-Host "`nTesting: $($endpoint.Name)" -ForegroundColor Cyan
    Write-Host "URL: $($endpoint.Url)" -ForegroundColor Gray
    
    try {
        if ($endpoint.Method -eq "GET") {
            $response = Invoke-RestMethod -Uri $endpoint.Url -Method GET -Headers $headers
        } else {
            $body = if ($endpoint.Body) { $endpoint.Body | ConvertTo-Json -Depth 10 } else { $null }
            $response = Invoke-RestMethod -Uri $endpoint.Url -Method POST -Headers $headers -Body $body
        }
        
        Write-Host "✅ Success" -ForegroundColor Green
        
        if ($Verbose) {
            Write-Host "Response:" -ForegroundColor Yellow
            $response | ConvertTo-Json -Depth 10 | Write-Host
        } else {
            # Show key information
            if ($response.success -ne $null) {
                Write-Host "Success: $($response.success)" -ForegroundColor $(if ($response.success) { "Green" } else { "Red" })
            }
            if ($response.isValid -ne $null) {
                Write-Host "Valid: $($response.isValid)" -ForegroundColor $(if ($response.isValid) { "Green" } else { "Red" })
            }
            if ($response.message) {
                Write-Host "Message: $($response.message)" -ForegroundColor White
            }
        }
    }
    catch {
        Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($Verbose) {
            Write-Host "Full Error:" -ForegroundColor Red
            $_.Exception | Format-List | Write-Host
        }
    }
}

# Test webhook endpoint (no authentication required)
Write-Host "`nTesting: PayOS Webhook Endpoint" -ForegroundColor Cyan
Write-Host "URL: $BaseUrl/api/Payment/webhook/payos" -ForegroundColor Gray

$webhookPayload = @{
    code = "00"
    desc = "success"
    success = $true
    data = @{
        orderCode = 123
        amount = 3000
        description = "VQRIO123"
        accountNumber = "12345678"
        reference = "TF230204212323"
        transactionDateTime = "2023-02-04 18:25:00"
        currency = "VND"
        paymentLinkId = "124c33293c43417ab7879e14c8d9eb18"
        code = "00"
        desc = "Thành công"
        counterAccountBankId = ""
        counterAccountBankName = ""
        counterAccountName = ""
        counterAccountNumber = ""
        virtualAccountName = ""
        virtualAccountNumber = ""
    }
    signature = "412e915d2871504ed31be63c8f62a149a4410d34c4c42affc9006ef9917eaa03"
}

try {
    $webhookHeaders = @{
        "Content-Type" = "application/json"
    }
    
    $webhookBody = $webhookPayload | ConvertTo-Json -Depth 10
    $webhookResponse = Invoke-RestMethod -Uri "$BaseUrl/api/Payment/webhook/payos" -Method POST -Headers $webhookHeaders -Body $webhookBody
    
    Write-Host "✅ Webhook Test Success" -ForegroundColor Green
    
    if ($Verbose) {
        Write-Host "Webhook Response:" -ForegroundColor Yellow
        $webhookResponse | ConvertTo-Json -Depth 10 | Write-Host
    } else {
        if ($webhookResponse.message) {
            Write-Host "Message: $($webhookResponse.message)" -ForegroundColor White
        }
    }
}
catch {
    Write-Host "❌ Webhook Test Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($Verbose) {
        Write-Host "Full Error:" -ForegroundColor Red
        $_.Exception | Format-List | Write-Host
    }
}

Write-Host "`nTest completed!" -ForegroundColor Green
Write-Host "For more detailed output, use -Verbose parameter" -ForegroundColor Yellow

