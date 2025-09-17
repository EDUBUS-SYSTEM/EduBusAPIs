using Data.Models.Enums;

namespace Services.Models.Payment;

public class TransactionListRequest
{
    public TransactionStatus? Status { get; set; }
    public Guid? ParentId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PerPage { get; set; } = 20;
    public string SortBy { get; set; } = "createdAtUtc";
    public string SortOrder { get; set; } = "desc";
}

