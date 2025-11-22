using Services.Models.PickupPoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Contracts
{
	public interface IPickupPointService
	{
		Task<PickupPointsResponse> GetUnassignedPickupPointsAsync();
		Task<AdminCreatePickupPointResponse> AdminCreatePickupPointAsync(AdminCreatePickupPointRequest request, Guid adminId);
		Task<ResetPickupPointBySemesterResponse> ResetPickupPointBySemesterAsync(ResetPickupPointBySemesterRequest request, Guid adminId);
		Task<GetPickupPointsBySemesterResponse> GetPickupPointsBySemesterAsync(GetPickupPointsBySemesterRequest request);
	}
}
