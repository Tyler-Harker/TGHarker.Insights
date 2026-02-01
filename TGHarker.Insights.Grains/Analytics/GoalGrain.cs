using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Enums;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;
using System.Text.RegularExpressions;

namespace TGHarker.Insights.Grains.Analytics;

public class GoalGrain : Grain, IGoalGrain
{
    private readonly IPersistentState<GoalState> _state;

    public GoalGrain(
        [PersistentState("goal", "Default")] IPersistentState<GoalState> state)
    {
        _state = state;
    }

    public Task<GoalInfo> GetInfoAsync()
    {
        var state = _state.State;
        return Task.FromResult(new GoalInfo(
            state.Id,
            state.ApplicationId,
            state.Name,
            state.Type,
            state.Condition,
            state.MonetaryValue,
            state.IsActive,
            state.TotalConversions,
            state.CreatedAt
        ));
    }

    public async Task CreateAsync(CreateGoalRequest request)
    {
        _state.State = new GoalState
        {
            Id = this.GetPrimaryKeyString(),
            ApplicationId = request.ApplicationId,
            Name = request.Name,
            Type = request.Type,
            Condition = request.Condition,
            MonetaryValue = request.MonetaryValue,
            IsActive = true,
            TotalConversions = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _state.WriteStateAsync();
    }

    public async Task UpdateAsync(UpdateGoalRequest request)
    {
        if (request.Name is not null)
            _state.State.Name = request.Name;

        if (request.Condition is not null)
            _state.State.Condition = request.Condition;

        if (request.MonetaryValue.HasValue)
            _state.State.MonetaryValue = request.MonetaryValue;

        if (request.IsActive.HasValue)
            _state.State.IsActive = request.IsActive.Value;

        await _state.WriteStateAsync();
    }

    public Task<bool> EvaluateAsync(GoalEvaluationContext context)
    {
        if (!_state.State.IsActive)
            return Task.FromResult(false);

        var condition = _state.State.Condition;

        return Task.FromResult(_state.State.Type switch
        {
            GoalType.PageView => EvaluatePageViewGoal(context.PagePath, condition.UrlPattern),
            GoalType.Event => EvaluateEventGoal(context.EventCategory, context.EventAction, condition),
            GoalType.Duration => context.SessionDurationSeconds >= (condition.MinDurationSeconds ?? 0),
            GoalType.PagesPerSession => context.PagesViewed >= (condition.MinPagesViewed ?? 0),
            _ => false
        });
    }

    public async Task RecordConversionAsync()
    {
        _state.State.TotalConversions++;
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }

    private static bool EvaluatePageViewGoal(string? pagePath, string? urlPattern)
    {
        if (string.IsNullOrEmpty(pagePath) || string.IsNullOrEmpty(urlPattern))
            return false;

        try
        {
            return Regex.IsMatch(pagePath, urlPattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            return pagePath.Equals(urlPattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static bool EvaluateEventGoal(string? eventCategory, string? eventAction, GoalCondition condition)
    {
        if (string.IsNullOrEmpty(eventCategory))
            return false;

        var categoryMatch = string.IsNullOrEmpty(condition.EventCategory) ||
                           eventCategory.Equals(condition.EventCategory, StringComparison.OrdinalIgnoreCase);

        var actionMatch = string.IsNullOrEmpty(condition.EventAction) ||
                         (eventAction?.Equals(condition.EventAction, StringComparison.OrdinalIgnoreCase) ?? false);

        return categoryMatch && actionMatch;
    }
}
