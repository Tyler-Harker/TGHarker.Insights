using Microsoft.AspNetCore.Mvc;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;
using TGHarker.Insights.Web.Pages.Dashboard;

namespace TGHarker.Insights.Web.Pages.Dashboard.Application;

public class FunnelsModel : DashboardPageModel
{
    public FunnelsModel(IClusterClient client) : base(client)
    {
    }

    public List<FunnelInfo> Funnels { get; set; } = [];
    public Dictionary<string, FunnelAnalytics> FunnelAnalytics { get; set; } = new();

    // Autocomplete data
    public List<string> AvailableRoutes { get; set; } = [];
    public List<string> AvailableEventCategories { get; set; } = [];
    public Dictionary<string, List<string>> EventActionsByCategory { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "30d";

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentPage = "funnels";

        var result = await LoadApplicationDataAsync();
        if (result != null)
            return result;

        // Load all funnels for this application - fetch all in parallel
        var funnelGrains = await Client.Search<IFunnelGrain>()
            .Where(f => f.ApplicationId == ApplicationId && f.IsActive == true)
            .ToListAsync();

        var funnelInfoTasks = funnelGrains.Select(g => g.GetInfoAsync());
        var funnelInfos = await Task.WhenAll(funnelInfoTasks);
        Funnels = funnelInfos.Where(info => !string.IsNullOrEmpty(info.Id)).ToList();

        // Calculate analytics for each funnel
        var to = DateTime.UtcNow;
        var from = Range switch
        {
            "7d" => DateTime.UtcNow.Date.AddDays(-7),
            "today" => DateTime.UtcNow.Date,
            _ => DateTime.UtcNow.Date.AddDays(-30) // default 30d
        };

        foreach (var funnel in Funnels)
        {
            var analytics = await CalculateFunnelAnalyticsAsync(funnel, from, to);
            FunnelAnalytics[funnel.Id] = analytics;
        }

        // Load autocomplete data
        await LoadAutocompleteDataAsync(from, to);

        return Page();
    }

    public async Task<IActionResult> OnPostCreateFunnelAsync(string funnelName, string stepsJson)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var applicationGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        var appInfo = await applicationGrain.GetInfoAsync();
        if (string.IsNullOrEmpty(appInfo.Id))
            return Forbid();

        var steps = System.Text.Json.JsonSerializer.Deserialize<List<FunnelStep>>(stepsJson,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (steps == null || steps.Count < 2)
        {
            return BadRequest("A funnel must have at least 2 steps");
        }

        // Set order for each step
        for (int i = 0; i < steps.Count; i++)
        {
            steps[i].Order = i + 1;
        }

        var funnelId = Guid.NewGuid().ToString("N");
        var funnelGrain = Client.GetGrain<IFunnelGrain>($"funnel-{ApplicationId}-{funnelId}");
        await funnelGrain.CreateAsync(ApplicationId, appInfo.OrganizationId, new CreateFunnelRequest(funnelName, steps));

        return RedirectToPage(new { applicationId = ApplicationId });
    }

    public async Task<IActionResult> OnPostDeleteFunnelAsync(string funnelId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var funnelGrain = Client.GetGrain<IFunnelGrain>(funnelId);
        await funnelGrain.DeleteAsync();

        return RedirectToPage(new { applicationId = ApplicationId });
    }

    private async Task<FunnelAnalytics> CalculateFunnelAnalyticsAsync(FunnelInfo funnel, DateTime from, DateTime to)
    {
        // Use pre-computed summary grain for scalability
        var summaryGrain = Client.GetGrain<IFunnelSummaryGrain>($"funnel-summary-{ApplicationId}-{funnel.Id}");
        var analytics = await summaryGrain.GetAnalyticsAsync(from, to);

        // If no pre-computed data, fall back to limited sample-based calculation
        if (analytics.TotalEntries == 0)
        {
            return await CalculateFunnelAnalyticsSampledAsync(funnel, from, to);
        }

        return analytics;
    }

