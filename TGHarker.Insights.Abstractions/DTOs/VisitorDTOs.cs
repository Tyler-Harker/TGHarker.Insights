using Orleans;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record VisitorInfo(
    [property: Id(0)] string Id,
    [property: Id(1)] string ApplicationId,
    [property: Id(2)] string? UserId,
    [property: Id(3)] DateTime FirstSeen,
    [property: Id(4)] DateTime LastSeen,
    [property: Id(5)] int TotalSessions,
    [property: Id(6)] int TotalPageViews,
    [property: Id(7)] string? Country,
    [property: Id(8)] string? City,
    [property: Id(9)] DeviceInfo Device,
    [property: Id(10)] Dictionary<string, string> Attributes
);

[GenerateSerializer]
public record VisitData(
    [property: Id(0)] string ApplicationId,
    [property: Id(1)] string? Country,
    [property: Id(2)] string? City,
    [property: Id(3)] string? UserAgent,
    [property: Id(4)] DeviceInfo? Device,
    [property: Id(5)] string OrganizationId
);
