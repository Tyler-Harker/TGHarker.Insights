using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IRetentionCohortGrain : IGrainWithStringKey
{
    Task AddVisitorAsync(string visitorId);
    Task RecordReturnVisitAsync(string visitorId, int weeksSinceCohort);
    Task<RetentionCohortData> GetDataAsync();
}
