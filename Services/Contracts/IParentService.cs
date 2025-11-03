using Services.Models.Parent;
using Services.Models.UserAccount;

namespace Services.Contracts
{
    public interface IParentService
    {
        Task<CreateUserResponse> CreateParentAsync(CreateParentRequest dto);
        Task<ImportUsersResult> ImportParentsFromExcelAsync(Stream excelFileStream);
        Task<byte[]> ExportParentsToExcelAsync();
    }
}
