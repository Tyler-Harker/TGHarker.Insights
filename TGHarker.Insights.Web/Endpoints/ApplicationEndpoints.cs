using System.Security.Claims;
using System.Text.Json;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;

namespace TGHarker.Insights.Web.Endpoints;

public static class ApplicationEndpoints
{
    public static void MapApplicationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/applications")
            .WithTags("Applications")
            .RequireAuthorization();

        group.MapGet("/", HandleListApplications);
        group.MapPost("/", HandleCreateApplication);
        group.MapGet("/{applicationId}", HandleGetApplication);
        group.MapPut("/{applicationId}", HandleUpdateApplication);
        group.MapDelete("/{applicationId}", HandleDeleteApplication);
        group.MapPost("/{applicationId}/regenerate-key", HandleRegenerateApiKey);
    }

    private static async Task<IResult> HandleListApplications(
        HttpContext context,
        IClusterClient client)
    {
        var organizationId = GetOrganizationId(context);
        if (string.IsNullOrEmpty(organizationId))
            return Results.Forbid();

        var grains = await client.Search<IApplicationGrain>()
            .Where(a => a.OrganizationId == organizationId)
            .ToListAsync();

        var results = new List<ApplicationInfo>();
        foreach (var grain in grains)
        {
            results.Add(await grain.GetInfoAsync());
        }

        return Results.Ok(results);
    }

    private static async Task<IResult> HandleCreateApplication(
        HttpContext context,
        CreateApplicationRequest request,
        IClusterClient client)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Results.Unauthorized();

        var organizationId = GetOrganizationId(context);
        if (string.IsNullOrEmpty(organizationId))
            return Results.Forbid();

        var applicationId = Guid.NewGuid().ToString("N")[..12];
        var grain = client.GetGrain<IApplicationGrain>($"app-{applicationId}");

        await grain.CreateAsync(request with { OwnerId = userId, OrganizationId = organizationId });
        var info = await grain.GetInfoAsync();

        return Results.Created($"/api/applications/{applicationId}", info);
    }

    private static async Task<IResult> HandleGetApplication(
        string applicationId,
        HttpContext context,
        IClusterClient client)
    {
        var organizationId = GetOrganizationId(context);
        if (string.IsNullOrEmpty(organizationId))
            return Results.Forbid();

        var grain = client.GetGrain<IApplicationGrain>($"app-{applicationId}");
        var info = await grain.GetInfoAsync();

        if (string.IsNullOrEmpty(info.Id))
            return Results.NotFound();

        if (info.OrganizationId != organizationId)
            return Results.Forbid();

        return Results.Ok(info);
    }

    private static async Task<IResult> HandleUpdateApplication(
        string applicationId,
        UpdateApplicationRequest request,
        HttpContext context,
        IClusterClient client)
    {
        var organizationId = GetOrganizationId(context);
        if (string.IsNullOrEmpty(organizationId))
            return Results.Forbid();

        var grain = client.GetGrain<IApplicationGrain>($"app-{applicationId}");
        var info = await grain.GetInfoAsync();

        if (string.IsNullOrEmpty(info.Id))
            return Results.NotFound();

        if (info.OrganizationId != organizationId)
            return Results.Forbid();

        await grain.UpdateAsync(request);
        return Results.Ok(await grain.GetInfoAsync());
    }

    private static async Task<IResult> HandleDeleteApplication(
        string applicationId,
        HttpContext context,
        IClusterClient client)
    {
        var organizationId = GetOrganizationId(context);
        if (string.IsNullOrEmpty(organizationId))
            return Results.Forbid();

        var grain = client.GetGrain<IApplicationGrain>($"app-{applicationId}");
        var info = await grain.GetInfoAsync();

        if (string.IsNullOrEmpty(info.Id))
            return Results.NotFound();

        if (info.OrganizationId != organizationId)
            return Results.Forbid();

        await grain.DeleteAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> HandleRegenerateApiKey(
        string applicationId,
        HttpContext context,
        IClusterClient client)
    {
        var organizationId = GetOrganizationId(context);
        if (string.IsNullOrEmpty(organizationId))
            return Results.Forbid();

        var grain = client.GetGrain<IApplicationGrain>($"app-{applicationId}");
        var info = await grain.GetInfoAsync();

        if (string.IsNullOrEmpty(info.Id))
            return Results.NotFound();

        if (info.OrganizationId != organizationId)
            return Results.Forbid();

        await grain.RegenerateApiKeyAsync();
        return Results.Ok(await grain.GetInfoAsync());
    }

    private static string? GetOrganizationId(HttpContext context)
    {
        var orgClaim = context.User.FindFirst("organization")?.Value;
        if (string.IsNullOrEmpty(orgClaim))
            return null;

        try
        {
            var doc = JsonDocument.Parse(orgClaim);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
                return idProp.GetString();
        }
        catch { }

        return null;
    }
}
