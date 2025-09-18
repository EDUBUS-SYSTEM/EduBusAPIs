using Services.Models.Route;

namespace Services.Contracts
{
    public interface IRouteSuggestionService
    {
        /// <summary>
        /// Generate route suggestions for student pickup
        /// </summary>
        Task<RouteSuggestionResponse> GenerateRouteSuggestionsAsync(RouteSuggestionRequest request);
        
        /// <summary>
        /// Optimize existing route
        /// </summary>
        Task<RouteSuggestionResponse> OptimizeExistingRouteAsync(Guid routeId);
    }
}
