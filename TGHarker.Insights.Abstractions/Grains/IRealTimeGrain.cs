using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IRealTimeGrain : IGrainWithStringKey
{
    Task RecordActiveVisitorAsync(string visitorId, string pagePath);
    Task RemoveActiveVisitorAsync(string visitorId);
    Task<RealTimeSnapshot> GetSnapshotAsync();
}
