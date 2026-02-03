using Orleans;
using TGHarker.Insights.Abstractions.Enums;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Abstractions.Attributes;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
[Searchable(typeof(ISessionGrain))]
public sealed class SessionState
{
    [Id(0)] public string Id { get; set; } = string.Empty;

    [Id(1)]
    [Queryable(Indexed = true)]
    public string ApplicationId { get; set; } = string.Empty;

    [Id(19)]
    [Queryable(Indexed = true)]
    public string OrganizationId { get; set; } = string.Empty;

    [Id(2)]
    [Queryable(Indexed = true)]
    public string VisitorId { get; set; } = string.Empty;

    [Id(3)]
    [Queryable(Indexed = true)]
    public DateTime StartedAt { get; set; }

    [Id(4)]
    [Queryable]
    public DateTime? EndedAt { get; set; }

    [Id(5)]
    [Queryable]
    public int PageViewCount { get; set; }

    [Id(6)]
    [Queryable]
    public int EventCount { get; set; }

    [Id(7)]
    [Queryable]
    public int DurationSeconds { get; set; }

    [Id(8)]
    [Queryable]
    public string? ReferrerUrl { get; set; }

    [Id(9)]
    [Queryable(Indexed = true)]
    public string? ReferrerDomain { get; set; }

    [Id(10)]
    [Queryable(Indexed = true)]
    public TrafficSource Source { get; set; }

    [Id(11)]
    [Queryable]
    public string? UtmSource { get; set; }

    [Id(12)]
    [Queryable]
    public string? UtmMedium { get; set; }

    [Id(13)]
    [Queryable]
    public string? UtmCampaign { get; set; }

    [Id(14)]
    [Queryable]
    public string? LandingPage { get; set; }

    [Id(15)]
    [Queryable]
    public string? ExitPage { get; set; }

    [Id(16)]
    [Queryable]
    public bool IsBounce { get; set; }

    [Id(17)]
    [Queryable]
    public bool HasConversion { get; set; }

    [Id(18)] public List<string> ConvertedGoalIds { get; set; } = [];

    /// <summary>
    /// Tracks which hourly metrics grain key the bounce was counted in (if any).
    /// Used to decrement bounces when session becomes non-bounce.
    /// </summary>
    [Id(20)] public string? BounceCountedInHour { get; set; }

    /// <summary>
    /// Tracks how much duration (in seconds) has already been added to hourly metrics.
    /// Used to calculate delta when session_end is called multiple times.
    /// </summary>
    [Id(21)] public int DurationAddedToMetrics { get; set; }
}
