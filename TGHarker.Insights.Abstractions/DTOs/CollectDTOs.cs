using Orleans;
using System.Text.Json;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record CollectRequest(
    [property: Id(0)] string Type,
    [property: Id(1)] string ApplicationId,
    [property: Id(2)] string VisitorId,
    [property: Id(3)] string SessionId,
    [property: Id(4)] DateTime Timestamp,
    [property: Id(5)] JsonElement Data,
    [property: Id(6)] ContextData Context
);

[GenerateSerializer]
public record ContextData(
    [property: Id(0)] string Url,
    [property: Id(1)] string UserAgent,
    [property: Id(2)] int ScreenWidth,
    [property: Id(3)] int ScreenHeight,
    [property: Id(4)] string Language,
    [property: Id(5)] string Timezone
);

[GenerateSerializer]
public record CollectBatchRequest(
    [property: Id(0)] List<CollectRequest> Events
);
