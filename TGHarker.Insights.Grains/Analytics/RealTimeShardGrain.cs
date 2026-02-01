using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;

namespace TGHarker.Insights.Grains.Analytics;

/// <summary>
/// Sharded real-time grain. Traffic is distributed across shards using consistent hashing on visitorId.
/// This prevents any single grain from becoming a bottleneck.
/// </summary>
public class RealTimeShardGrain : Grain, IRealTimeShardGrain
{
    private readonly Dictionary<string, (string PagePath, DateTime LastSeen)> _activeVisitors = new();
    private IGrainTimer? _cleanupTimer;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _cleanupTimer = this.RegisterGrainTimer(
            static (state, _) => state.CleanupStaleVisitorsAsync(),
            this,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromMinutes(1),
                Period = TimeSpan.FromMinutes(1)
            });

        return base.OnActivateAsync(cancellationToken);
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        _cleanupTimer?.Dispose();
        return base.OnDeactivateAsync(reason, cancellationToken);
    }

    public Task RecordActiveVisitorAsync(string visitorId, string pagePath)
    {
        _activeVisitors[visitorId] = (pagePath, DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task RemoveActiveVisitorAsync(string visitorId)
    {
        _activeVisitors.Remove(visitorId);
        return Task.CompletedTask;
    }

    public Task<RealTimeShardSnapshot> GetSnapshotAsync()
    {
        var visitorsByPage = _activeVisitors.Values
            .GroupBy(v => v.PagePath)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(new RealTimeShardSnapshot(
            _activeVisitors.Count,
            visitorsByPage,
            _activeVisitors.Keys.ToHashSet()
        ));
    }

    private Task CleanupStaleVisitorsAsync()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var staleVisitors = _activeVisitors
            .Where(kv => kv.Value.LastSeen < cutoff)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var visitorId in staleVisitors)
        {
            _activeVisitors.Remove(visitorId);
        }

        return Task.CompletedTask;
    }
}
