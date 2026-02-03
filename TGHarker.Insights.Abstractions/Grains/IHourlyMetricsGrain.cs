using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IHourlyMetricsGrain : IGrainWithStringKey
{
    Task IncrementPageViewsAsync();
    Task IncrementSessionsAsync();
    Task IncrementUniqueVisitorsAsync(string visitorId);
    Task IncrementEventsAsync(string category);
    Task IncrementConversionsAsync(string goalId, decimal? value);
    Task IncrementBouncesAsync();
    Task DecrementBouncesAsync();
    Task AddDurationAsync(int seconds);
    Task<HourlyMetrics> GetMetricsAsync();
}
