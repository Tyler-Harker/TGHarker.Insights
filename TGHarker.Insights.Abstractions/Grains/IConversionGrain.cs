using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IConversionGrain : IGrainWithStringKey
{
    Task RecordAsync(ConversionRecordData data);
    Task<ConversionInfo> GetInfoAsync();
}
