using Orleans;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
[Searchable(typeof(IVisitorGrain))]
public sealed class VisitorState
{
    [Id(0)] public string Id { get; set; } = string.Empty;

    [Id(1)]
    [Queryable(Indexed = true)]
    public string ApplicationId { get; set; } = string.Empty;

    [Id(12)]
    [Queryable(Indexed = true)]
    public string OrganizationId { get; set; } = string.Empty;

    [Id(2)]
    [Queryable]
    public string? UserId { get; set; }

    [Id(3)]
    [Queryable]
    public DateTime FirstSeen { get; set; }

    [Id(4)]
    [Queryable]
    public DateTime LastSeen { get; set; }

    [Id(5)]
    [Queryable]
    public int TotalSessions { get; set; }

    [Id(6)]
    [Queryable]
    public int TotalPageViews { get; set; }

    [Id(7)]
    [Queryable]
    public string? Country { get; set; }

    [Id(8)]
    [Queryable]
    public string? City { get; set; }

    [Id(9)] public string? UserAgent { get; set; }

    [Id(10)] public DeviceInfo Device { get; set; } = new();

    [Id(11)]
    [Queryable]
    public Dictionary<string, string> Attributes { get; set; } = new();
}
