using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IDailyMetricsGrain : IGrainWithStringKey
{
    Task AggregateFromHourlyAsync(HourlyMetrics hourlyMetrics);
    Task<DailyMetrics> GetMetricsAsync();
}
