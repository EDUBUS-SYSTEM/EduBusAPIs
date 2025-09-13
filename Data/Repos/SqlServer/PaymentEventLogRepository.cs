using Data.Models;
using Data.Repos.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Data.Repos.SqlServer;

public class PaymentEventLogRepository : SqlRepository<PaymentEventLog>, IPaymentEventLogRepository
{
    public PaymentEventLogRepository(DbContext dbContext) : base(dbContext)
    {
    }
}


