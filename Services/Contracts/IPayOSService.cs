using Services.Models.Payment;

namespace Services.Contracts;

public interface IPayOSService
{
    Task<PayOSCreatePaymentResponse> CreatePaymentAsync(PayOSCreatePaymentRequest request);
    Task<PayOSPaymentResponse> GetPaymentInfoAsync(string orderCode);
    Task<bool> VerifyWebhookSignatureAsync(string signature, string payload);
    Task<PayOSWebhookData> VerifyWebhookDataAsync(PayOSWebhookPayload webhookPayload);
    Task<string> CancelPaymentLinkAsync(long orderCode, string? cancellationReason = null);
    Task<string> ConfirmWebhookAsync(string webhookUrl);
}