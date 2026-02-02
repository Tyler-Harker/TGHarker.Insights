using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

public class RetentionCohortGrain : Grain, IRetentionCohortGrain
{
    private readonly IPersistentState<RetentionCohortState> _state;
    private readonly IGrainFactory _grainFactory;
    private bool _organizationIdResolved;

    public RetentionCohortGrain(
        [PersistentState("retentioncohort", "Default")] IPersistentState<RetentionCohortState> state,
        IGrainFactory grainFactory)
    {
        _state = state;
        _grainFactory = grainFactory;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_state.State.Id))
        {
            var key = this.GetPrimaryKeyString();
            var parts = key.Split('-');

            _state.State.Id = key;
            _state.State.ApplicationId = string.Join("-", parts.Skip(1).Take(parts.Length - 2));
            _state.State.CohortWeek = parts.Last();
        }

        _organizationIdResolved = !string.IsNullOrEmpty(_state.State.OrganizationId);

        return base.OnActivateAsync(cancellationToken);
    }

    private async Task EnsureOrganizationIdAsync()
    {
        if (_organizationIdResolved)
            return;

        var applicationGrain = _grainFactory.GetGrain<IApplicationGrain>($"app-{_state.State.ApplicationId}");
        var appInfo = await applicationGrain.GetInfoAsync();
        _state.State.OrganizationId = appInfo.OrganizationId;
        _organizationIdResolved = true;
    }

    public async Task AddVisitorAsync(string visitorId)
    {
        if (_state.State.VisitorIds.Add(visitorId))
        {
            await EnsureOrganizationIdAsync();
            _state.State.TotalVisitors++;
            await _state.WriteStateAsync();
        }
    }

    public async Task RecordReturnVisitAsync(string visitorId, int weeksSinceCohort)
    {
        if (!_state.State.VisitorIds.Contains(visitorId))
            return;

        if (!_state.State.ReturnsByWeek.TryGetValue(weeksSinceCohort, out var visitors))
        {
            visitors = new HashSet<string>();
            _state.State.ReturnsByWeek[weeksSinceCohort] = visitors;
        }

        if (visitors.Add(visitorId))
        {
            await EnsureOrganizationIdAsync();
            await _state.WriteStateAsync();
        }
    }

    public Task<RetentionCohortData> GetDataAsync()
    {
        var retentionByWeek = _state.State.ReturnsByWeek
            .ToDictionary(kv => kv.Key, kv => kv.Value.Count);

        return Task.FromResult(new RetentionCohortData(
            _state.State.ApplicationId,
            _state.State.CohortWeek,
            _state.State.TotalVisitors,
            retentionByWeek
        ));
    }
}
