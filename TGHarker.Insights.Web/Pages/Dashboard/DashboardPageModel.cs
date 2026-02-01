using System.Security.Claims;
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Load current application
        var applicationGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        Application = await applicationGrain.GetInfoAsync();

        if (string.IsNullOrEmpty(Application.Id) || Application.OwnerId != userId)
            return Forbid();

        // Load all applications for the switcher
        var grains = await Client.Search<IApplicationGrain>()
            .Where(p => p.OwnerId == userId)
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
}
