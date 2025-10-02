using Services.Models.Route;

namespace Services.Contracts
{
    public interface IRouteService
    {
        Task<RouteDto> CreateRouteAsync(CreateRouteRequest request);
        Task<CreateBulkRouteResponse> CreateBulkRoutesAsync(CreateBulkRouteRequest request);
        Task<RouteDto?> GetRouteByIdAsync(Guid id);
        Task<IEnumerable<RouteDto>> GetAllRoutesAsync();
        Task<IEnumerable<RouteDto>> GetActiveRoutesAsync();
        Task<RouteDto?> UpdateRouteAsync(Guid id, UpdateRouteRequest request);
		Task<RouteDto?> UpdateRouteBasicAsync(Guid id, UpdateRouteBasicRequest request);
		Task<UpdateBulkRouteResponse> UpdateBulkRoutesAsync(UpdateBulkRouteRequest request);
		Task<bool> SoftDeleteRouteAsync(Guid id);
        Task<bool> ActivateRouteAsync(Guid id);
        Task<bool> DeactivateRouteAsync(Guid id);
		Task<ReplaceAllRoutesResponse> ReplaceAllRoutesAsync(ReplaceAllRoutesRequest request);
	}
}