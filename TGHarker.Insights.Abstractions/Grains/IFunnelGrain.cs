using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IFunnelGrain : IGrainWithStringKey
{
    Task<FunnelInfo> GetInfoAsync();
    Task CreateAsync(string applicationId, CreateFunnelRequest request);
    Task UpdateAsync(string name, List<Models.FunnelStep> steps);
    Task SetActiveAsync(bool isActive);
    Task DeleteAsync();
}
