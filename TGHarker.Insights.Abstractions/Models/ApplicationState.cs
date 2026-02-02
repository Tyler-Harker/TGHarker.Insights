using Orleans;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
[Searchable(typeof(IApplicationGrain))]
public sealed class ApplicationState
{
    [Id(0)] public string Id { get; set; } = string.Empty;

    [Id(1)]
    [Queryable(Indexed = true)]
    [FullTextSearchable]
    public string Name { get; set; } = string.Empty;

    [Id(2)]
    [Queryable(Indexed = true)]
    public string OwnerId { get; set; } = string.Empty;

    [Id(9)]
    [Queryable(Indexed = true)]
    public string OrganizationId { get; set; } = string.Empty;

    [Id(3)]
    [Queryable]
    public string Domain { get; set; } = string.Empty;

    [Id(4)] public string ApiKey { get; set; } = string.Empty;

    [Id(5)] public DateTime CreatedAt { get; set; }

    [Id(6)]
    [Queryable]
    public bool IsActive { get; set; } = true;

    [Id(7)] public ApplicationSettings Settings { get; set; } = new();

    [Id(8)] public Dictionary<string, UserAttributeDefinition> UserAttributes { get; set; } = new();
}
