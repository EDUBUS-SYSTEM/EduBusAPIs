using Services.Models.Driver;
using Services.Models.Parent;
using Services.Models.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts
{
    public interface IDriverService
    {
        Task<CreateUserResponse> CreateDriverAsync(CreateDriverRequest dto);
        Task<ImportUsersResult> ImportDriversFromExcelAsync(Stream excelFileStream);
        Task<byte[]> ExportDriversToExcelAsync();
        Task<Data.Models.Driver?> GetDriverByIdAsync(Guid driverId);
        Task<Guid?> GetHealthCertificateFileIdAsync(Guid driverId);
    }
}
