using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;

namespace TGHarker.Insights.Web.Pages.Dashboard;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IClusterClient _client;

    public IndexModel(IClusterClient client)
    {
        _client = client;
    }

    public List<ApplicationInfo> Applications { get; set; } = [];

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return;

        var grains = await _client.Search<IApplicationGrain>()
            .Where(p => p.OwnerId == userId)
            .ToListAsync();

        foreach (var grain in grains)
        {
            Applications.Add(await grain.GetInfoAsync());
        }
    }
}
