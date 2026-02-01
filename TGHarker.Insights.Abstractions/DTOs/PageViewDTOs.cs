using Orleans;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record PageViewInfo(
    [property: Id(0)] string Id,
    [property: Id(1)] string ApplicationId,
    [property: Id(2)] string SessionId,
    [property: Id(3)] string VisitorId,
    [property: Id(4)] string PagePath,
    [property: Id(5)] string PageTitle,
    [property: Id(6)] DateTime Timestamp,
    [property: Id(7)] int TimeOnPageSeconds,
    [property: Id(8)] int ScrollDepthPercent
);

[GenerateSerializer]
public record PageViewData(
    [property: Id(0)] string PagePath,
    [property: Id(1)] string PageTitle,
    [property: Id(2)] DateTime Timestamp
);

[GenerateSerializer]
public record PageViewRecordData(
    [property: Id(0)] string ApplicationId,
    [property: Id(1)] string SessionId,
    [property: Id(2)] string VisitorId,
    [property: Id(3)] string PagePath,
    [property: Id(4)] string PageTitle,
    [property: Id(5)] DateTime Timestamp
);
