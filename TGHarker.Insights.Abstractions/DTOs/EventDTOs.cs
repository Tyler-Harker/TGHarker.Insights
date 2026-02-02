using Orleans;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record EventInfo(
    [property: Id(0)] string Id,
    [property: Id(1)] string ApplicationId,
    [property: Id(2)] string SessionId,
    [property: Id(3)] string VisitorId,
    [property: Id(4)] string Category,
    [property: Id(5)] string Action,
    [property: Id(6)] string? Label,
    [property: Id(7)] decimal? Value,
    [property: Id(8)] DateTime Timestamp
);

[GenerateSerializer]
public record EventData(
    [property: Id(0)] string Category,
    [property: Id(1)] string Action,
    [property: Id(2)] string? Label,
    [property: Id(3)] decimal? Value,
    [property: Id(4)] DateTime Timestamp
);

[GenerateSerializer]
public record EventRecordData(
    [property: Id(0)] string ApplicationId,
    [property: Id(1)] string SessionId,
    [property: Id(2)] string VisitorId,
    [property: Id(3)] string Category,
    [property: Id(4)] string Action,
    [property: Id(5)] string? Label,
    [property: Id(6)] decimal? Value,
    [property: Id(7)] DateTime Timestamp,
    [property: Id(8)] Dictionary<string, string>? CustomProperties,
    [property: Id(9)] string OrganizationId
);
