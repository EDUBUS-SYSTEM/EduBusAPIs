# PayOS Real Payment Testing Script
# This script helps test real payment scenarios with PayOS

param(
    [string]$BaseUrl = "https://localhost:7223",
    [string]$JwtToken = "",
    [string]$Environment = "development", # development or production
    [switch]$CreateTransaction,
    [switch]$GenerateQR,
    [switch]$TestWebhook,
    [switch]$MonitorLogs,
    [switch]$Verbose
)

Write-Host "PayOS Real Payment Testing Script" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow

# Check if JWT token is provided
if ([string]::IsNullOrEmpty($JwtToken)) {
    Write-Host "Error: JWT token is required. Please provide it using -JwtToken parameter" -ForegroundColor Red
    Write-Host "Usage: .\TestRealPayment.ps1 -JwtToken 'your-jwt-token-here' -CreateTransaction" -ForegroundColor Yellow
    exit 1
}

# Headers
$headers = @{
    "Authorization" = "Bearer $JwtToken"
    "Content-Type" = "application/json"
}

# Global variables
$global:TransactionId = $null
$global:TransactionCode = $null

function Show-Menu {
    Write-Host "`nPayOS Payment Testing Menu" -ForegroundColor Cyan
    Write-Host "=========================" -ForegroundColor Cyan
    Write-Host "1. Create Transaction" -ForegroundColor White
    Write-Host "2. Generate QR Code" -ForegroundColor White
    Write-Host "3. Test Webhook" -ForegroundColor White
    Write-Host "4. Monitor Transaction Status" -ForegroundColor White
    Write-Host "5. Check Transaction Events" -ForegroundColor White
    Write-Host "6. Test Payment Return URL" -ForegroundColor White
    Write-Host "7. Test Payment Cancel URL" -ForegroundColor White
    Write-Host "8. Show Current Transaction Info" -ForegroundColor White
    Write-Host "9. Exit" -ForegroundColor White
    Write-Host "`nCurrent Transaction ID: $global:TransactionId" -ForegroundColor Yellow
    Write-Host "Current Transaction Code: $global:TransactionCode" -ForegroundColor Yellow
}

function Create-Transaction {
    Write-Host "`nCreating Transaction..." -ForegroundColor Cyan
    
    $transactionData = @{
        pickupPointRequestId = "PPR_TEST_$(Get-Date -Format 'yyyyMMddHHmmss')"
        scheduleId = [System.Guid]::NewGuid().ToString()
    }
    
    try {
        $body = $transactionData | ConvertTo-Json
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/payment" -Method POST -Headers $headers -Body $body
        
        $global:TransactionId = $response.id
        $global:TransactionCode = $response.transactionCode
        
        Write-Host "✅ Transaction created successfully" -ForegroundColor Green
        Write-Host "Transaction ID: $($response.id)" -ForegroundColor White
        Write-Host "Transaction Code: $($response.transactionCode)" -ForegroundColor White
        Write-Host "Amount: $($response.amount) VND" -ForegroundColor White
        Write-Host "Status: $($response.status)" -ForegroundColor White
        
        if ($Verbose) {
            $response | ConvertTo-Json -Depth 10 | Write-Host
        }
    }
    catch {
        Write-Host "❌ Error creating transaction: $($_.Exception.Message)" -ForegroundColor Red
        if ($Verbose) {
            $_.Exception | Format-List | Write-Host
        }
    }
}

function Generate-QRCode {
    if ([string]::IsNullOrEmpty($global:TransactionId)) {
        Write-Host "❌ No transaction ID available. Please create a transaction first." -ForegroundColor Red
        return
    }
    
    Write-Host "`nGenerating QR Code for Transaction: $global:TransactionId" -ForegroundColor Cyan
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/payment/$global:TransactionId/qrcode" -Method POST -Headers $headers
        
        Write-Host "✅ QR Code generated successfully" -ForegroundColor Green
        Write-Host "QR Code: $($response.qrCode)" -ForegroundColor White
        Write-Host "Checkout URL: $($response.checkoutUrl)" -ForegroundColor White
        Write-Host "Expires At: $($response.expiresAt)" -ForegroundColor White
        
        # Save QR code to file
        $qrCodeData = $response.qrCode -replace "data:image/png;base64,", ""
        $qrCodeBytes = [System.Convert]::FromBase64String($qrCodeData)
        $qrCodePath = "qr_code_$global:TransactionId.png"
        [System.IO.File]::WriteAllBytes($qrCodePath, $qrCodeBytes)
        Write-Host "QR Code saved to: $qrCodePath" -ForegroundColor Green
        
        # Open QR code image
        if (Test-Path $qrCodePath) {
            Start-Process $qrCodePath
        }
        
        if ($Verbose) {
            $response | ConvertTo-Json -Depth 10 | Write-Host
        }
    }
    catch {
        Write-Host "❌ Error generating QR code: $($_.Exception.Message)" -ForegroundColor Red
        if ($Verbose) {
            $_.Exception | Format-List | Write-Host
        }
    }
}

