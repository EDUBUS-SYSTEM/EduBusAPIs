using AutoMapper;
using Data.Models;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Driver;
using Utils;

namespace Services.Implementations
{
    public class DriverLicenseService : IDriverLicenseService
    {
        private readonly IDriverLicenseRepository _driverLicenseRepository;
        private readonly IMapper _mapper;

        public DriverLicenseService(IDriverLicenseRepository driverLicenseRepository, IMapper mapper)
        {
            _driverLicenseRepository = driverLicenseRepository;
            _mapper = mapper;
        }

        public async Task<DriverLicenseResponse> CreateDriverLicenseAsync(CreateDriverLicenseRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Check if driver already has a license
            var existingLicense = await _driverLicenseRepository.FindByConditionAsync(dl => dl.DriverId == request.DriverId);
            if (existingLicense != null)
                throw new InvalidOperationException("Driver already has a license.");

            var driverLicense = _mapper.Map<DriverLicense>(request);
            
            // Hash the license number
            driverLicense.HashedLicenseNumber = SecurityHelper.EncryptToBytes(request.LicenseNumber);
            
            // Set audit fields
            driverLicense.CreatedBy = request.DriverId; // Assuming the driver is creating their own license
            
            var createdLicense = await _driverLicenseRepository.AddAsync(driverLicense);
            return _mapper.Map<DriverLicenseResponse>(createdLicense);
        }

        public async Task<DriverLicenseResponse?> GetDriverLicenseByDriverIdAsync(Guid driverId)
        {
            var driverLicenses = await _driverLicenseRepository.FindByConditionAsync(dl => dl.DriverId == driverId);
            var driverLicense = driverLicenses.FirstOrDefault();
            return driverLicense != null ? _mapper.Map<DriverLicenseResponse>(driverLicense) : null;
        }

        public async Task<DriverLicenseResponse> UpdateDriverLicenseAsync(Guid id, CreateDriverLicenseRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var existingLicense = await _driverLicenseRepository.FindAsync(id);
            if (existingLicense == null)
                throw new InvalidOperationException("Driver license not found.");

            // Update properties
            existingLicense.DateOfIssue = request.DateOfIssue;
            existingLicense.IssuedBy = request.IssuedBy;
            existingLicense.HashedLicenseNumber = SecurityHelper.EncryptToBytes(request.LicenseNumber);
            existingLicense.UpdatedBy = request.DriverId; // Assuming the driver is updating their own license
            existingLicense.UpdatedAt = DateTime.UtcNow;

            var updatedLicense = await _driverLicenseRepository.UpdateAsync(existingLicense);
            return _mapper.Map<DriverLicenseResponse>(updatedLicense);
        }

        public async Task<bool> DeleteDriverLicenseAsync(Guid id)
        {
            var driverLicense = await _driverLicenseRepository.FindAsync(id);
            if (driverLicense == null)
                return false;

            await _driverLicenseRepository.DeleteAsync(driverLicense);
            return true;
        }
    }
}
