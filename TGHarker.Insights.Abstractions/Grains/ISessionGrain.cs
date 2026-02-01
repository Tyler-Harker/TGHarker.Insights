using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface ISessionGrain : IGrainWithStringKey
{
    Task StartAsync(SessionStartData data);
    Task RecordPageViewAsync(PageViewData data);
    Task RecordEventAsync(EventData data);
    Task EndAsync(string? exitPage);
    Task<SessionInfo> GetInfoAsync();
    Task RecordConversionAsync(string goalId);
}
