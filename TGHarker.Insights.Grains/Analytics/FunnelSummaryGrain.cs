using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

/// <summary>
/// Aggregates funnel analytics across multiple days.
/// Uses pre-computed daily analytics for scalability.
/// </summary>
public class FunnelSummaryGrain : Grain, IFunnelSummaryGrain
{
    public async Task<FunnelAnalytics> GetAnalyticsAsync(DateTime from, DateTime to)
    {
        var key = this.GetPrimaryKeyString();
        // Key format: funnel-summary-{applicationId}-{funnelId}
        var parts = key.Replace("funnel-summary-", "").Split('-');
        var funnelGrainKey = "funnel-" + string.Join("-", parts);

        // Get funnel definition
        var funnelGrain = GrainFactory.GetGrain<IFunnelGrain>(funnelGrainKey);
        var funnelInfo = await funnelGrain.GetInfoAsync();

        if (string.IsNullOrEmpty(funnelInfo.Id))
        {
            return new FunnelAnalytics(
                "",
                "Unknown",
                [],
                0, 0, 0
            );
        }

        // Aggregate daily analytics
        var aggregatedStepCompletions = new Dictionary<int, int>();
        var aggregatedStepVisitors = new Dictionary<int, HashSet<string>>();

        var current = from.Date;
        var tasks = new List<Task<FunnelDayAnalytics>>();

        while (current <= to.Date)
        {
            var dayGrainKey = $"funnel-analytics-{funnelInfo.ApplicationId}-{funnelInfo.Id}-{current:yyyyMMdd}";
            var dayGrain = GrainFactory.GetGrain<IFunnelAnalyticsGrain>(dayGrainKey);
            tasks.Add(dayGrain.GetAnalyticsAsync());
            current = current.AddDays(1);
        }

        var dailyAnalytics = await Task.WhenAll(tasks);

        foreach (var day in dailyAnalytics)
        {
            foreach (var (step, count) in day.StepCompletions)
            {
                aggregatedStepCompletions[step] = aggregatedStepCompletions.GetValueOrDefault(step) + count;
            }

            foreach (var (step, visitors) in day.StepVisitors)
            {
                if (!aggregatedStepVisitors.TryGetValue(step, out var existing))
                {
                    existing = new HashSet<string>();
                    aggregatedStepVisitors[step] = existing;
                }
                existing.UnionWith(visitors);
            }
        }

        // Build step analytics
        var stepAnalytics = new List<FunnelStepAnalytics>();
        var previousVisitors = aggregatedStepVisitors.GetValueOrDefault(1)?.Count ?? 0;

        foreach (var step in funnelInfo.Steps.OrderBy(s => s.Order))
        {
            var visitors = aggregatedStepVisitors.GetValueOrDefault(step.Order)?.Count ?? 0;
            var conversionRate = previousVisitors > 0 ? (double)visitors / previousVisitors * 100 : 0;
            var dropOffRate = previousVisitors > 0 ? (double)(previousVisitors - visitors) / previousVisitors * 100 : 0;

            stepAnalytics.Add(new FunnelStepAnalytics(
                step.Order,
                step.Name,
                step.Type,
                visitors,
                step.Order == 1 ? 100 : conversionRate,
                step.Order == 1 ? 0 : dropOffRate
            ));

            previousVisitors = visitors;
        }

        var totalEntries = stepAnalytics.FirstOrDefault()?.Visitors ?? 0;
        var totalCompletions = stepAnalytics.LastOrDefault()?.Visitors ?? 0;
        var overallConversionRate = totalEntries > 0 ? (double)totalCompletions / totalEntries * 100 : 0;

        return new FunnelAnalytics(
            funnelInfo.Id,
            funnelInfo.Name,
            stepAnalytics,
            totalEntries,
            totalCompletions,
            overallConversionRate
        );
    }
}
