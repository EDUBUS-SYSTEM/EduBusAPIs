using Services.Models.Jetson;

namespace Services.Contracts
{
	public interface IJetsonService
	{
		Task<JetsonStudentSyncResponse> GetStudentsForSyncAsync(string deviceId, Guid routeId);
		Task<bool> SubmitAttendanceAsync(SubmitAttendanceRequest request);
		Task<ActiveTripResponse?> GetActiveTripForPlateAsync(string plateNumber);
	}
}
