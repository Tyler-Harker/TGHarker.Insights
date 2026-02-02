using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

public class ConversionGrain : Grain, IConversionGrain
{
    private readonly IPersistentState<ConversionState> _state;
    private readonly IGrainFactory _grainFactory;

    public ConversionGrain(
        [PersistentState("conversion", "Default")] IPersistentState<ConversionState> state,
        IGrainFactory grainFactory)
    {
        _state = state;
        _grainFactory = grainFactory;
    }

    public async Task RecordAsync(ConversionRecordData data)
    {
        var now = DateTime.UtcNow;

        _state.State = new ConversionState
        {
            Id = this.GetPrimaryKeyString(),
            ApplicationId = data.ApplicationId,
            OrganizationId = data.OrganizationId,
            GoalId = data.GoalId,
            SessionId = data.SessionId,
            VisitorId = data.VisitorId,
            Timestamp = now,
            Value = data.Value,
            Source = data.Source,
            UtmCampaign = data.UtmCampaign
        };

        await _state.WriteStateAsync();

        // Update goal conversion count
        var goalGrain = _grainFactory.GetGrain<IGoalGrain>($"goal-{data.ApplicationId}-{data.GoalId}");
        await goalGrain.RecordConversionAsync();

        // Update hourly metrics
        var metricsGrain = _grainFactory.GetGrain<IHourlyMetricsGrain>(
            $"metrics-hourly-{data.ApplicationId}-{now:yyyyMMddHH}");
        await metricsGrain.IncrementConversionsAsync(data.GoalId, data.Value);
    }

    public Task<ConversionInfo> GetInfoAsync()
    {
        var state = _state.State;
        return Task.FromResult(new ConversionInfo(
            state.Id,
            state.ApplicationId,
            state.GoalId,
            state.SessionId,
            state.VisitorId,
            state.Timestamp,
            state.Value,
            state.Source,
            state.UtmCampaign
        ));
    }
}
