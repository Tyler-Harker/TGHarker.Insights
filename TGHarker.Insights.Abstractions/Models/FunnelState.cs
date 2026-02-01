using Orleans;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
[Searchable(typeof(IFunnelGrain))]
public sealed class FunnelState
{
    [Id(0)] public string Id { get; set; } = string.Empty;

    [Id(1)]
    [Queryable(Indexed = true)]
    public string ApplicationId { get; set; } = string.Empty;

    [Id(2)]
    [Queryable]
    [FullTextSearchable]
    public string Name { get; set; } = string.Empty;

    [Id(3)] public List<FunnelStep> Steps { get; set; } = [];

    [Id(4)] public DateTime CreatedAt { get; set; }

    [Id(5)]
    [Queryable]
    public bool IsActive { get; set; } = true;
}
