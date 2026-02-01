using Orleans;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
[Searchable(typeof(IHourlyMetricsGrain))]
public sealed class HourlyMetricsState
{
    [Id(0)] public string Id { get; set; } = string.Empty;

    [Id(1)]
    [Queryable(Indexed = true)]
    public string ApplicationId { get; set; } = string.Empty;

    [Id(2)]
    [Queryable(Indexed = true)]
    public DateTime HourStart { get; set; }

    [Id(3)]
    [Queryable]
    public int PageViews { get; set; }

    [Id(4)]
    [Queryable]
    public int Sessions { get; set; }

    [Id(5)]
    [Queryable]
    public int UniqueVisitors { get; set; }

    [Id(6)]
    [Queryable]
    public int Events { get; set; }

    [Id(7)]
    [Queryable]
    public int Conversions { get; set; }

    [Id(8)]
    [Queryable]
    public decimal ConversionValue { get; set; }

    [Id(9)]
    [Queryable]
    public int Bounces { get; set; }

    [Id(10)]
    [Queryable]
    public int TotalDurationSeconds { get; set; }

    [Id(11)] public HashSet<string> UniqueVisitorIds { get; set; } = [];
    [Id(12)] public Dictionary<string, int> EventsByCategory { get; set; } = new();
    [Id(13)] public Dictionary<string, int> ConversionsByGoal { get; set; } = new();
}
