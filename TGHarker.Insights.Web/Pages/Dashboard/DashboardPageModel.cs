using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;

namespace TGHarker.Insights.Web.Pages.Dashboard;

[Authorize]
public abstract class DashboardPageModel : PageModel
{
    protected readonly IClusterClient Client;

    protected DashboardPageModel(IClusterClient client)
    {
        Client = client;
    }

    [BindProperty(SupportsGet = true)]
    public string ApplicationId { get; set; } = string.Empty;

    public ApplicationInfo? Application { get; set; }
    public List<ApplicationInfo> AllApplications { get; set; } = [];
    public string CurrentPage { get; set; } = string.Empty;

    protected async Task<IActionResult?> LoadApplicationDataAsync()
    {
        var organizationId = GetOrganizationId();
        if (string.IsNullOrEmpty(organizationId))
            return Forbid();

        // Load current application
        var applicationGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        Application = await applicationGrain.GetInfoAsync();

        if (string.IsNullOrEmpty(Application.Id) || Application.OrganizationId != organizationId)
            return Forbid();

        // Load all applications for the switcher (scoped to organization)
        var grains = await Client.Search<IApplicationGrain>()
            .Where(p => p.OrganizationId == organizationId)
            .ToListAsync();

        foreach (var grain in grains)
        {
            AllApplications.Add(await grain.GetInfoAsync());
        }

        return null;
    }

    protected string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    protected string? GetOrganizationId()
    {
        var orgClaim = User.FindFirst("organization")?.Value;
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
