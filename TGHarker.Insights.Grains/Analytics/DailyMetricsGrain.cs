using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

public class DailyMetricsGrain : Grain, IDailyMetricsGrain
{
    private readonly IPersistentState<DailyMetricsState> _state;

    public DailyMetricsGrain(
        [PersistentState("dailymetrics", "Default")] IPersistentState<DailyMetricsState> state)
    {
        _state = state;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_state.State.Id))
        {
            var key = this.GetPrimaryKeyString();
            var parts = key.Split('-');

            _state.State.Id = key;
            _state.State.ApplicationId = string.Join("-", parts.Skip(2).Take(parts.Length - 3));

            if (DateTime.TryParseExact(parts.Last(), "yyyyMMdd",
                null, System.Globalization.DateTimeStyles.None, out var date))
            {
                _state.State.Date = date;
            }
        }

        return base.OnActivateAsync(cancellationToken);
    }

    public async Task AggregateFromHourlyAsync(HourlyMetrics hourlyMetrics)
    {
        _state.State.PageViews += hourlyMetrics.PageViews;
        _state.State.Sessions += hourlyMetrics.Sessions;
        _state.State.Events += hourlyMetrics.Events;
        _state.State.Conversions += hourlyMetrics.Conversions;
        _state.State.ConversionValue += hourlyMetrics.ConversionValue;
        _state.State.Bounces += hourlyMetrics.Bounces;
        _state.State.TotalDurationSeconds += hourlyMetrics.TotalDurationSeconds;

        await _state.WriteStateAsync();
    }

    public Task<DailyMetrics> GetMetricsAsync()
    {
        var state = _state.State;
        return Task.FromResult(new DailyMetrics(
            state.ApplicationId,
            state.Date,
            state.PageViews,
            state.Sessions,
            state.UniqueVisitors,
            state.Events,
            state.Conversions,
            state.ConversionValue,
            state.Bounces,
            state.TotalDurationSeconds,
            new Dictionary<string, int>(state.PageViewsByPath),
            new Dictionary<string, int>(state.SessionsBySource)
        ));
    }
}
