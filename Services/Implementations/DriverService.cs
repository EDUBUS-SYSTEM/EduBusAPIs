using AutoMapper;
using ClosedXML.Excel;
using Data.Models;
using Data.Repos.Interfaces;
using Data.Repos.SqlServer;
using Services.Contracts;
using Services.Models.Driver;
using Services.Models.Parent;
using Services.Models.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Services.Implementations
{
    public class DriverService : IDriverService
    {
        private readonly IDriverRepository _driverRepository;
        private readonly IUserAccountRepository _userAccountRepository;
        private readonly IMapper _mapper;
        public DriverService(IDriverRepository driverRepository, IUserAccountRepository userAccountRepository, IMapper mapper)
        {
            _driverRepository = driverRepository;
            _userAccountRepository = userAccountRepository;
            _mapper = mapper;
        }
        public async Task<CreateUserResponse> CreateDriverAsync(CreateDriverRequest dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            var isExistingEmail = await _userAccountRepository.IsEmailExistAsync(dto.Email);
            if (isExistingEmail)
                throw new InvalidOperationException("Email already exists.");
            var isExistingPhone = await _userAccountRepository.IsPhoneNumberExistAsync(dto.PhoneNumber);
            if (isExistingPhone)
                throw new InvalidOperationException("Phone number already exists.");
            var driver = _mapper.Map<Driver>(dto);
            //hash password
            var rawPassword = SecurityHelper.GenerateRandomPassword();
            var hashedPassword = SecurityHelper.HashPassword(rawPassword);
            driver.HashedPassword = hashedPassword;
            //hash licenseNumber
            var hashLicenseNumber = SecurityHelper.EncryptToBytes(dto.LicenseNumber);
            driver.HashedLicenseNumber = hashedPassword;
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
                            Address = row.Cell(7).GetString().Trim(),
                            LicenseNumber = row.Cell(8).GetString().Trim()
                        };

                        // Validate basic data
                        if (string.IsNullOrWhiteSpace(driverDto.Email) ||
                            string.IsNullOrWhiteSpace(driverDto.FirstName) ||
                            string.IsNullOrWhiteSpace(driverDto.LastName) ||
                            string.IsNullOrWhiteSpace(driverDto.PhoneNumber) ||
                            driverDto.Gender == 0 ||
                            string.IsNullOrWhiteSpace(driverDto.DateOfBirthString) ||
                            string.IsNullOrWhiteSpace(driverDto.Address) ||
                            string.IsNullOrWhiteSpace(driverDto.LicenseNumber))

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

                        // Map DTO thành entity
                        var driver = _mapper.Map<Driver>(driverDto);

                        // Generate password
                        var rawPassword = SecurityHelper.GenerateRandomPassword();
                        var hashedPassword = SecurityHelper.HashPassword(rawPassword);
                        driver.HashedPassword = hashedPassword;
                        //hash LicenseNumber
                        var hashLicenseNumber = SecurityHelper.EncryptToBytes(driverDto.LicenseNumber);
                        driver.HashedLicenseNumber = hashLicenseNumber;
                        // Thêm vào database
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
            catch (Exception ex)
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
                worksheet.Cell(1, 8).Value = "License Number";

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
                    var licenseNumber = SecurityHelper.DecryptFromBytes(d.HashedLicenseNumber);
                    worksheet.Cell(row, 8).Value = licenseNumber;
                    row++;
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

    }
}


