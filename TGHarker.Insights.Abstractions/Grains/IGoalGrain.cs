using Orleans;
using TGHarker.Insights.Abstractions.DTOs;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IGoalGrain : IGrainWithStringKey
{
    Task<GoalInfo> GetInfoAsync();
    Task CreateAsync(CreateGoalRequest request);
    Task UpdateAsync(UpdateGoalRequest request);
    Task<bool> EvaluateAsync(GoalEvaluationContext context);
    Task RecordConversionAsync();
    Task DeleteAsync();
}
