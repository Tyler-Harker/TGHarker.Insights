using Microsoft.AspNetCore.Mvc;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;
using TGHarker.Insights.Web.Pages.Dashboard;

namespace TGHarker.Insights.Web.Pages.Dashboard.Application;

public class OverviewModel : DashboardPageModel
{
    public OverviewModel(IClusterClient client) : base(client)
    {
    }

    public OverviewMetrics Metrics { get; set; } = new();
    public List<PageStat> TopPages { get; set; } = [];
    public List<string> ChartLabels { get; set; } = [];
    public List<int> ChartData { get; set; } = [];
    public List<string> SourceLabels { get; set; } = [];
    public List<int> SourceData { get; set; } = [];
    public List<UserAttributeDefinition> FilterableAttributes { get; set; } = [];
    public Dictionary<string, List<string>> AttributeValues { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? FilterAttribute { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterValue { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "30d";

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentPage = "overview";

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

        // Load filterable attributes
        var applicationGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        var allAttributes = await applicationGrain.GetUserAttributesAsync();
        FilterableAttributes = allAttributes.Where(a => a.IsFilterable).ToList();

        // Get all visitors to build unique values per attribute and for filtering
        var visitorGrains = await Client.Search<IVisitorGrain>()
            .Where(v => v.ApplicationId == $"visitor-{ApplicationId}")
            .ToListAsync();

        var visitorInfos = new List<VisitorInfo>();
        foreach (var grain in visitorGrains)
        {
            visitorInfos.Add(await grain.GetInfoAsync());
        }

        // Build unique values for each filterable attribute
        foreach (var attr in FilterableAttributes)
        {
            var values = visitorInfos
                .Where(v => v.Attributes.ContainsKey(attr.Key))
                .Select(v => v.Attributes[attr.Key])
                .Distinct()
                .OrderBy(v => v)
                .ToList();
            AttributeValues[attr.Key] = values;
        }

        // Apply attribute filter if set
        HashSet<string>? filteredVisitorIds = null;
        if (!string.IsNullOrEmpty(FilterAttribute) && !string.IsNullOrEmpty(FilterValue))
        {
            filteredVisitorIds = visitorInfos
                .Where(v => v.Attributes.TryGetValue(FilterAttribute, out var val) && val == FilterValue)
                .Select(v => v.Id.Replace($"visitor-{ApplicationId}-", ""))
                .ToHashSet();
        }

        // Get metrics
        var metrics = new List<HourlyMetrics>();
        var current = from;
        while (current <= to)
        {
            var grainKey = $"metrics-hourly-{ApplicationId}-{current:yyyyMMddHH}";
            var grain = Client.GetGrain<IHourlyMetricsGrain>(grainKey);
            metrics.Add(await grain.GetMetricsAsync());
            current = current.AddHours(1);
        }

        Metrics = new OverviewMetrics
        {
            PageViews = metrics.Sum(m => m.PageViews),
            Sessions = metrics.Sum(m => m.Sessions),
            UniqueVisitors = metrics.Sum(m => m.UniqueVisitors),
            BounceRate = metrics.Sum(m => m.Sessions) > 0
                ? (double)metrics.Sum(m => m.Bounces) / metrics.Sum(m => m.Sessions) * 100
                : 0,
            AvgSessionDuration = metrics.Sum(m => m.Sessions) > 0
                ? metrics.Sum(m => m.TotalDurationSeconds) / metrics.Sum(m => m.Sessions)
                : 0
        };

        // Chart data - daily aggregation
        var dailyMetrics = metrics
            .GroupBy(m => m.HourStart.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key, PageViews = g.Sum(m => m.PageViews) })
            .ToList();

        ChartLabels = dailyMetrics.Select(d => d.Date.ToString("MMM d")).ToList();
        ChartData = dailyMetrics.Select(d => d.PageViews).ToList();

        // Top pages
        var pageViewGrains = await Client.Search<IPageViewGrain>()
            .Where(pv => pv.ApplicationId == ApplicationId && pv.Timestamp >= from && pv.Timestamp <= to)
            .ToListAsync();

        var pageViewInfos = new List<PageViewInfo>();
        foreach (var grain in pageViewGrains)
        {
            pageViewInfos.Add(await grain.GetInfoAsync());
        }

        // Apply visitor filter to page views if set
        if (filteredVisitorIds != null)
        {
            pageViewInfos = pageViewInfos.Where(pv => filteredVisitorIds.Contains(pv.VisitorId)).ToList();
        }

        TopPages = pageViewInfos
            .GroupBy(pv => pv.PagePath)
            .Select(g => new PageStat { Path = g.Key, Views = g.Count() })
            .OrderByDescending(p => p.Views)
            .Take(10)
            .ToList();

        // Traffic sources
        var sessionGrains = await Client.Search<ISessionGrain>()
            .Where(s => s.ApplicationId == ApplicationId && s.StartedAt >= from && s.StartedAt <= to)
            .ToListAsync();

        var sessionInfos = new List<SessionInfo>();
        foreach (var grain in sessionGrains)
        {
            sessionInfos.Add(await grain.GetInfoAsync());
        }

        // Apply visitor filter to sessions if set
        if (filteredVisitorIds != null)
        {
            sessionInfos = sessionInfos.Where(s => filteredVisitorIds.Contains(s.VisitorId)).ToList();
        }

        var sources = sessionInfos
            .GroupBy(s => s.Source.ToString())
            .OrderByDescending(g => g.Count())
            .ToList();

        SourceLabels = sources.Select(s => s.Key).ToList();
        SourceData = sources.Select(s => s.Count()).ToList();

        return Page();
    }

    public class OverviewMetrics
    {
        public int PageViews { get; set; }
        public int Sessions { get; set; }
        public int UniqueVisitors { get; set; }
        public double BounceRate { get; set; }
        public int AvgSessionDuration { get; set; }
    }

    public class PageStat
    {
        public string Path { get; set; } = string.Empty;
        public int Views { get; set; }
    }
}
