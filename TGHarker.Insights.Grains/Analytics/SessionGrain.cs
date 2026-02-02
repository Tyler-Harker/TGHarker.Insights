using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Enums;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

public class SessionGrain : Grain, ISessionGrain
{
    private readonly IPersistentState<SessionState> _state;
    private readonly IGrainFactory _grainFactory;

    public SessionGrain(
        [PersistentState("session", "Default")] IPersistentState<SessionState> state,
        IGrainFactory grainFactory)
    {
        _state = state;
        _grainFactory = grainFactory;
    }

    public async Task StartAsync(SessionStartData data)
    {
        var now = DateTime.UtcNow;

        _state.State = new SessionState
        {
            Id = this.GetPrimaryKeyString(),
            ApplicationId = data.ApplicationId,
            OrganizationId = data.OrganizationId,
            VisitorId = data.VisitorId,
            StartedAt = now,
            ReferrerUrl = data.ReferrerUrl,
            ReferrerDomain = ExtractDomain(data.ReferrerUrl),
            Source = ClassifyTrafficSource(data.ReferrerUrl, data.UtmSource, data.UtmMedium),
            UtmSource = data.UtmSource,
            UtmMedium = data.UtmMedium,
            UtmCampaign = data.UtmCampaign,
            LandingPage = data.LandingPage,
            IsBounce = true
        };

        await _state.WriteStateAsync();

        // Update hourly metrics
        var metricsGrain = _grainFactory.GetGrain<IHourlyMetricsGrain>(
            $"metrics-hourly-{data.ApplicationId}-{now:yyyyMMddHH}");
        await metricsGrain.IncrementSessionsAsync();
        await metricsGrain.IncrementUniqueVisitorsAsync(data.VisitorId);
    }

    public async Task RecordPageViewAsync(PageViewData data)
    {
        _state.State.PageViewCount++;

        if (_state.State.PageViewCount > 1)
            _state.State.IsBounce = false;

        await _state.WriteStateAsync();

        // Update hourly metrics
        var metricsGrain = _grainFactory.GetGrain<IHourlyMetricsGrain>(
            $"metrics-hourly-{_state.State.ApplicationId}-{DateTime.UtcNow:yyyyMMddHH}");
        await metricsGrain.IncrementPageViewsAsync();
    }

    public async Task RecordEventAsync(EventData data)
    {
        _state.State.EventCount++;
        await _state.WriteStateAsync();

        // Update hourly metrics
        var metricsGrain = _grainFactory.GetGrain<IHourlyMetricsGrain>(
            $"metrics-hourly-{_state.State.ApplicationId}-{DateTime.UtcNow:yyyyMMddHH}");
        await metricsGrain.IncrementEventsAsync(data.Category);
    }

    public async Task EndAsync(string? exitPage)
    {
        var now = DateTime.UtcNow;
        _state.State.EndedAt = now;
        _state.State.ExitPage = exitPage;
        _state.State.DurationSeconds = (int)(now - _state.State.StartedAt).TotalSeconds;

        await _state.WriteStateAsync();

        // Update hourly metrics
        var metricsGrain = _grainFactory.GetGrain<IHourlyMetricsGrain>(
            $"metrics-hourly-{_state.State.ApplicationId}-{now:yyyyMMddHH}");

        if (_state.State.IsBounce)
            await metricsGrain.IncrementBouncesAsync();

        await metricsGrain.AddDurationAsync(_state.State.DurationSeconds);
    }

    public Task<SessionInfo> GetInfoAsync()
    {
        var state = _state.State;
        return Task.FromResult(new SessionInfo(
            state.Id,
            state.ApplicationId,
            state.VisitorId,
            state.StartedAt,
            state.EndedAt,
            state.PageViewCount,
            state.EventCount,
            state.DurationSeconds,
            state.Source,
            state.IsBounce,
            state.HasConversion
        ));
    }

    public async Task RecordConversionAsync(string goalId)
    {
        if (!_state.State.ConvertedGoalIds.Contains(goalId))
        {
            _state.State.ConvertedGoalIds.Add(goalId);
            _state.State.HasConversion = true;
            await _state.WriteStateAsync();
        }
    }

    private static string? ExtractDomain(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return null;
        }
    }

    private static TrafficSource ClassifyTrafficSource(string? referrer, string? utmSource, string? utmMedium)
    {
        if (!string.IsNullOrEmpty(utmMedium))
        {
            return utmMedium.ToLowerInvariant() switch
            {
                "cpc" or "ppc" or "paidsearch" => TrafficSource.PaidSearch,
                "organic" => TrafficSource.OrganicSearch,
                "social" => TrafficSource.Social,
                "email" => TrafficSource.Email,
                "display" or "banner" => TrafficSource.Display,
                "affiliate" => TrafficSource.Affiliate,
                "referral" => TrafficSource.Referral,
                _ => TrafficSource.Other
            };
        }

        if (string.IsNullOrEmpty(referrer))
            return TrafficSource.Direct;

        var domain = ExtractDomain(referrer)?.ToLowerInvariant() ?? "";

        if (IsSearchEngine(domain))
            return TrafficSource.OrganicSearch;

        if (IsSocialNetwork(domain))
            return TrafficSource.Social;

        return TrafficSource.Referral;
    }

    private static bool IsSearchEngine(string domain)
    {
        var searchEngines = new[] { "google", "bing", "yahoo", "duckduckgo", "baidu", "yandex" };
        return searchEngines.Any(se => domain.Contains(se));
    }

    private static bool IsSocialNetwork(string domain)
    {
        var socialNetworks = new[] { "facebook", "twitter", "linkedin", "instagram", "pinterest", "reddit", "tiktok", "youtube" };
        return socialNetworks.Any(sn => domain.Contains(sn));
    }
}
