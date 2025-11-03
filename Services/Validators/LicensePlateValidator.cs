using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;
using Utils;

namespace Services.Validators
{
    public class LicensePlateValidator
    {
        private readonly IVehicleRepository _vehicleRepository;

        public LicensePlateValidator(IVehicleRepository vehicleRepository)
        {
            _vehicleRepository = vehicleRepository;
        }

        /// <summary>
        /// Checks if a license plate already exists in the system
        /// </summary>
        /// <param name="licensePlate">License plate to check</param>
        /// <param name="excludeVehicleId">Vehicle ID to exclude from the check (for updates)</param>
        /// <returns>True if duplicate exists, false otherwise</returns>
        public async Task<bool> IsDuplicateAsync(string licensePlate, Guid? excludeVehicleId = null)
        {
            var existingVehicles = await _vehicleRepository.GetQueryable()
                .Where(v => !v.IsDeleted && (excludeVehicleId == null || v.Id != excludeVehicleId))
                .ToListAsync();

            var normalizedNewPlate = Normalize(licensePlate);
            return existingVehicles.Any(v =>
                Normalize(SecurityHelper.DecryptFromBytes(v.HashedLicensePlate)) == normalizedNewPlate);
        }

        private static string Normalize(string licensePlate)
        {
            return licensePlate.Replace(" ", "").Replace("-", "").ToUpperInvariant();
        }
    }
}
