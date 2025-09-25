using AutoMapper;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.UnitPrice;

namespace Services.Implementations;

public class UnitPriceService : IUnitPriceService
{
    private readonly IUnitPriceRepository _unitPriceRepository;
    private readonly IMapper _mapper;

    public UnitPriceService(IUnitPriceRepository unitPriceRepository, IMapper mapper)
    {
        _unitPriceRepository = unitPriceRepository;
        _mapper = mapper;
    }

    public async Task<List<UnitPriceResponseDto>> GetAllUnitPricesAsync()
    {
        var unitPrices = await _unitPriceRepository.FindAllAsync();
        return _mapper.Map<List<UnitPriceResponseDto>>(unitPrices);
    }

    public async Task<List<UnitPriceResponseDto>> GetAllIncludingDeletedAsync()
    {
        var unitPrices = await _unitPriceRepository.GetAllIncludingDeletedAsync();
        return _mapper.Map<List<UnitPriceResponseDto>>(unitPrices);
    }

    public async Task<UnitPriceResponseDto?> GetUnitPriceByIdAsync(Guid id)
    {
        var unitPrice = await _unitPriceRepository.FindAsync(id);
        return unitPrice == null ? null : _mapper.Map<UnitPriceResponseDto>(unitPrice);
    }

    public async Task<List<UnitPriceResponseDto>> GetActiveUnitPricesAsync()
    {
        var unitPrices = await _unitPriceRepository.GetActiveUnitPricesAsync();
        return _mapper.Map<List<UnitPriceResponseDto>>(unitPrices);
    }

    public async Task<List<UnitPriceResponseDto>> GetEffectiveUnitPricesAsync(DateTime? date = null)
    {
        var checkDate = date ?? DateTime.UtcNow;
        var unitPrices = await _unitPriceRepository.GetEffectiveUnitPricesAsync(checkDate);
        return _mapper.Map<List<UnitPriceResponseDto>>(unitPrices);
    }

    public async Task<UnitPriceResponseDto?> GetCurrentEffectiveUnitPriceAsync()
    {
        var unitPrice = await _unitPriceRepository.GetCurrentEffectiveUnitPriceAsync();
        return unitPrice == null ? null : _mapper.Map<UnitPriceResponseDto>(unitPrice);
    }

    public async Task<UnitPriceResponseDto> CreateUnitPriceAsync(CreateUnitPriceDto dto)
    {
        // Check overlap with existing active UnitPrices
        await ValidateNoOverlappingActiveUnitPrices(dto.EffectiveFrom, dto.EffectiveTo);

        var unitPrice = new Data.Models.UnitPrice
        {
            Name = dto.Name,
            Description = dto.Description,
            PricePerKm = dto.PricePerKm,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            IsActive = true,
            ByAdminId = dto.ByAdminId,
            ByAdminName = dto.ByAdminName
        };

        await _unitPriceRepository.AddAsync(unitPrice);
        return _mapper.Map<UnitPriceResponseDto>(unitPrice);
    }

    private async Task ValidateNoOverlappingActiveUnitPrices(DateTime effectiveFrom, DateTime? effectiveTo)
    {
        var existingActivePrices = await _unitPriceRepository.GetActiveUnitPricesAsync();

        foreach (var existingPrice in existingActivePrices)
        {
            // Check overlap
            var existingFrom = existingPrice.EffectiveFrom;
            var existingTo = existingPrice.EffectiveTo ?? DateTime.MaxValue;
            var newFrom = effectiveFrom;
            var newTo = effectiveTo ?? DateTime.MaxValue;

            // Overlap exists if: (newFrom < existingTo) && (newTo > existingFrom)
            if (newFrom < existingTo && newTo > existingFrom)
            {
                throw new InvalidOperationException(
                    $"UnitPrice '{existingPrice.Name}' already exists effective from {existingFrom:dd/MM/yyyy} " +
                    $"to {(existingPrice.EffectiveTo?.ToString("dd/MM/yyyy") ?? "no limit")}. " +
                    "Cannot create new UnitPrice in this time range.");
            }
        }
    }

    public async Task<UnitPriceResponseDto?> UpdateUnitPriceAsync(UpdateUnitPriceDto dto)
    {
        var unitPrice = await _unitPriceRepository.FindAsync(dto.Id);
        if (unitPrice == null) return null;

        // Check overlap with other active UnitPrices
        await ValidateNoOverlappingActiveUnitPricesForUpdate(dto.EffectiveFrom, dto.EffectiveTo, dto.Id);

        unitPrice.Name = dto.Name;
        unitPrice.Description = dto.Description;
        unitPrice.PricePerKm = dto.PricePerKm;
        unitPrice.EffectiveFrom = dto.EffectiveFrom;
        unitPrice.EffectiveTo = dto.EffectiveTo;
        unitPrice.IsActive = dto.IsActive;
        unitPrice.UpdatedAt = DateTime.UtcNow;

        await _unitPriceRepository.UpdateAsync(unitPrice);
        return _mapper.Map<UnitPriceResponseDto>(unitPrice);
    }

    private async Task ValidateNoOverlappingActiveUnitPricesForUpdate(DateTime effectiveFrom, DateTime? effectiveTo, Guid currentId)
    {
        var existingActivePrices = await _unitPriceRepository.GetActiveUnitPricesAsync();

        foreach (var existingPrice in existingActivePrices.Where(p => p.Id != currentId))
        {
            // Check overlap
            var existingFrom = existingPrice.EffectiveFrom;
            var existingTo = existingPrice.EffectiveTo ?? DateTime.MaxValue;
            var newFrom = effectiveFrom;
            var newTo = effectiveTo ?? DateTime.MaxValue;

            // Overlap exists if: (newFrom < existingTo) && (newTo > existingFrom)
            if (newFrom < existingTo && newTo > existingFrom)
            {
                throw new InvalidOperationException(
                    $"UnitPrice '{existingPrice.Name}' already exists effective from {existingFrom:dd/MM/yyyy} " +
                    $"to {(existingPrice.EffectiveTo?.ToString("dd/MM/yyyy") ?? "no limit")}. " +
                    "Cannot update UnitPrice in this time range.");
            }
        }
    }

    public async Task<bool> DeleteUnitPriceAsync(Guid id)
    {
        var unitPrice = await _unitPriceRepository.FindAsync(id);
        if (unitPrice == null) return false;

        unitPrice.IsDeleted = true;
        unitPrice.IsActive = false;
        unitPrice.EffectiveTo = DateTime.UtcNow;
        unitPrice.UpdatedAt = DateTime.UtcNow;

        await _unitPriceRepository.DeleteAsync(unitPrice);
        return true;
    }

    public async Task<bool> ActivateUnitPriceAsync(Guid id)
    {
        var unitPrice = await _unitPriceRepository.FindAsync(id);
        if (unitPrice == null) return false;

        unitPrice.IsActive = true;
        unitPrice.UpdatedAt = DateTime.UtcNow;

        await _unitPriceRepository.UpdateAsync(unitPrice);
        return true;
    }

    public async Task<bool> DeactivateUnitPriceAsync(Guid id)
    {
        var unitPrice = await _unitPriceRepository.FindAsync(id);
        if (unitPrice == null) return false;

        unitPrice.IsActive = false;
        unitPrice.UpdatedAt = DateTime.UtcNow;

        await _unitPriceRepository.UpdateAsync(unitPrice);
        return true;
    }
}
