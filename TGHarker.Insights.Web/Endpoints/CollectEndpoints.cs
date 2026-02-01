using System.Text.Json;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Web.Endpoints;

public static class CollectEndpoints
{
    public static void MapCollectEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/collect")
            .WithTags("Collect")
            .AllowAnonymous();

        group.MapPost("/", HandleCollect)
            .WithName("Collect");

        group.MapPost("/batch", HandleBatchCollect)
            .WithName("CollectBatch");

        group.MapGet("/config/{applicationId}", HandleGetConfig)
            .WithName("GetCollectConfig");
    }

    private static async Task<IResult> HandleCollect(
        HttpContext context,
        CollectRequest request,
        IClusterClient client,
        ILogger<CollectEndpointHandler> logger)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
            return Results.Unauthorized();

        var propertyGrain = client.GetGrain<IApplicationGrain>($"app-{request.ApplicationId}");
        if (!await propertyGrain.ValidateApiKeyAsync(apiKey))
            return Results.Unauthorized();

        await ProcessEvent(client, request, logger);
        return Results.NoContent();
    }

    private static async Task<IResult> HandleBatchCollect(
        HttpContext context,
        CollectBatchRequest request,
        IClusterClient client,
        ILogger<CollectEndpointHandler> logger)
    {
        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
            return Results.Unauthorized();

        if (request.Events.Count == 0)
            return Results.NoContent();

        var applicationId = request.Events.First().ApplicationId;
        var propertyGrain = client.GetGrain<IApplicationGrain>($"app-{applicationId}");
        if (!await propertyGrain.ValidateApiKeyAsync(apiKey))
            return Results.Unauthorized();

        foreach (var evt in request.Events)
        {
            await ProcessEvent(client, evt, logger);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> HandleGetConfig(
        string applicationId,
        IClusterClient client)
    {
        var propertyGrain = client.GetGrain<IApplicationGrain>($"app-{applicationId}");
        var info = await propertyGrain.GetInfoAsync();

        return Results.Ok(new
        {
            ApplicationId = applicationId,
            Settings = new
            {
                info.Settings.SessionTimeoutMinutes,
                info.Settings.TrackPageViews,
                info.Settings.TrackEvents,
                info.Settings.TrackScrollDepth,
                info.Settings.SamplingRate,
                info.Settings.ExcludedPaths
            }
        });
    }

    private static async Task ProcessEvent(
        IClusterClient client,
        CollectRequest request,
        ILogger logger)
    {
        var visitorGrainKey = $"visitor-{request.ApplicationId}-{request.VisitorId}";
        var sessionGrainKey = $"session-{request.ApplicationId}-{request.SessionId}";

        switch (request.Type.ToLowerInvariant())
        {
            case "session_start":
                await HandleSessionStart(client, request, visitorGrainKey, sessionGrainKey);
                break;

            case "pageview":
                await HandlePageView(client, request, sessionGrainKey);
                break;

            case "event":
                await HandleEvent(client, request, sessionGrainKey);
                break;

            case "session_end":
                await HandleSessionEnd(client, request, sessionGrainKey);
                break;

            case "identify":
                await HandleIdentify(client, request, visitorGrainKey);
                break;

            case "set_attributes":
                await HandleSetAttributes(client, request, visitorGrainKey);
                break;

            default:
                logger.LogWarning("Unknown event type: {EventType}", request.Type);
                break;
        }

        // Update real-time tracking (sharded for horizontal scalability)
        var shardId = GetRealTimeShardId(request.VisitorId);
        var realTimeShardGrain = client.GetGrain<IRealTimeShardGrain>(
            $"realtime-shard-{request.ApplicationId}-{shardId}");
        var url = request.Context.Url;
        var path = new Uri(url).AbsolutePath;
        await realTimeShardGrain.RecordActiveVisitorAsync(request.VisitorId, path);
    }

    private static async Task HandleSessionStart(
        IClusterClient client,
        CollectRequest request,
        string visitorGrainKey,
        string sessionGrainKey)
    {
        var data = request.Data;

        // Update visitor
        var visitorGrain = client.GetGrain<IVisitorGrain>(visitorGrainKey);
        await visitorGrain.RecordVisitAsync(new VisitData(
            request.ApplicationId,
            null,
            null,
            request.Context.UserAgent,
            ParseDeviceInfo(request.Context.UserAgent)
        ));

        // Start session
        var sessionGrain = client.GetGrain<ISessionGrain>(sessionGrainKey);
        await sessionGrain.StartAsync(new SessionStartData(
            request.ApplicationId,
            request.VisitorId,
            GetJsonString(data, "referrer"),
            GetJsonString(data, "landingPage"),
            GetJsonString(data, "utmSource"),
            GetJsonString(data, "utmMedium"),
            GetJsonString(data, "utmCampaign")
        ));
    }

    private static async Task HandlePageView(
        IClusterClient client,
        CollectRequest request,
        string sessionGrainKey)
    {
        var data = request.Data;
        var pageViewId = Guid.NewGuid().ToString("N");
        var pageViewGrainKey = $"pv-{request.ApplicationId}-{pageViewId}";

        // Record page view
        var pageViewGrain = client.GetGrain<IPageViewGrain>(pageViewGrainKey);
        await pageViewGrain.RecordAsync(new PageViewRecordData(
            request.ApplicationId,
            request.SessionId,
            request.VisitorId,
            GetJsonString(data, "path") ?? "/",
            GetJsonString(data, "title") ?? "",
            request.Timestamp
        ));

        // Update session
        var sessionGrain = client.GetGrain<ISessionGrain>(sessionGrainKey);
        await sessionGrain.RecordPageViewAsync(new PageViewData(
            GetJsonString(data, "path") ?? "/",
            GetJsonString(data, "title") ?? "",
            request.Timestamp
        ));
    }

    private static async Task HandleEvent(
        IClusterClient client,
        CollectRequest request,
        string sessionGrainKey)
    {
        var data = request.Data;
        var eventId = Guid.NewGuid().ToString("N");
        var eventGrainKey = $"event-{request.ApplicationId}-{eventId}";

        var category = GetJsonString(data, "category") ?? "Unknown";
        var action = GetJsonString(data, "action") ?? "Unknown";

        // Record event
        var eventGrain = client.GetGrain<IEventGrain>(eventGrainKey);
        await eventGrain.RecordAsync(new EventRecordData(
            request.ApplicationId,
            request.SessionId,
            request.VisitorId,
            category,
            action,
            GetJsonString(data, "label"),
            GetJsonDecimal(data, "value"),
            request.Timestamp,
            null
        ));

        // Update session
        var sessionGrain = client.GetGrain<ISessionGrain>(sessionGrainKey);
        await sessionGrain.RecordEventAsync(new EventData(
            category,
            action,
            GetJsonString(data, "label"),
            GetJsonDecimal(data, "value"),
            request.Timestamp
        ));
    }

    private static async Task HandleSessionEnd(
        IClusterClient client,
        CollectRequest request,
        string sessionGrainKey)
    {
        var data = request.Data;
        var sessionGrain = client.GetGrain<ISessionGrain>(sessionGrainKey);
        await sessionGrain.EndAsync(GetJsonString(data, "exitPage"));

        // Remove from real-time (sharded)
        var shardId = GetRealTimeShardId(request.VisitorId);
        var realTimeShardGrain = client.GetGrain<IRealTimeShardGrain>(
            $"realtime-shard-{request.ApplicationId}-{shardId}");
        await realTimeShardGrain.RemoveActiveVisitorAsync(request.VisitorId);
    }

    private static async Task HandleIdentify(
        IClusterClient client,
        CollectRequest request,
        string visitorGrainKey)
    {
        var data = request.Data;
        var userId = GetJsonString(data, "userId");
        if (!string.IsNullOrEmpty(userId))
        {
            var visitorGrain = client.GetGrain<IVisitorGrain>(visitorGrainKey);
            await visitorGrain.IdentifyAsync(userId);
        }
    }

    private static async Task HandleSetAttributes(
        IClusterClient client,
        CollectRequest request,
        string visitorGrainKey)
    {
        var data = request.Data;
        if (data.TryGetProperty("attributes", out var attributesElement) &&
            attributesElement.ValueKind == JsonValueKind.Object)
        {
            var attributes = new Dictionary<string, string>();
            foreach (var prop in attributesElement.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    attributes[prop.Name] = prop.Value.GetString() ?? "";
                }
                else
                {
                    attributes[prop.Name] = prop.Value.ToString();
                }
            }

            if (attributes.Count > 0)
            {
                var visitorGrain = client.GetGrain<IVisitorGrain>(visitorGrainKey);
                await visitorGrain.SetAttributesAsync(attributes);

                // Register the attribute keys with the application
                var applicationGrain = client.GetGrain<IApplicationGrain>($"app-{request.ApplicationId}");
                await applicationGrain.RegisterUserAttributeKeysAsync(attributes.Keys);
            }
        }
    }

    private static string? GetJsonString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }

    private static decimal? GetJsonDecimal(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDecimal();
        }
        return null;
    }

    private const int RealTimeShardCount = 16;

    private static int GetRealTimeShardId(string visitorId)
    {
        var hash = visitorId.GetHashCode();
        return Math.Abs(hash % RealTimeShardCount);
    }

    private static DeviceInfo ParseDeviceInfo(string userAgent)
    {
        var ua = userAgent.ToLowerInvariant();
        return new DeviceInfo
        {
            IsMobile = ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"),
            DeviceType = ua.Contains("mobile") || ua.Contains("iphone") ? "Mobile" :
                        ua.Contains("tablet") || ua.Contains("ipad") ? "Tablet" : "Desktop",
            Browser = ua.Contains("chrome") ? "Chrome" :
                     ua.Contains("firefox") ? "Firefox" :
                     ua.Contains("safari") ? "Safari" :
                     ua.Contains("edge") ? "Edge" : "Unknown",
            OS = ua.Contains("windows") ? "Windows" :
                ua.Contains("mac") ? "macOS" :
                ua.Contains("linux") ? "Linux" :
                ua.Contains("android") ? "Android" :
                ua.Contains("ios") || ua.Contains("iphone") ? "iOS" : "Unknown"
        };
    }
}

public class CollectEndpointHandler { }
