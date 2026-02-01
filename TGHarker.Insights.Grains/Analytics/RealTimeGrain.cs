using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;

namespace TGHarker.Insights.Grains.Analytics;

public class RealTimeGrain : Grain, IRealTimeGrain
{
    private readonly Dictionary<string, (string PagePath, DateTime LastSeen)> _activeVisitors = new();
    private IGrainTimer? _cleanupTimer;

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Clean up stale visitors every minute
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

    public Task<RealTimeSnapshot> GetSnapshotAsync()
    {
        var visitorsByPage = _activeVisitors.Values
            .GroupBy(v => v.PagePath)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(new RealTimeSnapshot(
            _activeVisitors.Count,
            visitorsByPage,
            DateTime.UtcNow
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
