using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

public class HourlyMetricsGrain : Grain, IHourlyMetricsGrain
{
    private readonly IPersistentState<HourlyMetricsState> _state;
    private readonly IGrainFactory _grainFactory;

    // Buffered updates to reduce write frequency
    private int _bufferedPageViews;
    private int _bufferedSessions;
    private int _bufferedEvents;
    private int _bufferedBounces;
    private int _bufferedDuration;
    private readonly Dictionary<string, int> _bufferedEventsByCategory = new();
    private readonly HashSet<string> _bufferedVisitorIds = new();
    private bool _isDirty;
    private IGrainTimer? _flushTimer;
    private bool _organizationIdResolved;

    // Use a bloom filter approximation for unique visitors at scale
    // For production, consider using a proper HyperLogLog implementation
    private const int MaxUniqueVisitorIdsToTrack = 10000;

    public HourlyMetricsGrain(
        [PersistentState("hourlymetrics", "Default")] IPersistentState<HourlyMetricsState> state,
        IGrainFactory grainFactory)
    {
        _state = state;
        _grainFactory = grainFactory;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_state.State.Id))
        {
            var key = this.GetPrimaryKeyString();
            var parts = key.Split('-');

            _state.State.Id = key;
            _state.State.ApplicationId = string.Join("-", parts.Skip(2).Take(parts.Length - 3));

            if (DateTime.TryParseExact(parts.Last(), "yyyyMMddHH",
                null, System.Globalization.DateTimeStyles.None, out var hourStart))
            {
                _state.State.HourStart = hourStart;
            }
        }

        _organizationIdResolved = !string.IsNullOrEmpty(_state.State.OrganizationId);

        // Flush buffered updates every 5 seconds to reduce write amplification
        _flushTimer = this.RegisterGrainTimer(
            static (state, _) => state.FlushBufferAsync(),
            this,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(5),
                Period = TimeSpan.FromSeconds(5)
            });

        return base.OnActivateAsync(cancellationToken);
    }

    private async Task EnsureOrganizationIdAsync()
    {
        if (_organizationIdResolved)
            return;

        var applicationGrain = _grainFactory.GetGrain<IApplicationGrain>($"app-{_state.State.ApplicationId}");
        var appInfo = await applicationGrain.GetInfoAsync();
        _state.State.OrganizationId = appInfo.OrganizationId;
        _organizationIdResolved = true;
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _flushTimer?.Dispose();
        await FlushBufferAsync(); // Ensure buffered data is persisted
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    private async Task FlushBufferAsync()
    {
        if (!_isDirty)
            return;

        await EnsureOrganizationIdAsync();

        _state.State.PageViews += _bufferedPageViews;
        _state.State.Sessions += _bufferedSessions;
        _state.State.Events += _bufferedEvents;
        _state.State.Bounces += _bufferedBounces;
        _state.State.TotalDurationSeconds += _bufferedDuration;

        foreach (var (category, count) in _bufferedEventsByCategory)
        {
            if (_state.State.EventsByCategory.TryGetValue(category, out var existing))
            {
                _state.State.EventsByCategory[category] = existing + count;
            }
            else
            {
                _state.State.EventsByCategory[category] = count;
            }
        }

        // Only track individual visitor IDs up to a limit to prevent unbounded growth
        if (_state.State.UniqueVisitorIds.Count < MaxUniqueVisitorIdsToTrack)
        {
            foreach (var visitorId in _bufferedVisitorIds)
            {
                if (_state.State.UniqueVisitorIds.Count >= MaxUniqueVisitorIdsToTrack)
                    break;

                if (_state.State.UniqueVisitorIds.Add(visitorId))
                {
                    _state.State.UniqueVisitors++;
                }
            }
        }
        else
        {
            // At scale, just increment counter (approximate counting)
            _state.State.UniqueVisitors += _bufferedVisitorIds.Count;
        }

        // Reset buffers
        _bufferedPageViews = 0;
        _bufferedSessions = 0;
        _bufferedEvents = 0;
        _bufferedBounces = 0;
        _bufferedDuration = 0;
        _bufferedEventsByCategory.Clear();
        _bufferedVisitorIds.Clear();
        _isDirty = false;

        await _state.WriteStateAsync();
    }

    public Task IncrementPageViewsAsync()
    {
        _bufferedPageViews++;
        _isDirty = true;
        return Task.CompletedTask;
    }

    public Task IncrementSessionsAsync()
    {
        _bufferedSessions++;
        _isDirty = true;
        return Task.CompletedTask;
    }

    public Task IncrementUniqueVisitorsAsync(string visitorId)
    {
        _bufferedVisitorIds.Add(visitorId);
        _isDirty = true;
        return Task.CompletedTask;
    }

    public Task IncrementEventsAsync(string category)
    {
        _bufferedEvents++;
        if (_bufferedEventsByCategory.TryGetValue(category, out var count))
        {
            _bufferedEventsByCategory[category] = count + 1;
        }
        else
        {
            _bufferedEventsByCategory[category] = 1;
        }
        _isDirty = true;
        return Task.CompletedTask;
    }

    public async Task IncrementConversionsAsync(string goalId, decimal? value)
    {
        await EnsureOrganizationIdAsync();

        // Conversions are important - write immediately
        _state.State.Conversions++;
        _state.State.ConversionValue += value ?? 0;

        if (!_state.State.ConversionsByGoal.TryAdd(goalId, 1))
        {
            _state.State.ConversionsByGoal[goalId]++;
        }

        await _state.WriteStateAsync();
    }

    public Task IncrementBouncesAsync()
    {
        _bufferedBounces++;
        _isDirty = true;
        return Task.CompletedTask;
    }

    public Task DecrementBouncesAsync()
    {
        _bufferedBounces--;
        _isDirty = true;
        return Task.CompletedTask;
    }

    public Task AddDurationAsync(int seconds)
    {
        _bufferedDuration += seconds;
        _isDirty = true;
        return Task.CompletedTask;
    }

    public Task<HourlyMetrics> GetMetricsAsync()
    {
        // Include buffered data in the response for accuracy
        var state = _state.State;
        return Task.FromResult(new HourlyMetrics(
            state.ApplicationId,
            state.HourStart,
            state.PageViews + _bufferedPageViews,
            state.Sessions + _bufferedSessions,
            state.UniqueVisitors + _bufferedVisitorIds.Count,
            state.Events + _bufferedEvents,
            state.Conversions,
            state.ConversionValue,
            state.Bounces + _bufferedBounces,
            state.TotalDurationSeconds + _bufferedDuration,
            MergeCategories(state.EventsByCategory, _bufferedEventsByCategory),
            new Dictionary<string, int>(state.ConversionsByGoal)
        ));
    }

    private static Dictionary<string, int> MergeCategories(
        Dictionary<string, int> persisted,
        Dictionary<string, int> buffered)
    {
        var result = new Dictionary<string, int>(persisted);
        foreach (var (category, count) in buffered)
        {
            if (result.TryGetValue(category, out var existing))
            {
                result[category] = existing + count;
            }
            else
            {
                result[category] = count;
            }
        }
        return result;
    }
}
