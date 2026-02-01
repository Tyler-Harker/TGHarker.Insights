using Orleans;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record HourlyMetrics(
    [property: Id(0)] string ApplicationId,
    [property: Id(1)] DateTime HourStart,
    [property: Id(2)] int PageViews,
    [property: Id(3)] int Sessions,
    [property: Id(4)] int UniqueVisitors,
    [property: Id(5)] int Events,
    [property: Id(6)] int Conversions,
    [property: Id(7)] decimal ConversionValue,
    [property: Id(8)] int Bounces,
    [property: Id(9)] int TotalDurationSeconds,
    [property: Id(10)] Dictionary<string, int> EventsByCategory,
    [property: Id(11)] Dictionary<string, int> ConversionsByGoal
);

[GenerateSerializer]
public record DailyMetrics(
    [property: Id(0)] string ApplicationId,
    [property: Id(1)] DateTime Date,
    [property: Id(2)] int PageViews,
    [property: Id(3)] int Sessions,
    [property: Id(4)] int UniqueVisitors,
    [property: Id(5)] int Events,
    [property: Id(6)] int Conversions,
    [property: Id(7)] decimal ConversionValue,
    [property: Id(8)] int Bounces,
    [property: Id(9)] int TotalDurationSeconds,
    [property: Id(10)] Dictionary<string, int> PageViewsByPath,
    [property: Id(11)] Dictionary<string, int> SessionsBySource
);

[GenerateSerializer]
public record RealTimeSnapshot(
    [property: Id(0)] int ActiveVisitors,
    [property: Id(1)] Dictionary<string, int> VisitorsByPage,
    [property: Id(2)] DateTime Timestamp
);

[GenerateSerializer]
public record RetentionCohortData(
    [property: Id(0)] string ApplicationId,
    [property: Id(1)] string CohortWeek,
    [property: Id(2)] int TotalVisitors,
    [property: Id(3)] Dictionary<int, int> RetentionByWeek
);
