using AutoMapper;
using Data.Models;
using Services.Models.Vehicle;
using Utils;

namespace Services.Extensions
{
    public static class VehicleExtensions
    {
        public static VehicleDto ToDecryptedDto(this Vehicle vehicle, IMapper mapper)
        {
            var dto = mapper.Map<VehicleDto>(vehicle);
            dto.LicensePlate = SecurityHelper.DecryptFromBytes(vehicle.HashedLicensePlate);
            return dto;
        }

        public static IEnumerable<VehicleDto> ToDecryptedDtos(this IEnumerable<Vehicle> vehicles, IMapper mapper)
        {
            return vehicles.Select(v => v.ToDecryptedDto(mapper));
        }
    }
}
