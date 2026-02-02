using Orleans;
using TGHarker.Insights.Abstractions.Enums;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record SessionInfo(
    [property: Id(0)] string Id,
    [property: Id(1)] string ApplicationId,
    [property: Id(2)] string VisitorId,
    [property: Id(3)] DateTime StartedAt,
    [property: Id(4)] DateTime? EndedAt,
    [property: Id(5)] int PageViewCount,
    [property: Id(6)] int EventCount,
    [property: Id(7)] int DurationSeconds,
    [property: Id(8)] TrafficSource Source,
    [property: Id(9)] bool IsBounce,
    [property: Id(10)] bool HasConversion
);

[GenerateSerializer]
public record SessionStartData(
    [property: Id(0)] string ApplicationId,
    [property: Id(1)] string VisitorId,
    [property: Id(2)] string? ReferrerUrl,
    [property: Id(3)] string? LandingPage,
    [property: Id(4)] string? UtmSource,
    [property: Id(5)] string? UtmMedium,
    [property: Id(6)] string? UtmCampaign,
    [property: Id(7)] string OrganizationId
);
