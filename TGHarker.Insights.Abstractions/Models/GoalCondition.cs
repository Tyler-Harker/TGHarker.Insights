using Orleans;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
public sealed class GoalCondition
{
    [Id(0)] public string? UrlPattern { get; set; }
    [Id(1)] public string? EventCategory { get; set; }
    [Id(2)] public string? EventAction { get; set; }
    [Id(3)] public int? MinDurationSeconds { get; set; }
    [Id(4)] public int? MinPagesViewed { get; set; }
}