function Test-Webhook {
    Write-Host "`nTesting Webhook..." -ForegroundColor Cyan
    
    $webhookPayload = @{
        code = "00"
        desc = "success"
        success = $true
        data = @{
            orderCode = if ($global:TransactionCode) { [long]$global:TransactionCode.Replace("TXN_", "").Replace("_", "") } else { 123456789 }
            amount = 150000
            description = "Test payment"
            accountNumber = "1234567890"
            reference = if ($global:TransactionCode) { $global:TransactionCode } else { "TXN_TEST_001" }
            transactionDateTime = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
            currency = "VND"
            paymentLinkId = "pay_123456789"
            code = "00"
            desc = "Thành công"
            counterAccountBankId = ""
            counterAccountBankName = ""
            counterAccountName = ""
            counterAccountNumber = ""
            virtualAccountName = ""
            virtualAccountNumber = ""
        }
        signature = "test_signature_123456789"
    }
    
    try {
        $body = $webhookPayload | ConvertTo-Json -Depth 10
        $webhookHeaders = @{
            "Content-Type" = "application/json"
        }
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/payment/webhook/payos" -Method POST -Headers $webhookHeaders -Body $body
        
        Write-Host "✅ Webhook test completed" -ForegroundColor Green
        Write-Host "Response: $($response.message)" -ForegroundColor White
        
        if ($Verbose) {
            $response | ConvertTo-Json -Depth 10 | Write-Host
        }
    }
    catch {
        Write-Host "❌ Error testing webhook: $($_.Exception.Message)" -ForegroundColor Red
        if ($Verbose) {
            $_.Exception | Format-List | Write-Host
        }
    }
}

function Monitor-TransactionStatus {
    if ([string]::IsNullOrEmpty($global:TransactionId)) {
        Write-Host "❌ No transaction ID available. Please create a transaction first." -ForegroundColor Red
        return
    }
    
    Write-Host "`nMonitoring Transaction Status..." -ForegroundColor Cyan
    Write-Host "Transaction ID: $global:TransactionId" -ForegroundColor White
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/payment/$global:TransactionId" -Method GET -Headers $headers
        
        Write-Host "✅ Transaction status retrieved" -ForegroundColor Green
        Write-Host "Status: $($response.status)" -ForegroundColor White
        Write-Host "Amount: $($response.amount) VND" -ForegroundColor White
        Write-Host "Provider: $($response.provider)" -ForegroundColor White
        Write-Host "Created At: $($response.createdAt)" -ForegroundColor White
        Write-Host "Paid At: $($response.paidAtUtc)" -ForegroundColor White
        
        if ($Verbose) {
            $response | ConvertTo-Json -Depth 10 | Write-Host
        }
    }
    catch {
        Write-Host "❌ Error monitoring transaction: $($_.Exception.Message)" -ForegroundColor Red
        if ($Verbose) {
            $_.Exception | Format-List | Write-Host
        }
    }
}

function Get-TransactionEvents {
    if ([string]::IsNullOrEmpty($global:TransactionId)) {
        Write-Host "❌ No transaction ID available. Please create a transaction first." -ForegroundColor Red
        return
    }
    
    Write-Host "`nGetting Transaction Events..." -ForegroundColor Cyan
    Write-Host "Transaction ID: $global:TransactionId" -ForegroundColor White
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/payment/$global:TransactionId/events" -Method GET -Headers $headers
        
        Write-Host "✅ Transaction events retrieved" -ForegroundColor Green
        Write-Host "Number of events: $($response.Count)" -ForegroundColor White
        
        foreach ($event in $response) {
            Write-Host "Event: $($event.status) - $($event.message) at $($event.atUtc)" -ForegroundColor White
        }
        
        if ($Verbose) {
            $response | ConvertTo-Json -Depth 10 | Write-Host
        }
    }
    catch {
        Write-Host "❌ Error getting transaction events: $($_.Exception.Message)" -ForegroundColor Red
        if ($Verbose) {
            $_.Exception | Format-List | Write-Host
        }
    }
}

