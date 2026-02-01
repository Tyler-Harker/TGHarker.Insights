using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IVisitorGrain : IGrainWithStringKey
{
    Task<VisitorInfo> GetInfoAsync();
    Task RecordVisitAsync(VisitData data);
    Task IdentifyAsync(string userId);
    Task SetAttributesAsync(Dictionary<string, string> attributes);
}
