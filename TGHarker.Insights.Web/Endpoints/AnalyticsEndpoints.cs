using System.Security.Claims;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;

namespace TGHarker.Insights.Web.Endpoints;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/applications/{applicationId}/analytics")
            .WithTags("Analytics")
            .RequireAuthorization();

        group.MapGet("/overview", HandleOverview);
        group.MapGet("/realtime", HandleRealtime);
        group.MapGet("/pages", HandlePages);
        group.MapGet("/sources", HandleSources);
        group.MapGet("/events", HandleEvents);
        group.MapGet("/conversions", HandleConversions);
        group.MapGet("/retention", HandleRetention);
    }

    private static async Task<IResult> HandleOverview(
        string applicationId,
        DateTime? from,
        DateTime? to,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        // Get hourly metrics for the date range
        var metrics = new List<HourlyMetrics>();
        var current = fromDate;
        while (current <= toDate)
        {
            var grainKey = $"metrics-hourly-{applicationId}-{current:yyyyMMddHH}";
            var grain = client.GetGrain<IHourlyMetricsGrain>(grainKey);
            metrics.Add(await grain.GetMetricsAsync());
            current = current.AddHours(1);
        }

        var totalPageViews = metrics.Sum(m => m.PageViews);
        var totalSessions = metrics.Sum(m => m.Sessions);
        var totalBounces = metrics.Sum(m => m.Bounces);
        var totalDuration = metrics.Sum(m => m.TotalDurationSeconds);

        return Results.Ok(new
        {
            PageViews = totalPageViews,
            Sessions = totalSessions,
            UniqueVisitors = metrics.Sum(m => m.UniqueVisitors),
            Events = metrics.Sum(m => m.Events),
            Conversions = metrics.Sum(m => m.Conversions),
            ConversionValue = metrics.Sum(m => m.ConversionValue),
            BounceRate = totalSessions > 0 ? (double)totalBounces / totalSessions * 100 : 0,
            AvgSessionDuration = totalSessions > 0 ? totalDuration / totalSessions : 0,
            From = fromDate,
            To = toDate
        });
    }

    private static async Task<IResult> HandleRealtime(
        string applicationId,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var grain = client.GetGrain<IRealTimeGrain>($"realtime-{applicationId}");
        var snapshot = await grain.GetSnapshotAsync();

        return Results.Ok(snapshot);
    }

    private static async Task<IResult> HandlePages(
        string applicationId,
        DateTime? from,
        DateTime? to,
        int limit,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;
        var actualLimit = limit > 0 ? limit : 10;

        var pageViewGrains = await client.Search<IPageViewGrain>()
            .Where(pv => pv.ApplicationId == applicationId &&
                        pv.Timestamp >= fromDate &&
                        pv.Timestamp <= toDate)
            .ToListAsync();

        var pageViewInfos = new List<PageViewInfo>();
        foreach (var grain in pageViewGrains)
        {
            pageViewInfos.Add(await grain.GetInfoAsync());
        }

        var grouped = pageViewInfos
            .GroupBy(pv => pv.PagePath)
            .Select(g => new { Path = g.Key, Views = g.Count() })
            .OrderByDescending(x => x.Views)
            .Take(actualLimit)
            .ToList();

        return Results.Ok(grouped);
    }

    private static async Task<IResult> HandleSources(
        string applicationId,
        DateTime? from,
        DateTime? to,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var sessionGrains = await client.Search<ISessionGrain>()
            .Where(s => s.ApplicationId == applicationId &&
                       s.StartedAt >= fromDate &&
                       s.StartedAt <= toDate)
            .ToListAsync();

        var sessionInfos = new List<SessionInfo>();
        foreach (var grain in sessionGrains)
        {
            sessionInfos.Add(await grain.GetInfoAsync());
        }

        var grouped = sessionInfos
            .GroupBy(s => s.Source.ToString())
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        return Results.Ok(grouped);
    }

    private static async Task<IResult> HandleEvents(
        string applicationId,
        DateTime? from,
        DateTime? to,
        string? category,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var query = client.Search<IEventGrain>()
            .Where(e => e.ApplicationId == applicationId &&
                       e.Timestamp >= fromDate &&
                       e.Timestamp <= toDate);

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(e => e.Category == category);
        }

        var eventGrains = await query.ToListAsync();

        var eventInfos = new List<EventInfo>();
        foreach (var grain in eventGrains)
        {
            eventInfos.Add(await grain.GetInfoAsync());
        }

        var grouped = eventInfos
            .GroupBy(e => new { e.Category, e.Action })
            .Select(g => new
            {
                g.Key.Category,
                g.Key.Action,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return Results.Ok(grouped);
    }

    private static async Task<IResult> HandleConversions(
        string applicationId,
        DateTime? from,
        DateTime? to,
        string? goalId,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var fromDate = from ?? DateTime.UtcNow.Date.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var query = client.Search<IConversionGrain>()
            .Where(c => c.ApplicationId == applicationId &&
                       c.Timestamp >= fromDate &&
                       c.Timestamp <= toDate);

        if (!string.IsNullOrEmpty(goalId))
        {
            query = query.Where(c => c.GoalId == goalId);
        }

        var conversionGrains = await query.ToListAsync();

        var conversionInfos = new List<ConversionInfo>();
        foreach (var grain in conversionGrains)
        {
            conversionInfos.Add(await grain.GetInfoAsync());
        }

        var grouped = conversionInfos
            .GroupBy(c => c.GoalId)
            .Select(g => new
            {
                GoalId = g.Key,
                Count = g.Count(),
                TotalValue = g.Sum(c => c.Value ?? 0)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return Results.Ok(grouped);
    }

    private static async Task<IResult> HandleRetention(
        string applicationId,
        int? cohortWeeks,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
            return Results.Forbid();

        var weeks = cohortWeeks ?? 8;
        var cohorts = new List<RetentionCohortData>();

        var currentWeek = GetIsoWeek(DateTime.UtcNow);
        for (var i = 0; i < weeks; i++)
        {
            var weekOffset = DateTime.UtcNow.AddDays(-7 * i);
            var cohortWeek = GetIsoWeek(weekOffset);
            var grain = client.GetGrain<IRetentionCohortGrain>($"cohort-{applicationId}-{cohortWeek}");
            cohorts.Add(await grain.GetDataAsync());
        }

        return Results.Ok(cohorts);
    }

    private static async Task<bool> ValidateApplicationAccess(
        string applicationId,
        HttpContext context,
        IClusterClient client)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return false;

        var grain = client.GetGrain<IApplicationGrain>($"app-{applicationId}");
        var info = await grain.GetInfoAsync();

        return info.OwnerId == userId;
    }

    private static string GetIsoWeek(DateTime date)
    {
        var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
        var week = cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        return $"{date.Year}W{week:D2}";
    }
}
