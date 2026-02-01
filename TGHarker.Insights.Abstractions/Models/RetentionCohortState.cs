using Orleans;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
[Searchable(typeof(IRetentionCohortGrain))]
public sealed class RetentionCohortState
{
    [Id(0)] public string Id { get; set; } = string.Empty;

    [Id(1)]
    [Queryable(Indexed = true)]
    public string ApplicationId { get; set; } = string.Empty;

    [Id(2)]
    [Queryable(Indexed = true)]
    public string CohortWeek { get; set; } = string.Empty;

    [Id(3)]
    [Queryable]
    public int TotalVisitors { get; set; }

    [Id(4)] public HashSet<string> VisitorIds { get; set; } = [];

    [Id(5)] public Dictionary<int, HashSet<string>> ReturnsByWeek { get; set; } = new();
}
