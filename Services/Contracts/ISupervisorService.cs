using Services.Models.Supervisor;
using Services.Models.UserAccount;

namespace Services.Contracts
{
    public interface ISupervisorService
    {
        Task<CreateUserResponse> CreateSupervisorAsync(CreateSupervisorRequest dto);
        Task<ImportUsersResult> ImportSupervisorsFromExcelAsync(Stream excelFileStream);
        Task<byte[]> ExportSupervisorsToExcelAsync();
        Task<SupervisorResponse?> GetSupervisorResponseByIdAsync(Guid supervisorId);
        Task<IEnumerable<SupervisorResponse>> GetAllSupervisorsAsync();
    }
}

