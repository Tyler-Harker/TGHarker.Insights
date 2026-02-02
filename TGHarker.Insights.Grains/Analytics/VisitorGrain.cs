using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

public class VisitorGrain : Grain, IVisitorGrain
{
    private readonly IPersistentState<VisitorState> _state;

    public VisitorGrain(
        [PersistentState("visitor", "Default")] IPersistentState<VisitorState> state)
    {
        _state = state;
    }

    public Task<VisitorInfo> GetInfoAsync()
    {
        var state = _state.State;
        return Task.FromResult(new VisitorInfo(
            state.Id,
            state.ApplicationId,
            state.UserId,
            state.FirstSeen,
            state.LastSeen,
            state.TotalSessions,
            state.TotalPageViews,
            state.Country,
            state.City,
            state.Device,
            state.Attributes
        ));
    }

    public async Task RecordVisitAsync(VisitData data)
    {
        var now = DateTime.UtcNow;
        var isNewVisitor = string.IsNullOrEmpty(_state.State.Id);

        if (isNewVisitor)
        {
            _state.State.Id = this.GetPrimaryKeyString();
            _state.State.ApplicationId = data.ApplicationId;
            _state.State.OrganizationId = data.OrganizationId;
            _state.State.FirstSeen = now;
        }

        _state.State.LastSeen = now;
        _state.State.TotalSessions++;

        if (data.Country is not null)
            _state.State.Country = data.Country;

        if (data.City is not null)
            _state.State.City = data.City;

        if (data.UserAgent is not null)
            _state.State.UserAgent = data.UserAgent;

        if (data.Device is not null)
            _state.State.Device = data.Device;

        await _state.WriteStateAsync();
    }

    public async Task IdentifyAsync(string userId)
    {
        _state.State.UserId = userId;
        await _state.WriteStateAsync();
    }

    public async Task SetAttributesAsync(Dictionary<string, string> attributes)
    {
        foreach (var kvp in attributes)
        {
            _state.State.Attributes[kvp.Key] = kvp.Value;
        }
        await _state.WriteStateAsync();
    }
}