    /// <summary>
    /// Calculates funnel analytics using a limited sample for scalability.
    /// This prevents loading millions of records into memory.
    /// </summary>
    private async Task<FunnelAnalytics> CalculateFunnelAnalyticsSampledAsync(FunnelInfo funnel, DateTime from, DateTime to)
    {
        const int maxRecords = 10000; // Limit records to prevent memory issues

        // Get limited page views for this application - fetch all in parallel
        var pageViewGrains = await Client.Search<IPageViewGrain>()
            .Where(pv => pv.ApplicationId == ApplicationId && pv.Timestamp >= from && pv.Timestamp <= to)
            .Take(maxRecords)
            .ToListAsync();

        var pageViewInfoTasks = pageViewGrains.Select(g => g.GetInfoAsync());
        var pageViewInfos = (await Task.WhenAll(pageViewInfoTasks)).ToList();

        // Get limited events for this application - fetch all in parallel
        var eventGrains = await Client.Search<IEventGrain>()
            .Where(e => e.ApplicationId == ApplicationId && e.Timestamp >= from && e.Timestamp <= to)
            .Take(maxRecords)
            .ToListAsync();

        var eventInfoTasks = eventGrains.Select(g => g.GetInfoAsync());
        var eventInfos = (await Task.WhenAll(eventInfoTasks)).ToList();

        // Group data by visitor
        var pageViewsByVisitor = pageViewInfos.GroupBy(pv => pv.VisitorId).ToDictionary(g => g.Key, g => g.OrderBy(pv => pv.Timestamp).ToList());
        var eventsByVisitor = eventInfos.GroupBy(e => e.VisitorId).ToDictionary(g => g.Key, g => g.OrderBy(e => e.Timestamp).ToList());

        // Get unique visitors
        var allVisitors = pageViewsByVisitor.Keys.Union(eventsByVisitor.Keys).ToList();

        // Calculate step completions
        var stepAnalytics = new List<FunnelStepAnalytics>();
        var previousStepVisitors = new HashSet<string>(allVisitors);

        foreach (var step in funnel.Steps.OrderBy(s => s.Order))
        {
            var stepVisitors = new HashSet<string>();

            foreach (var visitorId in previousStepVisitors)
            {
                if (step.Type == FunnelStepType.PageVisit)
                {
                    if (pageViewsByVisitor.TryGetValue(visitorId, out var pageViews))
                    {
                        if (pageViews.Any(pv => MatchesPath(pv.PagePath, step.PagePath)))
                        {
                            stepVisitors.Add(visitorId);
                        }
                    }
                }
                else if (step.Type == FunnelStepType.Event)
                {
                    if (eventsByVisitor.TryGetValue(visitorId, out var events))
                    {
                        if (events.Any(e => e.Category == step.EventCategory && e.Action == step.EventAction))
                        {
                            stepVisitors.Add(visitorId);
                        }
                    }
                }
            }

            var visitors = stepVisitors.Count;
            var previousVisitors = previousStepVisitors.Count;
            var conversionRate = previousVisitors > 0 ? (double)visitors / previousVisitors * 100 : 0;
            var dropOffRate = previousVisitors > 0 ? (double)(previousVisitors - visitors) / previousVisitors * 100 : 0;

            stepAnalytics.Add(new FunnelStepAnalytics(
                step.Order,
                step.Name,
                step.Type,
                visitors,
                conversionRate,
                dropOffRate
            ));

            previousStepVisitors = stepVisitors;
        }

        var totalEntries = stepAnalytics.FirstOrDefault()?.Visitors ?? 0;
        var totalCompletions = stepAnalytics.LastOrDefault()?.Visitors ?? 0;
        var overallConversionRate = totalEntries > 0 ? (double)totalCompletions / totalEntries * 100 : 0;

        return new FunnelAnalytics(
            funnel.Id,
            funnel.Name,
            stepAnalytics,
            totalEntries,
            totalCompletions,
            overallConversionRate
        );
    }

    private static bool MatchesPath(string actualPath, string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return false;

        // Support wildcard matching
        if (pattern.EndsWith("*"))
        {
            var prefix = pattern[..^1];
            return actualPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return actualPath.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private async Task LoadAutocompleteDataAsync(DateTime from, DateTime to)
    {
        const int maxSamples = 5000;

        // Load unique page paths - fetch all in parallel
        var pageViewGrains = await Client.Search<IPageViewGrain>()
            .Where(pv => pv.ApplicationId == ApplicationId && pv.Timestamp >= from && pv.Timestamp <= to)
            .Take(maxSamples)
            .ToListAsync();

        var pageViewInfoTasks = pageViewGrains.Select(g => g.GetInfoAsync());
        var pageViewInfos = await Task.WhenAll(pageViewInfoTasks);

        var routes = pageViewInfos
            .Where(info => !string.IsNullOrEmpty(info.PagePath))
            .Select(info => info.PagePath)
            .ToHashSet();
        AvailableRoutes = routes.OrderBy(r => r).ToList();

        // Load unique event categories and actions - fetch all in parallel
        var eventGrains = await Client.Search<IEventGrain>()
            .Where(e => e.ApplicationId == ApplicationId && e.Timestamp >= from && e.Timestamp <= to)
            .Take(maxSamples)
            .ToListAsync();

        var eventInfoTasks = eventGrains.Select(g => g.GetInfoAsync());
        var eventInfos = await Task.WhenAll(eventInfoTasks);

        var categories = new HashSet<string>();
        var actionsByCategory = new Dictionary<string, HashSet<string>>();

        foreach (var info in eventInfos)
        {
            if (!string.IsNullOrEmpty(info.Category))
            {
                categories.Add(info.Category);

                if (!actionsByCategory.ContainsKey(info.Category))
                {
                    actionsByCategory[info.Category] = new HashSet<string>();
                }

                if (!string.IsNullOrEmpty(info.Action))
                {
                    actionsByCategory[info.Category].Add(info.Action);
                }
            }
        }

        AvailableEventCategories = categories.OrderBy(c => c).ToList();
        EventActionsByCategory = actionsByCategory.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.OrderBy(a => a).ToList()
        );
    }
}
