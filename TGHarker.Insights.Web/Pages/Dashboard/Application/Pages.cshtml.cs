using Microsoft.AspNetCore.Mvc;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;
using TGHarker.Insights.Web.Pages.Dashboard;

namespace TGHarker.Insights.Web.Pages.Dashboard.Application;

public class PagesModel : DashboardPageModel
{
    public PagesModel(IClusterClient client) : base(client)
    {
    }

    public List<PageStatDetail> PageStats { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "today";

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentPage = "pages";

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

        // Get page views - fetch all in parallel
        var pageViewGrains = await Client.Search<IPageViewGrain>()
            .Where(pv => pv.ApplicationId == ApplicationId && pv.Timestamp >= from && pv.Timestamp <= to)
            .ToListAsync();

        var pageViewInfoTasks = pageViewGrains.Select(g => g.GetInfoAsync());
        var pageViewInfos = (await Task.WhenAll(pageViewInfoTasks)).ToList();

        // Get sessions for bounce rate calculation - fetch all in parallel
        var sessionGrains = await Client.Search<ISessionGrain>()
            .Where(s => s.ApplicationId == ApplicationId && s.StartedAt >= from && s.StartedAt <= to)
            .ToListAsync();

        var sessionInfoTasks = sessionGrains.Select(g => g.GetInfoAsync());
        var sessionInfos = (await Task.WhenAll(sessionInfoTasks)).ToList();

        var totalSessions = sessionInfos.Count;
        var totalBounces = sessionInfos.Count(s => s.IsBounce);
        var overallBounceRate = totalSessions > 0 ? (double)totalBounces / totalSessions * 100 : 0;

        // Calculate page stats
        PageStats = pageViewInfos
            .GroupBy(pv => pv.PagePath)
            .Select(g =>
            {
                return new PageStatDetail
                {
                    Path = g.Key,
                    Views = g.Count(),
                    UniqueVisitors = g.Select(pv => pv.VisitorId).Distinct().Count(),
                    AvgTimeOnPage = g.Any() ? g.Average(pv => pv.TimeOnPageSeconds) : 0,
                    BounceRate = overallBounceRate // Using overall bounce rate as approximation
                };
            })
            .OrderByDescending(p => p.Views)
            .ToList();

        return Page();
    }

    public class PageStatDetail
    {
        public string Path { get; set; } = string.Empty;
        public int Views { get; set; }
        public int UniqueVisitors { get; set; }
        public double AvgTimeOnPage { get; set; }
        public double BounceRate { get; set; }
    }
}
