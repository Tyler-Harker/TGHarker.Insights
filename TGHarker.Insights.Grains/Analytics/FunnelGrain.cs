using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

public class FunnelGrain : Grain, IFunnelGrain
{
    private readonly IPersistentState<FunnelState> _state;

    public FunnelGrain(
        [PersistentState("funnel", "Default")] IPersistentState<FunnelState> state)
    {
        _state = state;
    }

    public Task<FunnelInfo> GetInfoAsync()
    {
        var state = _state.State;
        return Task.FromResult(new FunnelInfo(
            state.Id,
            state.Name,
            state.ApplicationId,
            state.Steps,
            state.CreatedAt,
            state.IsActive
        ));
    }

    public async Task CreateAsync(string applicationId, string organizationId, CreateFunnelRequest request)
    {
        _state.State = new FunnelState
        {
            Id = this.GetPrimaryKeyString(),
            ApplicationId = applicationId,
            OrganizationId = organizationId,
            Name = request.Name,
            Steps = request.Steps,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _state.WriteStateAsync();
    }

    public async Task UpdateAsync(string name, List<FunnelStep> steps)
    {
        _state.State.Name = name;
        _state.State.Steps = steps;
        await _state.WriteStateAsync();
    }

    public async Task SetActiveAsync(bool isActive)
    {
        _state.State.IsActive = isActive;
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }
}
