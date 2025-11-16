
using Services.Models.VietMap;
namespace Services.Contracts
{
    public interface IVietMapService
    {
        Task<RouteResult?> GetRouteAsync(
                double originLat,
                double originLng,
                double destLat,
                double destLng,
                string vehicle = "car");
        Task<double?> CalculateDistanceAsync(
            double originLat, 
            double originLng, 
            double destLat, 
            double destLng);
    }
}
