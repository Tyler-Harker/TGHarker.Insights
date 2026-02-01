using Orleans;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record RealTimeShardSnapshot(
    [property: Id(0)] int ActiveVisitors,
    [property: Id(1)] Dictionary<string, int> VisitorsByPage,
    [property: Id(2)] HashSet<string> VisitorIds
);
