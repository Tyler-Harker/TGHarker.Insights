using System.Security.Claims;
using System.Text.Json;
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
        group.MapGet("/stream", HandleStream);
    }

    private static async Task HandleStream(
        string applicationId,
        string? range,
        HttpContext context,
        IClusterClient client)
    {
        if (!await ValidateApplicationAccess(applicationId, context, client))
        {
            context.Response.StatusCode = 403;
            return;
        }

        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        var writer = context.Response.BodyWriter;
        var cancellationToken = context.RequestAborted;

        var to = DateTime.UtcNow;
        var from = range switch
        {
            "7d" => DateTime.UtcNow.Date.AddDays(-7),
            "24h" => DateTime.UtcNow.AddHours(-24),
            _ => DateTime.UtcNow.Date.AddDays(-30)
        };
        var isHourlyChart = range == "24h";

        try
        {
            // 1. Stream metrics first (fastest)
            var hourlyGrainKeys = new List<string>();
            var current = from;
            while (current <= to)
            {
                hourlyGrainKeys.Add($"metrics-hourly-{applicationId}-{current:yyyyMMddHH}");
                current = current.AddHours(1);
            }

            var metricsTasks = hourlyGrainKeys.Select(key =>
                client.GetGrain<IHourlyMetricsGrain>(key).GetMetricsAsync());
            var metrics = (await Task.WhenAll(metricsTasks)).ToList();

            var totalSessions = metrics.Sum(m => m.Sessions);
            var metricsData = new
            {
                pageViews = metrics.Sum(m => m.PageViews),
                sessions = totalSessions,
                uniqueVisitors = metrics.Sum(m => m.UniqueVisitors),
                bounceRate = totalSessions > 0
                    ? Math.Round((double)metrics.Sum(m => m.Bounces) / totalSessions * 100, 1)
                    : 0,
                avgSessionDuration = totalSessions > 0
                    ? metrics.Sum(m => m.TotalDurationSeconds) / totalSessions
                    : 0
            };

            await WriteSSEEvent(context.Response, "metrics", metricsData, cancellationToken);

            // 2. Stream chart data
            object chartData;
            if (isHourlyChart)
            {
                var hourlyMetrics = metrics.OrderBy(m => m.HourStart).ToList();
                chartData = new
                {
                    isHourly = true,
                    timestamps = hourlyMetrics.Select(m => m.HourStart.ToString("o")).ToList(),
                    labels = hourlyMetrics.Select(m => m.HourStart.ToString("h tt")).ToList(),
                    data = hourlyMetrics.Select(m => m.PageViews).ToList()
                };
            }
            else
            {
                var dailyMetrics = metrics
                    .GroupBy(m => m.HourStart.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new { Date = g.Key, PageViews = g.Sum(m => m.PageViews) })
                    .ToList();

                chartData = new
                {
                    isHourly = false,
                    labels = dailyMetrics.Select(d => d.Date.ToString("MMM d")).ToList(),
                    data = dailyMetrics.Select(d => d.PageViews).ToList()
                };
            }

            await WriteSSEEvent(context.Response, "chart", chartData, cancellationToken);

            // 3. Stream top pages
            var pageViewGrains = await client.Search<IPageViewGrain>()
                .Where(pv => pv.ApplicationId == applicationId && pv.Timestamp >= from && pv.Timestamp <= to)
                .ToListAsync();

            var pageViewInfoTasks = pageViewGrains.Select(g => g.GetInfoAsync());
            var pageViewInfos = await Task.WhenAll(pageViewInfoTasks);

            var topPages = pageViewInfos
                .GroupBy(pv => pv.PagePath)
                .Select(g => new { path = g.Key, views = g.Count() })
                .OrderByDescending(p => p.views)
                .Take(10)
                .ToList();

            await WriteSSEEvent(context.Response, "topPages", topPages, cancellationToken);

            // 4. Stream traffic sources
            var sessionGrains = await client.Search<ISessionGrain>()
                .Where(s => s.ApplicationId == applicationId && s.StartedAt >= from && s.StartedAt <= to)
                .ToListAsync();

            var sessionInfoTasks = sessionGrains.Select(g => g.GetInfoAsync());
            var sessionInfos = await Task.WhenAll(sessionInfoTasks);

            var sources = sessionInfos
                .GroupBy(s => s.Source.ToString())
                .OrderByDescending(g => g.Count())
                .Select(g => new { source = g.Key, count = g.Count() })
                .ToList();

            await WriteSSEEvent(context.Response, "sources", sources, cancellationToken);

            // 5. Signal completion
            await WriteSSEEvent(context.Response, "complete", new { }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected
        }
    }

    private static async Task WriteSSEEvent(HttpResponse response, string eventName, object data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var message = $"event: {eventName}\ndata: {json}\n\n";
        await response.WriteAsync(message, cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
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
