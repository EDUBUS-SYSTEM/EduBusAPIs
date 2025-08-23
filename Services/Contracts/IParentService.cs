using Services.Models.Parent;
using Services.Models.UserAccount;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts
{
    public interface IParentService
    {
        Task<CreateUserResponse> CreateParentAsync(CreateParentRequest dto);
        Task<ImportUsersResult> ImportParentsFromExcelAsync(Stream excelFileStream);
        Task<byte[]> ExportParentsToExcelAsync();
    }
}
