using Orleans;
using TGHarker.Insights.Abstractions.Enums;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record GoalInfo(
    [property: Id(0)] string Id,
    [property: Id(1)] string ApplicationId,
    [property: Id(2)] string Name,
    [property: Id(3)] GoalType Type,
    [property: Id(4)] GoalCondition Condition,
    [property: Id(5)] decimal? MonetaryValue,
    [property: Id(6)] bool IsActive,
    [property: Id(7)] int TotalConversions,
    [property: Id(8)] DateTime CreatedAt
);

[GenerateSerializer]
public record CreateGoalRequest(
    [property: Id(0)] string ApplicationId,
    [property: Id(1)] string Name,
    [property: Id(2)] GoalType Type,
    [property: Id(3)] GoalCondition Condition,
    [property: Id(4)] decimal? MonetaryValue
);

[GenerateSerializer]
public record UpdateGoalRequest(
    [property: Id(0)] string? Name,
    [property: Id(1)] GoalCondition? Condition,
    [property: Id(2)] decimal? MonetaryValue,
    [property: Id(3)] bool? IsActive
);

[GenerateSerializer]
public record GoalEvaluationContext(
    [property: Id(0)] string? PagePath,
    [property: Id(1)] string? EventCategory,
    [property: Id(2)] string? EventAction,
    [property: Id(3)] int SessionDurationSeconds,
    [property: Id(4)] int PagesViewed
);
