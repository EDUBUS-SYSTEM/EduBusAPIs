using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts
{
	public interface ITripHubService
	{
		Task BroadcastTripStatusChangedAsync(Guid tripId, string status, DateTime? startTime, DateTime? endTime);
		Task BroadcastAttendanceUpdatedAsync(Guid tripId, Guid stopId, object attendanceSummary);
		Task BroadcastStopArrivalAsync(Guid tripId, Guid stopId, DateTime arrivedAt);
	}
}
