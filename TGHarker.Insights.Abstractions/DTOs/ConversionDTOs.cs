using Orleans;
using TGHarker.Insights.Abstractions.Enums;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record ConversionInfo(
    [property: Id(0)] string Id,
    [property: Id(1)] string ApplicationId,
    [property: Id(2)] string GoalId,
    [property: Id(3)] string SessionId,
    [property: Id(4)] string VisitorId,
    [property: Id(5)] DateTime Timestamp,
    [property: Id(6)] decimal? Value,
    [property: Id(7)] TrafficSource Source,
    [property: Id(8)] string? UtmCampaign
);

[GenerateSerializer]
public record ConversionRecordData(
    [property: Id(0)] string ApplicationId,
    [property: Id(1)] string GoalId,
    [property: Id(2)] string SessionId,
    [property: Id(3)] string VisitorId,
    [property: Id(4)] decimal? Value,
    [property: Id(5)] TrafficSource Source,
    [property: Id(6)] string? UtmCampaign,
    [property: Id(7)] string OrganizationId
);
