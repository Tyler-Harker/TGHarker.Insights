using Orleans;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
[Searchable(typeof(IEventGrain))]
public sealed class EventState
{
    [Id(0)] public string Id { get; set; } = string.Empty;

    [Id(1)]
    [Queryable(Indexed = true)]
    public string ApplicationId { get; set; } = string.Empty;

    [Id(10)]
    [Queryable(Indexed = true)]
    public string OrganizationId { get; set; } = string.Empty;

    [Id(2)]
    [Queryable(Indexed = true)]
    public string SessionId { get; set; } = string.Empty;

    [Id(3)]
    [Queryable(Indexed = true)]
    public string VisitorId { get; set; } = string.Empty;

    [Id(4)]
    [Queryable(Indexed = true)]
    public string Category { get; set; } = string.Empty;

    [Id(5)]
    [Queryable(Indexed = true)]
    public string Action { get; set; } = string.Empty;

    [Id(6)]
    [Queryable]
    [FullTextSearchable]
    public string? Label { get; set; }

    [Id(7)]
    [Queryable]
    public decimal? Value { get; set; }

    [Id(8)]
    [Queryable(Indexed = true)]
    public DateTime Timestamp { get; set; }

    [Id(9)] public Dictionary<string, string> CustomProperties { get; set; } = new();
}
