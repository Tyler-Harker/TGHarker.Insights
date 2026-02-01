using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IEventGrain : IGrainWithStringKey
{
    Task RecordAsync(EventRecordData data);
    Task<EventInfo> GetInfoAsync();
}
