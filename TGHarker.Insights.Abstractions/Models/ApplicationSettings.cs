using Orleans;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
public sealed class ApplicationSettings
{
    [Id(0)] public int SessionTimeoutMinutes { get; set; } = 30;
    [Id(1)] public bool TrackPageViews { get; set; } = true;
    [Id(2)] public bool TrackEvents { get; set; } = true;
    [Id(3)] public bool TrackScrollDepth { get; set; } = false;
    [Id(4)] public double SamplingRate { get; set; } = 1.0;
    [Id(5)] public List<string> ExcludedPaths { get; set; } = [];
    [Id(6)] public List<string> ExcludedIpRanges { get; set; } = [];
}
