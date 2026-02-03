using Microsoft.AspNetCore.Mvc;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;
using TGHarker.Insights.Web.Pages.Dashboard;

namespace TGHarker.Insights.Web.Pages.Dashboard.Application;

public class EventsModel : DashboardPageModel
{
    public EventsModel(IClusterClient client) : base(client)
    {
    }

    public int TotalEvents { get; set; }
    public int UniqueEventNames { get; set; }
    public double EventsPerSession { get; set; }
    public List<EventStat> EventStats { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "today";

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentPage = "events";

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

        // Get events - fetch all in parallel
        var eventGrains = await Client.Search<IEventGrain>()
            .Where(e => e.ApplicationId == ApplicationId && e.Timestamp >= from && e.Timestamp <= to)
            .ToListAsync();

        var eventInfoTasks = eventGrains.Select(g => g.GetInfoAsync());
        var eventInfos = (await Task.WhenAll(eventInfoTasks)).ToList();

        TotalEvents = eventInfos.Count;
        UniqueEventNames = eventInfos.Select(e => $"{e.Category}:{e.Action}").Distinct().Count();

        // Get session count for events per session calculation
        var sessionGrains = await Client.Search<ISessionGrain>()
            .Where(s => s.ApplicationId == ApplicationId && s.StartedAt >= from && s.StartedAt <= to)
            .ToListAsync();

        var sessionCount = sessionGrains.Count;
        EventsPerSession = sessionCount > 0 ? (double)TotalEvents / sessionCount : 0;

        // Event stats grouped by category:action
        EventStats = eventInfos
            .GroupBy(e => $"{e.Category}:{e.Action}")
            .Select(g => new EventStat
            {
                Name = g.Key,
                Count = g.Count(),
                UniqueUsers = g.Select(e => e.VisitorId).Distinct().Count(),
                Percentage = TotalEvents > 0 ? (double)g.Count() / TotalEvents * 100 : 0
            })
            .OrderByDescending(e => e.Count)
            .ToList();

        return Page();
    }

    public class EventStat
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public int UniqueUsers { get; set; }
        public double Percentage { get; set; }
    }
}
