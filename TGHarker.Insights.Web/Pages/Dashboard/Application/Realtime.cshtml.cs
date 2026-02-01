using Microsoft.AspNetCore.Mvc;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Web.Pages.Dashboard;

namespace TGHarker.Insights.Web.Pages.Dashboard.Application;

public class RealtimeModel : DashboardPageModel
{
    public RealtimeModel(IClusterClient client) : base(client)
    {
    }

    public RealTimeSnapshot Snapshot { get; set; } = new(0, new Dictionary<string, int>(), DateTime.UtcNow);

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentPage = "realtime";

        var result = await LoadApplicationDataAsync();
        if (result != null)
            return result;

        var realTimeCoordinator = Client.GetGrain<IRealTimeCoordinatorGrain>($"realtime-{ApplicationId}");
        Snapshot = await realTimeCoordinator.GetSnapshotAsync();

        return Page();
    }
}
