using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

/// <summary>
/// Tracks funnel step completions per day with buffered writes for scalability.
/// </summary>
public class FunnelAnalyticsGrain : Grain, IFunnelAnalyticsGrain
{
    private readonly IPersistentState<FunnelAnalyticsState> _state;

    // Buffer for batch writes
    private readonly Dictionary<int, HashSet<string>> _bufferedCompletions = new();
    private bool _isDirty;
    private IGrainTimer? _flushTimer;

    // Limit tracked visitor IDs per step to prevent unbounded growth
    private const int MaxVisitorIdsPerStep = 10000;

    public FunnelAnalyticsGrain(
        [PersistentState("funnelanalytics", "Default")] IPersistentState<FunnelAnalyticsState> state)
    {
        _state = state;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_state.State.FunnelId))
        {
            var key = this.GetPrimaryKeyString();
            // Key format: funnel-analytics-{funnelId}-{yyyyMMdd}
            var parts = key.Split('-');
            if (parts.Length >= 4)
            {
                _state.State.FunnelId = string.Join("-", parts.Skip(2).Take(parts.Length - 3));
                if (DateTime.TryParseExact(parts.Last(), "yyyyMMdd",
                    null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    _state.State.Date = date;
                }
            }
        }

        _flushTimer = this.RegisterGrainTimer(
            static (state, _) => state.FlushBufferAsync(),
            this,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(10),
                Period = TimeSpan.FromSeconds(10)
            });

        return base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _flushTimer?.Dispose();
        await FlushBufferAsync();
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    public Task RecordStepCompletionAsync(string visitorId, int stepOrder)
    {
        if (!_bufferedCompletions.TryGetValue(stepOrder, out var visitors))
        {
            visitors = new HashSet<string>();
            _bufferedCompletions[stepOrder] = visitors;
        }

        visitors.Add(visitorId);
        _isDirty = true;
        return Task.CompletedTask;
    }

    public Task<FunnelDayAnalytics> GetAnalyticsAsync()
    {
        // Merge buffered data with persisted state
        var stepCompletions = new Dictionary<int, int>(_state.State.StepCompletions);
        var stepVisitors = new Dictionary<int, HashSet<string>>();

        foreach (var (step, visitors) in _state.State.StepVisitors)
        {
            stepVisitors[step] = new HashSet<string>(visitors);
        }

        foreach (var (step, visitors) in _bufferedCompletions)
        {
            if (!stepVisitors.TryGetValue(step, out var existing))
            {
                existing = new HashSet<string>();
                stepVisitors[step] = existing;
            }

            foreach (var visitorId in visitors)
            {
                if (existing.Add(visitorId))
                {
                    stepCompletions[step] = stepCompletions.GetValueOrDefault(step) + 1;
                }
            }
        }

        return Task.FromResult(new FunnelDayAnalytics(
            _state.State.FunnelId,
            _state.State.Date,
            stepCompletions,
            stepVisitors
        ));
    }

    private async Task FlushBufferAsync()
    {
        if (!_isDirty)
            return;

        foreach (var (step, visitors) in _bufferedCompletions)
        {
            if (!_state.State.StepVisitors.TryGetValue(step, out var existingVisitors))
            {
                existingVisitors = new HashSet<string>();
                _state.State.StepVisitors[step] = existingVisitors;
            }

            foreach (var visitorId in visitors)
            {
                // Only track individual visitors up to limit
                if (existingVisitors.Count < MaxVisitorIdsPerStep)
                {
                    if (existingVisitors.Add(visitorId))
                    {
                        _state.State.StepCompletions[step] = _state.State.StepCompletions.GetValueOrDefault(step) + 1;
                    }
                }
                else
                {
                    // At scale, just increment (approximate)
                    _state.State.StepCompletions[step] = _state.State.StepCompletions.GetValueOrDefault(step) + 1;
                }
            }
        }

        _bufferedCompletions.Clear();
        _isDirty = false;

        await _state.WriteStateAsync();
    }
}
