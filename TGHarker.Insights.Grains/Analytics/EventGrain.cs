using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

public class EventGrain : Grain, IEventGrain
{
    private readonly IPersistentState<EventState> _state;

    public EventGrain(
        [PersistentState("event", "Default")] IPersistentState<EventState> state)
    {
        _state = state;
    }

    public async Task RecordAsync(EventRecordData data)
    {
        _state.State = new EventState
        {
            Id = this.GetPrimaryKeyString(),
            ApplicationId = data.ApplicationId,
            OrganizationId = data.OrganizationId,
            SessionId = data.SessionId,
            VisitorId = data.VisitorId,
            Category = data.Category,
            Action = data.Action,
            Label = data.Label,
            Value = data.Value,
            Timestamp = data.Timestamp,
            CustomProperties = data.CustomProperties ?? new Dictionary<string, string>()
        };

        await _state.WriteStateAsync();
    }

    public Task<EventInfo> GetInfoAsync()
    {
        var state = _state.State;
        return Task.FromResult(new EventInfo(
            state.Id,
            state.ApplicationId,
            state.SessionId,
            state.VisitorId,
            state.Category,
            state.Action,
            state.Label,
            state.Value,
            state.Timestamp
        ));
    }
}
