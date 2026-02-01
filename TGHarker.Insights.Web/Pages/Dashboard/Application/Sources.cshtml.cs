using Microsoft.AspNetCore.Mvc;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;
using TGHarker.Insights.Web.Pages.Dashboard;

namespace TGHarker.Insights.Web.Pages.Dashboard.Application;

public class SourcesModel : DashboardPageModel
{
    public SourcesModel(IClusterClient client) : base(client)
    {
    }

    public List<SourceStat> SourceStats { get; set; } = [];
    public List<ReferrerStat> ReferrerStats { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "30d";

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentPage = "sources";

        var result = await LoadApplicationDataAsync();
        if (result != null)
            return result;

        var to = DateTime.UtcNow;
        var from = Range switch
        {
            "7d" => DateTime.UtcNow.Date.AddDays(-7),
            "today" => DateTime.UtcNow.Date,
            _ => DateTime.UtcNow.Date.AddDays(-30) // default 30d
        };

        // Get sessions
        var sessionGrains = await Client.Search<ISessionGrain>()
            .Where(s => s.ApplicationId == ApplicationId && s.StartedAt >= from && s.StartedAt <= to)
            .ToListAsync();

        var sessionInfos = new List<SessionInfo>();
        foreach (var grain in sessionGrains)
        {
            sessionInfos.Add(await grain.GetInfoAsync());
        }

        var totalSessions = sessionInfos.Count;

        // Source stats
        SourceStats = sessionInfos
            .GroupBy(s => s.Source.ToString())
            .Select(g => new SourceStat
            {
                Source = g.Key,
                Sessions = g.Count(),
                Percentage = totalSessions > 0 ? (double)g.Count() / totalSessions * 100 : 0
            })
            .OrderByDescending(s => s.Sessions)
            .ToList();

        // Note: Referrer data would need to be tracked separately
        // For now, showing source breakdown only
        ReferrerStats = [];

        return Page();
    }

    public class SourceStat
    {
        public string Source { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public double Percentage { get; set; }
    }

    public class ReferrerStat
    {
        public string Referrer { get; set; } = string.Empty;
        public int Sessions { get; set; }
        public double BounceRate { get; set; }
    }
}
