using Data.Models.Enums;

namespace Services.Models.Transaction
{
    public class AdminCreateTransactionResponse
    {
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public List<Guid> StudentIds { get; set; } = new();
        public string TransactionCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TransactionStatus Status { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid CreatedByAdminId { get; set; }
        public List<TransportFeeItemInfo> TransportFeeItems { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }
}
