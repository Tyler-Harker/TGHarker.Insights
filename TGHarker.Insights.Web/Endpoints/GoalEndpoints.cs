using System.Security.Claims;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;

namespace TGHarker.Insights.Web.Endpoints;

public static class GoalEndpoints
{
    public static void MapGoalEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/applications/{applicationId}/goals")
            .WithTags("Goals")
            .RequireAuthorization();

        group.MapGet("/", HandleListGoals);
        group.MapPost("/", HandleCreateGoal);
        group.MapGet("/{goalId}", HandleGetGoal);
        group.MapPut("/{goalId}", HandleUpdateGoal);
        group.MapDelete("/{goalId}", HandleDeleteGoal);
    }

    private static async Task<IResult> HandleListGoals(
        string applicationId,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var grains = await client.Search<IGoalGrain>()
            .Where(g => g.ApplicationId == applicationId)
            .ToListAsync();

        var results = new List<GoalInfo>();
        foreach (var grain in grains)
        {
            results.Add(await grain.GetInfoAsync());
        }

        return Results.Ok(results);
    }

    private static async Task<IResult> HandleCreateGoal(
        string applicationId,
        CreateGoalRequest request,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var goalId = Guid.NewGuid().ToString("N")[..8];
        var grain = client.GetGrain<IGoalGrain>($"goal-{applicationId}-{goalId}");

        await grain.CreateAsync(request with { ApplicationId = applicationId });
        var info = await grain.GetInfoAsync();

        return Results.Created($"/api/applications/{applicationId}/goals/{goalId}", info);
    }

    private static async Task<IResult> HandleGetGoal(
        string applicationId,
        string goalId,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var grain = client.GetGrain<IGoalGrain>($"goal-{applicationId}-{goalId}");
        var info = await grain.GetInfoAsync();

        if (string.IsNullOrEmpty(info.Id))
            return Results.NotFound();

        return Results.Ok(info);
    }

    private static async Task<IResult> HandleUpdateGoal(
        string applicationId,
        string goalId,
        UpdateGoalRequest request,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var grain = client.GetGrain<IGoalGrain>($"goal-{applicationId}-{goalId}");
        var info = await grain.GetInfoAsync();

        if (string.IsNullOrEmpty(info.Id))
            return Results.NotFound();

        await grain.UpdateAsync(request);
        return Results.Ok(await grain.GetInfoAsync());
    }

    private static async Task<IResult> HandleDeleteGoal(
        string applicationId,
        string goalId,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var grain = client.GetGrain<IGoalGrain>($"goal-{applicationId}-{goalId}");
        var info = await grain.GetInfoAsync();

        if (string.IsNullOrEmpty(info.Id))
            return Results.NotFound();

        await grain.DeleteAsync();
        return Results.NoContent();
    }

    private static async Task<bool> ValidateApplicationAccess(
        string applicationId,
        HttpContext context,
        IClusterClient client)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return false;

        var grain = client.GetGrain<IApplicationGrain>($"app-{applicationId}");
        var info = await grain.GetInfoAsync();

        return info.OwnerId == userId;
    }
}
