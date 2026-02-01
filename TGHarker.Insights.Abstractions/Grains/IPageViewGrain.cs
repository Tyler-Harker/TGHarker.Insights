using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IPageViewGrain : IGrainWithStringKey
{
    Task RecordAsync(PageViewRecordData data);
    Task<PageViewInfo> GetInfoAsync();
}
