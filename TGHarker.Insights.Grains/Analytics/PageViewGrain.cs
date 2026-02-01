using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

public class PageViewGrain : Grain, IPageViewGrain
{
    private readonly IPersistentState<PageViewState> _state;

    public PageViewGrain(
        [PersistentState("pageview", "Default")] IPersistentState<PageViewState> state)
    {
        _state = state;
    }

    public async Task RecordAsync(PageViewRecordData data)
    {
        _state.State = new PageViewState
        {
            Id = this.GetPrimaryKeyString(),
            ApplicationId = data.ApplicationId,
            SessionId = data.SessionId,
            VisitorId = data.VisitorId,
            PagePath = data.PagePath,
            PageTitle = data.PageTitle,
            Timestamp = data.Timestamp
        };

        await _state.WriteStateAsync();
    }

    public Task<PageViewInfo> GetInfoAsync()
    {
        var state = _state.State;
        return Task.FromResult(new PageViewInfo(
            state.Id,
            state.ApplicationId,
            state.SessionId,
            state.VisitorId,
            state.PagePath,
            state.PageTitle,
            state.Timestamp,
            state.TimeOnPageSeconds,
            state.ScrollDepthPercent
        ));
    }
}
