using Orleans;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record FunnelInfo(
    [property: Id(0)] string Id,
    [property: Id(1)] string Name,
    [property: Id(2)] string ApplicationId,
    [property: Id(3)] List<FunnelStep> Steps,
    [property: Id(4)] DateTime CreatedAt,
    [property: Id(5)] bool IsActive
);

[GenerateSerializer]
public record CreateFunnelRequest(
    [property: Id(0)] string Name,
    [property: Id(1)] List<FunnelStep> Steps
);

[GenerateSerializer]
public record FunnelAnalytics(
    [property: Id(0)] string FunnelId,
    [property: Id(1)] string FunnelName,
    [property: Id(2)] List<FunnelStepAnalytics> Steps,
    [property: Id(3)] int TotalEntries,
    [property: Id(4)] int TotalCompletions,
    [property: Id(5)] double OverallConversionRate
);

[GenerateSerializer]
public record FunnelStepAnalytics(
    [property: Id(0)] int Order,
    [property: Id(1)] string Name,
    [property: Id(2)] FunnelStepType Type,
    [property: Id(3)] int Visitors,
    [property: Id(4)] double ConversionRate,
    [property: Id(5)] double DropOffRate
);
