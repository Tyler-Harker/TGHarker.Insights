using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

/// <summary>
/// Tracks funnel step completions in real-time for a specific time period.
/// Key format: funnel-analytics-{funnelId}-{yyyyMMdd}
/// </summary>
public interface IFunnelAnalyticsGrain : IGrainWithStringKey
{
    /// <summary>
    /// Record that a visitor completed a funnel step.
    /// </summary>
    Task RecordStepCompletionAsync(string visitorId, int stepOrder);

    /// <summary>
    /// Get analytics for all steps.
    /// </summary>
    Task<FunnelDayAnalytics> GetAnalyticsAsync();
}

/// <summary>
/// Aggregates funnel analytics across multiple days.
/// Key format: funnel-summary-{funnelId}
/// </summary>
public interface IFunnelSummaryGrain : IGrainWithStringKey
{
    Task<FunnelAnalytics> GetAnalyticsAsync(DateTime from, DateTime to);
}
