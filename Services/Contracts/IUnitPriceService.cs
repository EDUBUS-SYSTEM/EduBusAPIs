using Services.Models.UnitPrice;

namespace Services.Contracts;

public interface IUnitPriceService
{
    Task<List<UnitPriceResponseDto>> GetAllUnitPricesAsync();
    Task<List<UnitPriceResponseDto>> GetAllIncludingDeletedAsync();
    Task<UnitPriceResponseDto?> GetUnitPriceByIdAsync(Guid id);
    Task<List<UnitPriceResponseDto>> GetActiveUnitPricesAsync();
    Task<List<UnitPriceResponseDto>> GetEffectiveUnitPricesAsync(DateTime? date = null);
    Task<UnitPriceResponseDto?> GetCurrentEffectiveUnitPriceAsync();
    Task<UnitPriceResponseDto> CreateUnitPriceAsync(CreateUnitPriceDto dto);
    Task<UnitPriceResponseDto?> UpdateUnitPriceAsync(UpdateUnitPriceDto dto);
    Task<bool> DeleteUnitPriceAsync(Guid id);
    Task<bool> ActivateUnitPriceAsync(Guid id);
    Task<bool> DeactivateUnitPriceAsync(Guid id);
}
