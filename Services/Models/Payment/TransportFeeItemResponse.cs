using Data.Models.Enums;

namespace Services.Models.Payment;

public class TransportFeeItemResponse
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public TransportFeeItemStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public decimal UnitPriceVndPerKm { get; set; }
    public double QuantityKm { get; set; }
    public decimal Subtotal { get; set; }
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
}

