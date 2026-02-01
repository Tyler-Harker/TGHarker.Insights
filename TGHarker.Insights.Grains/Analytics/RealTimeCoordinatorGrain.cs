using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;

namespace TGHarker.Insights.Grains.Analytics;

/// <summary>
/// Coordinator grain that aggregates snapshots from all shards.
/// Called only for dashboard reads, not for every incoming event.
/// </summary>
public class RealTimeCoordinatorGrain : Grain, IRealTimeCoordinatorGrain
{
    public const int ShardCount = 16; // Number of shards per application

    public async Task<RealTimeSnapshot> GetSnapshotAsync()
    {
        var key = this.GetPrimaryKeyString();
        var applicationId = key.Replace("realtime-", "");

        // Gather snapshots from all shards in parallel
        var shardTasks = new List<Task<RealTimeShardSnapshot>>();
        for (int i = 0; i < ShardCount; i++)
        {
            var shardGrain = GrainFactory.GetGrain<IRealTimeShardGrain>($"realtime-shard-{applicationId}-{i}");
            shardTasks.Add(shardGrain.GetSnapshotAsync());
        }

        var shardSnapshots = await Task.WhenAll(shardTasks);

        // Aggregate results
        var totalVisitors = 0;
        var visitorsByPage = new Dictionary<string, int>();

        foreach (var snapshot in shardSnapshots)
        {
            totalVisitors += snapshot.ActiveVisitors;

            foreach (var (page, count) in snapshot.VisitorsByPage)
            {
                if (visitorsByPage.TryGetValue(page, out var existing))
                {
                    visitorsByPage[page] = existing + count;
                }
                else
                {
                    visitorsByPage[page] = count;
                }
            }
        }

        return new RealTimeSnapshot(totalVisitors, visitorsByPage, DateTime.UtcNow);
    }

    /// <summary>
    /// Get the shard ID for a visitor using consistent hashing.
    /// </summary>
    public static int GetShardId(string visitorId)
    {
        // Use a simple hash to distribute visitors across shards
        var hash = visitorId.GetHashCode();
        return Math.Abs(hash % ShardCount);
    }
}
