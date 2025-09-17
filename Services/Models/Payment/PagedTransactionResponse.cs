namespace Services.Models.Payment;

public class PagedTransactionResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PerPage { get; set; }
    public IEnumerable<TransactionSummaryResponse> Data { get; set; } = new List<TransactionSummaryResponse>();
}

