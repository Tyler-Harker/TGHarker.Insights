using Orleans;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
public sealed class UserAttributeDefinition
{
    [Id(0)] public string Key { get; set; } = string.Empty;
    [Id(1)] public bool IsFilterable { get; set; } = true;
    [Id(2)] public DateTime FirstSeen { get; set; }
    [Id(3)] public DateTime LastSeen { get; set; }
}
