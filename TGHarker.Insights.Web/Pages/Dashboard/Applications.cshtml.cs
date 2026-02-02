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
public class ApplicationsModel : PageModel
{
    private readonly IClusterClient _client;
    private const int PageSize = 10;

    public ApplicationsModel(IClusterClient client)
    {
        _client = client;
    }

    public List<ApplicationInfo> Applications { get; set; } = [];
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public async Task OnGetAsync()
    {
        var organizationId = GetOrganizationId();
        if (string.IsNullOrEmpty(organizationId))
            return;

        var grains = await _client.Search<IApplicationGrain>()
            .Where(p => p.OrganizationId == organizationId)
            .ToListAsync();

        // Fetch all application info in parallel
        var infoTasks = grains.Select(g => g.GetInfoAsync());
        var allApps = (await Task.WhenAll(infoTasks)).ToList();

        // Apply search filter
        if (!string.IsNullOrEmpty(Search))
        {
            allApps = allApps
                .Where(a => a.Name.Contains(Search, StringComparison.OrdinalIgnoreCase) ||
                            a.Domain.Contains(Search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        TotalCount = allApps.Count;

        // Apply pagination
        Applications = allApps
            .OrderBy(a => a.Name)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
    }

    private string? GetOrganizationId()
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
