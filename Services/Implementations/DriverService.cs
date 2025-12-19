using AutoMapper;
using ClosedXML.Excel;
using Data.Models;
using Data.Models.Enums;
using Data.Repos.Interfaces;
using Services.Contracts;
using Services.Models.Driver;
using Services.Models.UserAccount;
using Services.Validators;
using Utils;

namespace Services.Implementations
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository _driverRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IDriverVehicleRepository _driverVehicleRepository;
        private readonly IDriverWorkingHoursRepository _driverWorkingHoursRepository;
        private readonly IMapper _mapper;
        private readonly UserAccountValidationService _validationService;
        public DriverService(IDriverRepository driverRepository, IUserAccountRepository userAccountRepository, IDriverVehicleRepository driverVehicleRepository, IDriverWorkingHoursRepository driverWorkingHoursRepository, IMapper mapper, UserAccountValidationService validationService)
        {
            _driverRepository = driverRepository;
            _userAccountRepository = userAccountRepository;
            _driverVehicleRepository = driverVehicleRepository;
            _driverWorkingHoursRepository = driverWorkingHoursRepository;
            _mapper = mapper;
            _validationService = validationService;
        }
        public async Task<CreateUserResponse> CreateDriverAsync(CreateDriverRequest dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            await _validationService.ValidateEmailAndPhoneAsync(dto.Email, dto.PhoneNumber);
            var driver = _mapper.Map<Driver>(dto);
            //hash password
            var (rawPassword, hashedPassword) = SecurityHelper.GenerateAndHashPassword();
            driver.HashedPassword = hashedPassword;
            var createdDriver = await _driverRepository.AddAsync(driver);
            var response = _mapper.Map<CreateUserResponse>(createdDriver);
            response.Password = rawPassword;    
            return response;
        }

        public async Task<ImportUsersResult> ImportDriversFromExcelAsync(Stream excelFileStream)
        {
            if (excelFileStream == null)
                throw new ArgumentNullException(nameof(excelFileStream));
            var result = new ImportUsersResult();
            var validPriverDtos = new List<(ImportDriverDto dto, int rowNumber)>();
            try
            {
                using var workbook = new XLWorkbook(excelFileStream);
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                    throw new InvalidOperationException("Excel file does not contain any worksheets.");

                // Read data from Excel file (skip header row)
                var rows = worksheet.RangeUsed()?.RowsUsed().Skip(1);
                if (rows == null || !rows.Any())
                    throw new InvalidOperationException("Excel file does not contain any data rows.");
                // Parse data from Excel to DTO
                foreach (var row in rows)
                {
                    var rowNumber = row.RowNumber();
                    try
                    {
                        var driverDto = new ImportDriverDto
                        {
                            Email = row.Cell(1).GetString().Trim(),
                            FirstName = row.Cell(2).GetString().Trim(),
                            LastName = row.Cell(3).GetString().Trim(),
                            PhoneNumber = row.Cell(4).GetString().Trim(),
                            Gender = row.Cell(5).GetValue<int>(),
                            DateOfBirthString = row.Cell(6).GetString().Trim(),
                            Address = row.Cell(7).GetString().Trim()
                        };

                        // Validate basic data
                        if (string.IsNullOrWhiteSpace(driverDto.Email) ||
                            string.IsNullOrWhiteSpace(driverDto.FirstName) ||
                            string.IsNullOrWhiteSpace(driverDto.LastName) ||
                            string.IsNullOrWhiteSpace(driverDto.PhoneNumber) ||
                            driverDto.Gender == 0 ||
                            string.IsNullOrWhiteSpace(driverDto.DateOfBirthString) ||
                            string.IsNullOrWhiteSpace(driverDto.Address))

                        {
                            result.FailedUsers.Add(new ImportUserError
                            {
                                RowNumber = rowNumber,
                                Email = driverDto.Email,
                                FirstName = driverDto.FirstName,
                                LastName = driverDto.LastName,
                                PhoneNumber = driverDto.PhoneNumber,
                                ErrorMessage = "All properties are required."
                            });
                            continue;
                        }

                        // Validate email format
                        if (!EmailHelper.IsValidEmail(driverDto.Email))
                        {
                            result.FailedUsers.Add(new ImportUserError
                            {
                                RowNumber = rowNumber,
                                Email = driverDto.Email,
                                FirstName = driverDto.FirstName,
                                LastName = driverDto.LastName,
                                PhoneNumber = driverDto.PhoneNumber,
                                ErrorMessage = "Invalid email format."
                            });
                            continue;
                        }

                        validPriverDtos.Add((driverDto, rowNumber));
                    }
                    catch (Exception ex)
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = rowNumber,
                            Email = "",
                            FirstName = "",
                            LastName = "",
                            ErrorMessage = $"Error parsing data: {ex.Message}"
                        });
                    }
                }
                // Check duplicate emails in Excel file
                var duplicateEmailGroups = validPriverDtos
                    .GroupBy(x => x.dto.Email.ToLower())
                    .Where(g => g.Count() > 1);
                foreach (var duplicateGroup in duplicateEmailGroups)
                {
                    var duplicates = duplicateGroup.ToList();
                    // Keep the first record, mark the remaining records as errors
                    for (int i = 1; i < duplicates.Count; i++)
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = duplicates[i].rowNumber,
                            Email = duplicates[i].dto.Email,
                            FirstName = duplicates[i].dto.FirstName,
                            LastName = duplicates[i].dto.LastName,
                            PhoneNumber = duplicates[i].dto.PhoneNumber,
                            ErrorMessage = "Duplicate email found in Excel file."
                        });
                        validPriverDtos.Remove(duplicates[i]);
                    }
                }
                //Check duplicate phones in Excel file
                var duplicatePhoneGroups = validPriverDtos
                    .Where(x => !string.IsNullOrWhiteSpace(x.dto.PhoneNumber))
                    .GroupBy(x => x.dto.PhoneNumber)
                    .Where(g => g.Count() > 1);
                foreach (var duplicateGroup in duplicatePhoneGroups)
                {
                    var duplicates = duplicateGroup.ToList();
                    // Keep the first record, mark the remaining records as errors
                    for (int i = 1; i < duplicates.Count; i++)
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = duplicates[i].rowNumber,
                            Email = duplicates[i].dto.Email,
                            FirstName = duplicates[i].dto.FirstName,
                            LastName = duplicates[i].dto.LastName,
                            PhoneNumber = duplicates[i].dto.PhoneNumber,
                            ErrorMessage = "Duplicate phone number found in Excel file."
                        });
                        validPriverDtos.Remove(duplicates[i]);
                    }
                }
                // Set total number of records processed
                result.TotalProcessed = rows.Count();

                // Process driver
                foreach (var (driverDto, rowNumber) in validPriverDtos)
                {
                    try
                    {
                        // Check email exists in database
                        var isExistingEmail = await _userAccountRepository.IsEmailExistAsync(driverDto.Email);
                        if (isExistingEmail)
                        {
                            result.FailedUsers.Add(new ImportUserError
                            {
                                RowNumber = rowNumber,
                                Email = driverDto.Email,
                                FirstName = driverDto.FirstName,
                                LastName = driverDto.LastName,
                                PhoneNumber = driverDto.PhoneNumber,
                                ErrorMessage = "Email already exists in database."
                            });
                            continue;
                        }

                        // Check if phone number exists in database
                        if (!string.IsNullOrWhiteSpace(driverDto.PhoneNumber))
                        {
                            var isExistingPhone = await _userAccountRepository.IsPhoneNumberExistAsync(driverDto.PhoneNumber);
                            if (isExistingPhone)
                            {
                                result.FailedUsers.Add(new ImportUserError
                                {
                                    RowNumber = rowNumber,
                                    Email = driverDto.Email,
                                    FirstName = driverDto.FirstName,
                                    LastName = driverDto.LastName,
                                    PhoneNumber = driverDto.PhoneNumber,
                                    ErrorMessage = "Phone number already exists in database."
                                });
                                continue;
                            }
                        }

                        // Map DTO -> entity
                        var driver = _mapper.Map<Driver>(driverDto);

                        // Generate password
                        var (rawPassword, hashedPassword) = SecurityHelper.GenerateAndHashPassword();
                        driver.HashedPassword = hashedPassword;
                        // Note: License number will be handled separately through DriverLicense entity
                        var createdDriver = await _driverRepository.AddAsync(driver);

                        var successResult = _mapper.Map<ImportUserSuccess>(createdDriver);
                        successResult.RowNumber = rowNumber;
                        successResult.Password = rawPassword;
                        result.SuccessfulUsers.Add(successResult);
                    }
                    catch (Exception ex)
                    {
                        result.FailedUsers.Add(new ImportUserError
                        {
                            RowNumber = rowNumber,
                            Email = driverDto.Email,
                            FirstName = driverDto.FirstName,
                            LastName = driverDto.LastName,
                            PhoneNumber = driverDto.PhoneNumber,
                        
                            ErrorMessage = $"Error creating driver: {ex.Message}"
                        });
                    }
                }
                return result;
            }
            catch
            {
                throw;
            }
        }

        public async Task<byte[]> ExportDriversToExcelAsync()
        {
            var drivers = await _driverRepository.FindAllAsync();
            if (drivers == null || !drivers.Any())
            {
                return Array.Empty<byte>();
            }
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Drivers");

                // Header
                worksheet.Cell(1, 1).Value = "First Name";
                worksheet.Cell(1, 2).Value = "Last Name";
                worksheet.Cell(1, 3).Value = "Email";
                worksheet.Cell(1, 4).Value = "Phone";
                worksheet.Cell(1, 5).Value = "Gender";
                worksheet.Cell(1, 6).Value = "Date of Birth";
                worksheet.Cell(1, 7).Value = "Address";

                int row = 2;
                foreach (var d in drivers)
                {
                    worksheet.Cell(row, 1).Value = d.FirstName;
                    worksheet.Cell(row, 2).Value = d.LastName;
                    worksheet.Cell(row, 3).Value = d.Email;
                    worksheet.Cell(row, 4).Value = d.PhoneNumber;
                    worksheet.Cell(row, 5).Value = d.Gender.ToString();
                    worksheet.Cell(row, 6).Value = d.DateOfBirth?.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 7).Value = d.Address;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<Driver?> GetDriverByIdAsync(Guid driverId)
        {
            return await _driverRepository.FindAsync(driverId);
        }

        public async Task<Guid?> GetHealthCertificateFileIdAsync(Guid driverId)
        {
            var driver = await _driverRepository.FindAsync(driverId);
            return driver?.HealthCertificateFileId;
        }

        public async Task<IEnumerable<DriverResponse>> GetAllDriversAsync()
        {
            var drivers = await _driverRepository.FindAllAsync();
            return _mapper.Map<IEnumerable<DriverResponse>>(drivers);
        }

        public async Task<DriverResponse?> GetDriverResponseByIdAsync(Guid driverId)
        {
            var driver = await _driverRepository.FindAsync(driverId);
            return _mapper.Map<DriverResponse>(driver);
        }

        public async Task<DriverResponse> UpdateDriverStatusAsync(Guid driverId, DriverStatus status, string? note)
        {
            var driver = await _driverRepository.FindAsync(driverId) ?? throw new InvalidOperationException("Driver not found");
            driver.Status = status;
            driver.StatusNote = note;
            driver.UpdatedAt = DateTime.UtcNow;
            var updated = await _driverRepository.UpdateAsync(driver);
            return _mapper.Map<DriverResponse>(updated!);
        }

        public async Task<DriverResponse> SuspendDriverAsync(Guid driverId, string reason, DateTime? untilDate)
        {
            var note = untilDate.HasValue ? $"Suspended until {untilDate.Value:O}. Reason: {reason}" : $"Suspended. Reason: {reason}";
            return await UpdateDriverStatusAsync(driverId, DriverStatus.Suspended, note);
        }

        public async Task<DriverResponse> ReactivateDriverAsync(Guid driverId)
        {
            return await UpdateDriverStatusAsync(driverId, DriverStatus.Active, null);
        }

        public async Task<IEnumerable<DriverResponse>> GetDriversByStatusAsync(DriverStatus status)
        {
            var list = await _driverRepository.FindByConditionAsync(d => d.Status == status && !d.IsDeleted);
            return list.Select(_mapper.Map<DriverResponse>);
        }

        public async Task<bool> IsDriverAvailableAsync(Guid driverId, DateTime startTime, DateTime endTime)
        {
            // Check overlapping assignments
            var hasConflict = await _driverVehicleRepository.HasTimeConflictAsync(driverId, startTime, endTime);
            return !hasConflict;
        }

        
    }
}


