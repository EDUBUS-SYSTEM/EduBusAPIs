using Services.Implementations;
using Services.Models.Route;

namespace Services.Contracts
{
	public interface IVrpEngine 
	{
		// Unique name, used to select the engine via config
		string Name { get; }

		Task<RouteSuggestionResponse> GenerateSuggestionsAsync(VRPData data);
	}
}
