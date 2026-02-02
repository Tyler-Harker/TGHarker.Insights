using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;
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
    public string? OrganizationName { get; set; }
    public OverviewMetrics Metrics { get; set; } = new();
    public List<AppMetricSummary> TopApplications { get; set; } = [];
    public List<string> ChartLabels { get; set; } = [];
    public List<int> ChartData { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "30d";

    public async Task OnGetAsync()
    {
        var organizationId = GetOrganizationId();
        if (string.IsNullOrEmpty(organizationId))
            return;

        OrganizationName = GetOrganizationName();

        // Load all applications for the organization
        var grains = await _client.Search<IApplicationGrain>()
            .Where(p => p.OrganizationId == organizationId)
            .ToListAsync();

        foreach (var grain in grains)
        {
            Applications.Add(await grain.GetInfoAsync());
        }

        if (!Applications.Any())
            return;

        // Calculate date range
        var to = DateTime.UtcNow;
        var from = Range switch
        {
            "7d" => DateTime.UtcNow.Date.AddDays(-7),
            "today" => DateTime.UtcNow.Date,
            _ => DateTime.UtcNow.Date.AddDays(-30)
        };

        // Aggregate metrics across all applications
        var allMetrics = new List<HourlyMetrics>();
        var appMetrics = new Dictionary<string, (ApplicationInfo App, int PageViews, int Sessions)>();

        foreach (var app in Applications)
        {
            var appId = app.Id.StartsWith("app-") ? app.Id[4..] : app.Id;
            var current = from;
            var appPageViews = 0;
            var appSessions = 0;

            while (current <= to)
            {
                var grainKey = $"metrics-hourly-{appId}-{current:yyyyMMddHH}";
                var grain = _client.GetGrain<IHourlyMetricsGrain>(grainKey);
                var metrics = await grain.GetMetricsAsync();
                allMetrics.Add(metrics);
                appPageViews += metrics.PageViews;
                appSessions += metrics.Sessions;
                current = current.AddHours(1);
            }

            appMetrics[app.Id] = (app, appPageViews, appSessions);
        }

        // Calculate aggregate metrics
        Metrics = new OverviewMetrics
        {
            PageViews = allMetrics.Sum(m => m.PageViews),
            Sessions = allMetrics.Sum(m => m.Sessions),
            UniqueVisitors = allMetrics.Sum(m => m.UniqueVisitors),
            BounceRate = allMetrics.Sum(m => m.Sessions) > 0
                ? (double)allMetrics.Sum(m => m.Bounces) / allMetrics.Sum(m => m.Sessions) * 100
                : 0,
            AvgSessionDuration = allMetrics.Sum(m => m.Sessions) > 0
                ? allMetrics.Sum(m => m.TotalDurationSeconds) / allMetrics.Sum(m => m.Sessions)
                : 0,
            TotalApplications = Applications.Count
        };

        // Top applications by page views
        TopApplications = appMetrics.Values
            .OrderByDescending(a => a.PageViews)
            .Take(5)
            .Select(a => new AppMetricSummary
            {
                AppId = a.App.Id.StartsWith("app-") ? a.App.Id[4..] : a.App.Id,
                Name = a.App.Name,
                Domain = a.App.Domain,
                PageViews = a.PageViews,
                Sessions = a.Sessions
            })
            .ToList();

        // Chart data - daily aggregation across all apps
        var dailyMetrics = allMetrics
            .GroupBy(m => m.HourStart.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { Date = g.Key, PageViews = g.Sum(m => m.PageViews) })
            .ToList();

        ChartLabels = dailyMetrics.Select(d => d.Date.ToString("MMM d")).ToList();
        ChartData = dailyMetrics.Select(d => d.PageViews).ToList();
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

    private string? GetOrganizationName()
    {
        var orgClaim = User.FindFirst("organization")?.Value;
        if (string.IsNullOrEmpty(orgClaim))
            return null;

        try
        {
            var doc = JsonDocument.Parse(orgClaim);
            if (doc.RootElement.TryGetProperty("name", out var nameProp))
                return nameProp.GetString();
        }
        catch { }

        return null;
    }

    public class OverviewMetrics
    {
        public int PageViews { get; set; }
        public int Sessions { get; set; }
        public int UniqueVisitors { get; set; }
        public double BounceRate { get; set; }
        public int AvgSessionDuration { get; set; }
        public int TotalApplications { get; set; }
    }

    public class AppMetricSummary
    {
        public string AppId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public int PageViews { get; set; }
        public int Sessions { get; set; }
    }
}
