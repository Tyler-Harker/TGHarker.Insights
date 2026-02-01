using Orleans;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
public sealed class FunnelDefinition
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public string ApplicationId { get; set; } = string.Empty;
    [Id(3)] public List<FunnelStep> Steps { get; set; } = [];
    [Id(4)] public DateTime CreatedAt { get; set; }
    [Id(5)] public bool IsActive { get; set; } = true;
}

[GenerateSerializer]
public sealed class FunnelStep
{
    [Id(0)] public int Order { get; set; }
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public FunnelStepType Type { get; set; }
    [Id(3)] public string? PagePath { get; set; }
    [Id(4)] public string? EventCategory { get; set; }
    [Id(5)] public string? EventAction { get; set; }
}

public enum FunnelStepType
{
    PageVisit,
    Event
}