function Test-PaymentReturn {
    if ([string]::IsNullOrEmpty($global:TransactionCode)) {
        Write-Host "❌ No transaction code available. Please create a transaction first." -ForegroundColor Red
        return
    }
    
    Write-Host "`nTesting Payment Return URL..." -ForegroundColor Cyan
    
    $orderCode = $global:TransactionCode.Replace("TXN_", "").Replace("_", "")
    $returnUrl = "$BaseUrl/api/payment/return?code=00&id=123456789&cancel=false&status=success&orderCode=$orderCode"
    
    try {
        $response = Invoke-RestMethod -Uri $returnUrl -Method GET
        
        Write-Host "✅ Payment return test completed" -ForegroundColor Green
        Write-Host "Success: $($response.success)" -ForegroundColor White
        Write-Host "Message: $($response.message)" -ForegroundColor White
        Write-Host "Status: $($response.status)" -ForegroundColor White
        
        if ($Verbose) {
            $response | ConvertTo-Json -Depth 10 | Write-Host
        }
    }
    catch {
        Write-Host "❌ Error testing payment return: $($_.Exception.Message)" -ForegroundColor Red
        if ($Verbose) {
            $_.Exception | Format-List | Write-Host
        }
    }
}

function Test-PaymentCancel {
    if ([string]::IsNullOrEmpty($global:TransactionCode)) {
        Write-Host "❌ No transaction code available. Please create a transaction first." -ForegroundColor Red
        return
    }
    
    Write-Host "`nTesting Payment Cancel URL..." -ForegroundColor Cyan
    
    $orderCode = $global:TransactionCode.Replace("TXN_", "").Replace("_", "")
    $cancelUrl = "$BaseUrl/api/payment/cancel?code=01&id=123456789&cancel=true&status=cancelled&orderCode=$orderCode"
    
    try {
        $response = Invoke-RestMethod -Uri $cancelUrl -Method GET
        
        Write-Host "✅ Payment cancel test completed" -ForegroundColor Green
        Write-Host "Success: $($response.success)" -ForegroundColor White
        Write-Host "Message: $($response.message)" -ForegroundColor White
        Write-Host "Status: $($response.status)" -ForegroundColor White
        
        if ($Verbose) {
            $response | ConvertTo-Json -Depth 10 | Write-Host
        }
    }
    catch {
        Write-Host "❌ Error testing payment cancel: $($_.Exception.Message)" -ForegroundColor Red
        if ($Verbose) {
            $_.Exception | Format-List | Write-Host
        }
    }
}

function Show-TransactionInfo {
    Write-Host "`nCurrent Transaction Information" -ForegroundColor Cyan
    Write-Host "=============================" -ForegroundColor Cyan
    Write-Host "Transaction ID: $global:TransactionId" -ForegroundColor White
    Write-Host "Transaction Code: $global:TransactionCode" -ForegroundColor White
    Write-Host "Base URL: $BaseUrl" -ForegroundColor White
    Write-Host "Environment: $Environment" -ForegroundColor White
}

function Start-Monitoring {
    Write-Host "`nStarting Transaction Monitoring..." -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop monitoring" -ForegroundColor Yellow
    
    while ($true) {
        if (-not [string]::IsNullOrEmpty($global:TransactionId)) {
            Monitor-TransactionStatus
            Start-Sleep -Seconds 10
        } else {
            Write-Host "No transaction to monitor. Please create a transaction first." -ForegroundColor Yellow
            break
        }
    }
}

# Main execution
if ($CreateTransaction) {
    Create-Transaction
    exit 0
}

if ($GenerateQR) {
    Generate-QRCode
    exit 0
}

if ($TestWebhook) {
    Test-Webhook
    exit 0
}

if ($MonitorLogs) {
    Start-Monitoring
    exit 0
}

# Interactive mode
do {
    Show-Menu
    $choice = Read-Host "`nEnter your choice (1-9)"
    
    switch ($choice) {
        "1" { Create-Transaction }
        "2" { Generate-QRCode }
        "3" { Test-Webhook }
        "4" { Monitor-TransactionStatus }
        "5" { Get-TransactionEvents }
        "6" { Test-PaymentReturn }
        "7" { Test-PaymentCancel }
        "8" { Show-TransactionInfo }
        "9" { 
            Write-Host "Exiting..." -ForegroundColor Green
            break 
        }
        default { 
            Write-Host "Invalid choice. Please try again." -ForegroundColor Red 
        }
    }
    
    if ($choice -ne "9") {
        Read-Host "`nPress Enter to continue..."
    }
} while ($choice -ne "9")

Write-Host "`nTest completed!" -ForegroundColor Green
Write-Host "For more detailed output, use -Verbose parameter" -ForegroundColor Yellow

