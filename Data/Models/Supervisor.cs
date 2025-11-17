using Data.Models.Enums;

namespace Data.Models;

public partial class Supervisor : UserAccount
{
    public SupervisorStatus Status { get; set; } = SupervisorStatus.Active;
    public DateTime? LastActiveDate { get; set; }
    public string? StatusNote { get; set; }
}

