using AutoMapper;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Contracts;
using Services.Models.Payment;

namespace Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransportFeeItemRepository _transportFeeItemRepository;
    private readonly IPaymentEventLogRepository _paymentEventLogRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IPickupPointRequestRepository _pickupPointRequestRepository;
    private readonly IPayOSService _payOSService;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;
    private readonly PayOSConfig _config;

    public PaymentService(
        ITransactionRepository transactionRepository,
        ITransportFeeItemRepository transportFeeItemRepository,
        IPaymentEventLogRepository paymentEventLogRepository,
        IStudentRepository studentRepository,
        IPickupPointRequestRepository pickupPointRequestRepository,
        IPayOSService payOSService,
        IMapper mapper,
        ILogger<PaymentService> logger,
        IOptions<PayOSConfig> config)
    {
        _transactionRepository = transactionRepository;
        _transportFeeItemRepository = transportFeeItemRepository;
        _paymentEventLogRepository = paymentEventLogRepository;
        _studentRepository = studentRepository;
        _pickupPointRequestRepository = pickupPointRequestRepository;
        _payOSService = payOSService;
        _mapper = mapper;
        _logger = logger;
        _config = config.Value;
    }

    public async Task<PagedTransactionResponse> GetTransactionsAsync(TransactionListRequest request)
    {
        try
        {
            var query = _transactionRepository.GetQueryable();

            // Apply filters
            if (request.Status.HasValue)
                query = query.Where(t => t.Status == request.Status.Value);

            if (request.ParentId.HasValue)
                query = query.Where(t => t.ParentId == request.ParentId.Value);

            if (request.From.HasValue)
                query = query.Where(t => t.CreatedAt >= request.From.Value);

            if (request.To.HasValue)
                query = query.Where(t => t.CreatedAt <= request.To.Value);

            // Get total count
            var total = await query.CountAsync();

            // Apply sorting
            query = request.SortBy.ToLower() switch
            {
                "amount" => request.SortOrder.ToLower() == "asc" 
                    ? query.OrderBy(t => t.Amount) 
                    : query.OrderByDescending(t => t.Amount),
                "status" => request.SortOrder.ToLower() == "asc" 
                    ? query.OrderBy(t => t.Status) 
                    : query.OrderByDescending(t => t.Status),
                _ => request.SortOrder.ToLower() == "asc" 
                    ? query.OrderBy(t => t.CreatedAt) 
                    : query.OrderByDescending(t => t.CreatedAt)
            };

            // Apply pagination
            var transactions = await query
                .Skip((request.Page - 1) * request.PerPage)
                .Take(request.PerPage)
                .ToListAsync();

            var transactionSummaries = _mapper.Map<IEnumerable<TransactionSummaryResponse>>(transactions);

            return new PagedTransactionResponse
            {
                Total = total,
                Page = request.Page,
                PerPage = request.PerPage,
                Data = transactionSummaries
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions with request: {@Request}", request);
            throw;
        }
    }

    public async Task<TransactionDetailResponse?> GetTransactionDetailAsync(Guid transactionId)
    {
        try
        {
            var transaction = await _transactionRepository.FindAsync(transactionId);
            if (transaction == null)
                return null;

            // Load related data
            var transportFeeItems = await _transportFeeItemRepository
                .FindByConditionAsync(tfi => tfi.TransactionId == transactionId);

            var response = _mapper.Map<TransactionDetailResponse>(transaction);
            
            // Map transport fee items with student names
            var itemsWithStudentNames = new List<TransportFeeItemResponse>();
            foreach (var item in transportFeeItems)
            {
                var student = await _studentRepository.FindAsync(item.StudentId);
                var itemResponse = _mapper.Map<TransportFeeItemResponse>(item);
                itemResponse.StudentName = student?.FirstName + " " + student?.LastName ?? "Unknown";
                itemsWithStudentNames.Add(itemResponse);
            }
            
            response.Items = itemsWithStudentNames;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction detail for ID: {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<Transaction?> GetTransactionByOrderCodeAsync(long orderCode)
    {
        try
        {
            var transactions = await _transactionRepository.FindByConditionAsync(t => t.TransactionCode == orderCode.ToString());
            return transactions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction by order code: {OrderCode}", orderCode);
            throw;
        }
    }

    public async Task<QrResponse> GenerateOrRefreshQrAsync(Guid transactionId)
    {
        try
        {
            if (transactionId == Guid.Empty)
                throw new ArgumentException("Transaction ID cannot be empty");

            var transaction = await _transactionRepository.FindAsync(transactionId);
            if (transaction == null)
                throw new ArgumentException("Transaction not found");

            if (transaction.Status != TransactionStatus.Notyet)
                throw new InvalidOperationException("Transaction is not in Notyet status");

            // Always generate new QR code for PayOS integration
            // Ensure numeric orderCode as PayOS requires
            long orderCode;
            if (!long.TryParse(transaction.TransactionCode, out orderCode))
            {
                orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                transaction.TransactionCode = orderCode.ToString();
                transaction.UpdatedAt = DateTime.UtcNow;
                await _transactionRepository.UpdateAsync(transaction);
            }

            // Generate QR using PayOS
            var payOSRequest = new PayOSCreatePaymentRequest
            {
                OrderCode = orderCode,
                Amount = (int)transaction.Amount,
                Description = transaction.Description,
                ReturnUrl = _config.ReturnUrl ?? "https://edubus.app/payment/success",
                CancelUrl = _config.CancelUrl ?? "https://edubus.app/payment/cancel",
                // Items optional; remove to satisfy PayOS validation for simple amount-only payments
                Items = Array.Empty<PayOSItem>()
            };

            var payOSResponse = await _payOSService.CreatePaymentAsync(payOSRequest);
            
            if (payOSResponse.Code != "00" || payOSResponse.Data == null)
            {
                throw new InvalidOperationException($"PayOS error: {payOSResponse.Desc}");
            }

            // Update transaction with PayOS data
            transaction.ProviderTransactionId = payOSResponse.Data.PaymentLinkId;
            transaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository.UpdateAsync(transaction);

            // Log event
            await LogPaymentEventAsync(transactionId, TransactionStatus.Notyet, 
                PaymentEventSource.manual, "QR code generated/refreshed");

            return new QrResponse
            {
                QrCode = payOSResponse.Data.QrCode,
                CheckoutUrl = payOSResponse.Data.CheckoutUrl,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_config.QrExpirationMinutes)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating/refreshing QR for transaction: {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<IEnumerable<PaymentEventResponse>> GetTransactionEventsAsync(Guid transactionId)
    {
        try
        {
            var events = await _paymentEventLogRepository
                .FindByConditionAsync(e => e.TransactionId == transactionId);

            return _mapper.Map<IEnumerable<PaymentEventResponse>>(events.OrderBy(e => e.AtUtc));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction events for ID: {TransactionId}", transactionId);
            throw;
        }
    }

    // Simple create-and-QR flow was removed per user's request. Keep existing flows only.

    public async Task<TransactionSummaryResponse> CancelTransactionAsync(Guid transactionId)
    {
        try
        {
            if (transactionId == Guid.Empty)
                throw new ArgumentException("Transaction ID cannot be empty");

            var transaction = await _transactionRepository.FindAsync(transactionId);
            if (transaction == null)
                throw new ArgumentException("Transaction not found");

            if (!await IsTransactionCancellableAsync(transaction))
                throw new InvalidOperationException("Transaction cannot be cancelled");

            // Update transaction status
            transaction.Status = TransactionStatus.Cancelled;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _transactionRepository.UpdateAsync(transaction);

            // Update related transport fee items
            var transportFeeItems = await _transportFeeItemRepository
                .FindByConditionAsync(tfi => tfi.TransactionId == transactionId);

            foreach (var item in transportFeeItems)
            {
                item.Status = TransportFeeItemStatus.Cancelled;
                item.UpdatedAt = DateTime.UtcNow;
                await _transportFeeItemRepository.UpdateAsync(item);
            }

            // Log event
            await LogPaymentEventAsync(transactionId, TransactionStatus.Cancelled, 
                PaymentEventSource.manual, "Transaction cancelled by admin");

            return _mapper.Map<TransactionSummaryResponse>(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transaction: {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<TransactionSummaryResponse> MarkTransactionAsPaidAsync(Guid transactionId, MarkPaidRequest request)
    {
        try
        {
            if (transactionId == Guid.Empty)
                throw new ArgumentException("Transaction ID cannot be empty");

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var transaction = await _transactionRepository.FindAsync(transactionId);
            if (transaction == null)
                throw new ArgumentException("Transaction not found");

            if (await IsTransactionPaidAsync(transaction))
                throw new InvalidOperationException("Transaction is already paid");

            // Update transaction
            transaction.Status = TransactionStatus.Paid;
            transaction.Provider = PaymentProvider.Manual;
            transaction.ProviderTransactionId = request.ProviderTransactionId;
            transaction.PaidAtUtc = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;
            await _transactionRepository.UpdateAsync(transaction);

            // Update related transport fee items
            var transportFeeItems = await _transportFeeItemRepository
                .FindByConditionAsync(tfi => tfi.TransactionId == transactionId);

            foreach (var item in transportFeeItems)
            {
                item.Status = TransportFeeItemStatus.Paid;
                item.UpdatedAt = DateTime.UtcNow;
                await _transportFeeItemRepository.UpdateAsync(item);
            }
            await ActivateStudentsForTransactionAsync(transactionId, PaymentEventSource.manual);

            // Log event
            var message = $"Transaction marked as paid manually. Note: {request.Note}";
            await LogPaymentEventAsync(transactionId, TransactionStatus.Paid, 
                PaymentEventSource.manual, message, request.Note);

            return _mapper.Map<TransactionSummaryResponse>(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking transaction as paid: {TransactionId}", transactionId);
            throw;
        }
    }

    public async Task<bool> HandlePayOSWebhookAsync(PayOSWebhookPayload payload)
    {
        try
        {
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));

            if (payload.Data == null)
                throw new ArgumentException("Webhook payload data is null");

            if (payload.Data.OrderCode == 0)
                throw new ArgumentException("Order code is required in webhook payload");

            _logger.LogInformation("Received PayOS webhook: {@Payload}", payload);

            // Find transaction by order code
            var transaction = await _transactionRepository
                .FindByConditionAsync(t => t.TransactionCode == payload.Data.OrderCode.ToString())
                .ContinueWith(t => t.Result.FirstOrDefault());

            if (transaction == null)
            {
                _logger.LogWarning("Transaction not found for order code: {OrderCode}", payload.Data.OrderCode);
                return false;
            }

            // Verify webhook signature first
            var isValidSignature = await _payOSService.VerifyPayOSWebhookSignatureAsync(payload.Data, payload.Signature);
            if (!isValidSignature)
            {
                _logger.LogWarning("Invalid webhook signature for order code: {OrderCode}", payload.Data.OrderCode);
                return false;
            }

            // Verify webhook data using PayOS SDK
            var verifiedData = await _payOSService.VerifyWebhookDataAsync(payload);

            // Update transaction status based on verified PayOS response
            var newStatus = verifiedData.Code == "00" ? TransactionStatus.Paid : TransactionStatus.Failed;
            
            if (newStatus == TransactionStatus.Paid)
            {
                transaction.Status = TransactionStatus.Paid;
                transaction.PaidAtUtc = DateTime.UtcNow;
                transaction.ProviderTransactionId = verifiedData.Reference;
                transaction.Provider = PaymentProvider.PayOS;
                transaction.UpdatedAt = DateTime.UtcNow;

                // Update related transport fee items
                var transportFeeItems = await _transportFeeItemRepository
                    .FindByConditionAsync(tfi => tfi.TransactionId == transaction.Id);

                foreach (var item in transportFeeItems)
                {
                    item.Status = TransportFeeItemStatus.Paid;
                    item.UpdatedAt = DateTime.UtcNow;
                    await _transportFeeItemRepository.UpdateAsync(item);
                }
                await ActivateStudentsForTransactionAsync(transaction.Id, PaymentEventSource.webhook);
            }
            else
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.UpdatedAt = DateTime.UtcNow;
            }

            await _transactionRepository.UpdateAsync(transaction);

            // Log event
            var message = newStatus == TransactionStatus.Paid 
                ? $"Payment successful via PayOS. Amount: {verifiedData.Amount} VND"
                : $"Payment failed via PayOS. Reason: {verifiedData.Desc}";
                
            await LogPaymentEventAsync(transaction.Id, newStatus, 
                PaymentEventSource.webhook, message, System.Text.Json.JsonSerializer.Serialize(verifiedData));

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PayOS webhook: {@Payload}", payload);
            return false;
        }
    }

    public async Task<Transaction> CreateTransactionForPickupPointRequestAsync(string pickupPointRequestId, Guid scheduleId)
    {
        try
        {
            if (string.IsNullOrEmpty(pickupPointRequestId))
                throw new ArgumentException("Pickup point request ID cannot be empty");

            if (scheduleId == Guid.Empty)
                throw new ArgumentException("Schedule ID cannot be empty");

            // Get pickup point request from MongoDB
            var pickupPointRequest = await _pickupPointRequestRepository.FindAsync(Guid.Parse(pickupPointRequestId));
            if (pickupPointRequest == null)
                throw new ArgumentException("Pickup point request not found");

            // Get students from the request
            var studentIds = pickupPointRequest.StudentIds;
            if (studentIds == null || !studentIds.Any())
                throw new InvalidOperationException("No students found in pickup point request");

            // Calculate transport fees for each student
            var transportFeeItems = new List<TransportFeeItem>();
            decimal totalAmount = 0;

            foreach (var studentId in studentIds)
            {
                var student = await _studentRepository.FindAsync(studentId);
                if (student == null) continue;

                // Calculate fee based on distance and unit price
                var distance = pickupPointRequest.DistanceKm;
                var unitPrice = 5000m; // Default unit price per km
                var quantity = distance;
                var subtotal = unitPrice * (decimal)quantity;

                var transportFeeItem = new TransportFeeItem
                {
                    StudentId = studentId,
                    Description = $"Phí vận chuyển học sinh {student.FirstName} {student.LastName}",
                    DistanceKm = distance,
                    UnitPriceVndPerKm = unitPrice,
                    QuantityKm = quantity,
                    Subtotal = subtotal,
                    PeriodMonth = DateTime.UtcNow.Month,
                    PeriodYear = DateTime.UtcNow.Year,
                    Status = TransportFeeItemStatus.Unbilled
                };

                transportFeeItems.Add(transportFeeItem);
                totalAmount += subtotal;
            }

            if (totalAmount <= 0)
                throw new InvalidOperationException("Total amount must be greater than 0");

            // Create transaction
            var transaction = new Transaction
            {
                ParentId = pickupPointRequest.ParentId ?? Guid.Empty,
                TransactionCode = $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{pickupPointRequestId}",
                Status = TransactionStatus.Notyet,
                Amount = totalAmount,
                Currency = "VND",
                Description = $"Phí vận chuyển học sinh - Yêu cầu điểm đón {pickupPointRequestId}",
                Provider = PaymentProvider.PayOS,
                PickupPointRequestId = pickupPointRequestId,
                ScheduleId = scheduleId,
                Metadata = System.Text.Json.JsonSerializer.Serialize(new { 
                    StudentCount = studentIds.Count(),
                    DistanceKm = pickupPointRequest.DistanceKm,
                    CreatedFrom = "PickupPointRequest"
                })
            };

            // Save transaction first
            var savedTransaction = await _transactionRepository.AddAsync(transaction);

            // Update transport fee items with transaction ID
            foreach (var item in transportFeeItems)
            {
                item.TransactionId = savedTransaction.Id;
                item.Status = TransportFeeItemStatus.Invoiced;
                await _transportFeeItemRepository.AddAsync(item);
            }

            // Log event
            await LogPaymentEventAsync(savedTransaction.Id, TransactionStatus.Notyet, 
                PaymentEventSource.manual, $"Transaction created for pickup point request: {pickupPointRequestId}");

            return savedTransaction;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating transaction for pickup point request: {PickupPointRequestId}", pickupPointRequestId);
            throw;
        }
    }

    public async Task<bool> IsTransactionCancellableAsync(Transaction transaction)
    {
        return transaction.Status == TransactionStatus.Notyet;
    }

    public async Task<bool> IsTransactionPaidAsync(Transaction transaction)
    {
        return transaction.Status == TransactionStatus.Paid;
    }

    private async Task LogPaymentEventAsync(Guid transactionId, TransactionStatus status, 
        PaymentEventSource source, string message, string? rawPayload = null)
    {
        try
        {
            var eventLog = new PaymentEventLog
            {
                TransactionId = transactionId,
                Status = status,
                AtUtc = DateTime.UtcNow,
                Source = source,
                Message = message,
                RawPayload = rawPayload
            };

            await _paymentEventLogRepository.AddAsync(eventLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging payment event for transaction: {TransactionId}", transactionId);
            // Don't throw here to avoid breaking the main flow
        }
    }
    private async Task ActivateStudentsForTransactionAsync(Guid transactionId, PaymentEventSource source)
    {
        var items = await _transportFeeItemRepository.FindByConditionAsync(tfi => tfi.TransactionId == transactionId);
        var studentIds = items.Select(i => i.StudentId).Distinct().ToList();
        if (!studentIds.Any()) return;

        var students = await _studentRepository.GetQueryable()
            .Where(s => studentIds.Contains(s.Id) && !s.IsDeleted)
            .ToListAsync();

        foreach (var s in students)
        {
            if (s.Status == StudentStatus.Available || s.Status == StudentStatus.Pending)
            {
                s.Status = StudentStatus.Active;
                await _studentRepository.UpdateAsync(s);
            }
        }

        await LogPaymentEventAsync(transactionId, TransactionStatus.Paid, source, "Students activated after payment");
    }
}
