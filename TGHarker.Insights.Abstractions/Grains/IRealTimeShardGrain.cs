using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

/// <summary>
/// Sharded real-time grain to distribute load across multiple grains.
/// Key format: realtime-shard-{applicationId}-{shardId}
/// </summary>
public interface IRealTimeShardGrain : IGrainWithStringKey
{
    Task RecordActiveVisitorAsync(string visitorId, string pagePath);
    Task RemoveActiveVisitorAsync(string visitorId);
    Task<RealTimeShardSnapshot> GetSnapshotAsync();
}

/// <summary>
/// Coordinator grain that aggregates data from all shards.
/// Key format: realtime-{applicationId}
/// </summary>
public interface IRealTimeCoordinatorGrain : IGrainWithStringKey
{
    Task<RealTimeSnapshot> GetSnapshotAsync();
}
