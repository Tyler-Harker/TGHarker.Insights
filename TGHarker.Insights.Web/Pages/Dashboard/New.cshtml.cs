using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;

namespace TGHarker.Insights.Web.Pages.Dashboard;

[Authorize]
public class NewModel : PageModel
{
    private readonly IClusterClient _client;

    public NewModel(IClusterClient client)
    {
        _client = client;
    }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Domain { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var organizationId = GetOrganizationId();
        if (string.IsNullOrEmpty(organizationId))
            return Forbid();

        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Domain))
        {
            ModelState.AddModelError(string.Empty, "Name and Domain are required.");
            return Page();
        }

        var applicationId = Guid.NewGuid().ToString("N")[..12];
        var grain = _client.GetGrain<IApplicationGrain>($"app-{applicationId}");

        await grain.CreateAsync(new CreateApplicationRequest(Name, userId, Domain, organizationId));

        return RedirectToPage("/Dashboard/Application/Overview", new { applicationId });
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
