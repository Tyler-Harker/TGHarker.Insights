using Orleans;

namespace TGHarker.Insights.Abstractions.Models;

[GenerateSerializer]
public sealed class DeviceInfo
{
    [Id(0)] public string? Browser { get; set; }
    [Id(1)] public string? BrowserVersion { get; set; }
    [Id(2)] public string? OS { get; set; }
    [Id(3)] public string? OSVersion { get; set; }
    [Id(4)] public string? DeviceType { get; set; }
    [Id(5)] public bool IsMobile { get; set; }
}
