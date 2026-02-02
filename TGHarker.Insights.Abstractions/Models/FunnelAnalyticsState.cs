using Orleans;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
public sealed class FunnelAnalyticsState
{
    [Id(0)] public string FunnelId { get; set; } = string.Empty;
    [Id(1)] public DateTime Date { get; set; }
    [Id(2)] public Dictionary<int, int> StepCompletions { get; set; } = new();
    [Id(3)] public Dictionary<int, HashSet<string>> StepVisitors { get; set; } = new();
    [Id(4)] public string ApplicationId { get; set; } = string.Empty;
    [Id(5)] public string OrganizationId { get; set; } = string.Empty;
}
