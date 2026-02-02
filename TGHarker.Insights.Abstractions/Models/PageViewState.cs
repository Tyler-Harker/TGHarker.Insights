using Orleans;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
[Searchable(typeof(IPageViewGrain))]
public sealed class PageViewState
{
    [Id(0)] public string Id { get; set; } = string.Empty;

    [Id(1)]
    [Queryable(Indexed = true)]
    public string ApplicationId { get; set; } = string.Empty;

    [Id(9)]
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
    public string PagePath { get; set; } = string.Empty;

    [Id(5)]
    [Queryable]
    [FullTextSearchable]
    public string PageTitle { get; set; } = string.Empty;

    [Id(6)]
    [Queryable(Indexed = true)]
    public DateTime Timestamp { get; set; }

    [Id(7)]
    [Queryable]
    public int TimeOnPageSeconds { get; set; }

    [Id(8)]
    [Queryable]
    public int ScrollDepthPercent { get; set; }
}
