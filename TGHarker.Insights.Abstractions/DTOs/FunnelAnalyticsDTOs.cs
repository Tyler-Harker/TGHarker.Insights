using Orleans;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record FunnelDayAnalytics(
    [property: Id(0)] string FunnelId,
    [property: Id(1)] DateTime Date,
    [property: Id(2)] Dictionary<int, int> StepCompletions, // stepOrder -> count
    [property: Id(3)] Dictionary<int, HashSet<string>> StepVisitors // stepOrder -> visitorIds (limited)
);
