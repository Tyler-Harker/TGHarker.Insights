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
    public string Range { get; set; } = "24h";

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
            "24h" => DateTime.UtcNow.AddHours(-24),
            _ => DateTime.UtcNow.Date.AddDays(-30) // default 30d
        };

        // Get sessions - fetch all in parallel
        var sessionGrains = await Client.Search<ISessionGrain>()
            .Where(s => s.ApplicationId == ApplicationId && s.StartedAt >= from && s.StartedAt <= to)
            .ToListAsync();

        var sessionInfoTasks = sessionGrains.Select(g => g.GetInfoAsync());
        var sessionInfos = (await Task.WhenAll(sessionInfoTasks)).ToList();

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

        // Referrer stats - group by referrer domain
        ReferrerStats = sessionInfos
            .Where(s => !string.IsNullOrEmpty(s.ReferrerDomain))
            .GroupBy(s => s.ReferrerDomain!)
            .Select(g => new ReferrerStat
            {
                Referrer = g.Key,
                Sessions = g.Count(),
                BounceRate = g.Count() > 0 ? (double)g.Count(s => s.IsBounce) / g.Count() * 100 : 0
            })
            .OrderByDescending(r => r.Sessions)
            .Take(10)
            .ToList();

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
