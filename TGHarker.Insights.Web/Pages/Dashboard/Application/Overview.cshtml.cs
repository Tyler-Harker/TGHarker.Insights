using Microsoft.AspNetCore.Mvc;
using Orleans;
using TGHarker.Insights.Web.Pages.Dashboard;

namespace TGHarker.Insights.Web.Pages.Dashboard.Application;

public class OverviewModel : DashboardPageModel
{
    public OverviewModel(IClusterClient client) : base(client)
    {
    }

    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "24h";

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentPage = "overview";

        var result = await LoadApplicationDataAsync();
        if (result != null)
            return result;

        // Data is loaded via SSE - just render the page with loading states
        return Page();
    }
}
