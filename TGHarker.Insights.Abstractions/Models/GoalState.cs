using Orleans;
using TGHarker.Insights.Abstractions.Enums;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
[Searchable(typeof(IGoalGrain))]
public sealed class GoalState
{
    [Id(0)] public string Id { get; set; } = string.Empty;

    [Id(1)]
    [Queryable(Indexed = true)]
    public string ApplicationId { get; set; } = string.Empty;

    [Id(9)]
    [Queryable(Indexed = true)]
    public string OrganizationId { get; set; } = string.Empty;

    [Id(2)]
    [Queryable]
    [FullTextSearchable]
    public string Name { get; set; } = string.Empty;

    [Id(3)]
    [Queryable]
    public GoalType Type { get; set; }

    [Id(4)] public GoalCondition Condition { get; set; } = new();

    [Id(5)]
    [Queryable]
    public decimal? MonetaryValue { get; set; }

    [Id(6)]
    [Queryable]
    public bool IsActive { get; set; } = true;

    [Id(7)]
    [Queryable]
    public int TotalConversions { get; set; }

    [Id(8)] public DateTime CreatedAt { get; set; }
}
