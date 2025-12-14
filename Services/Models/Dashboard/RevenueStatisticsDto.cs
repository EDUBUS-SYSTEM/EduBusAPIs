namespace Services.Models.Dashboard
{
    public class RevenueStatisticsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal FailedAmount { get; set; }
        public string Currency { get; set; } = "VND";
        public int PaidTransactionCount { get; set; }
        public int PendingTransactionCount { get; set; }
        public int FailedTransactionCount { get; set; }
    }
}


