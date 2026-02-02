using Orleans;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Abstractions.DTOs;

[GenerateSerializer]
public record ApplicationInfo(
    [property: Id(0)] string Id,
    [property: Id(1)] string Name,
    [property: Id(2)] string OwnerId,
    [property: Id(3)] string Domain,
    [property: Id(4)] string ApiKey,
    [property: Id(5)] DateTime CreatedAt,
    [property: Id(6)] bool IsActive,
    [property: Id(7)] ApplicationSettings Settings,
    [property: Id(8)] string OrganizationId
);

[GenerateSerializer]
public record CreateApplicationRequest(
    [property: Id(0)] string Name,
    [property: Id(1)] string OwnerId,
    [property: Id(2)] string Domain,
    [property: Id(3)] string OrganizationId
);

[GenerateSerializer]
public record UpdateApplicationRequest(
    [property: Id(0)] string? Name,
    [property: Id(1)] string? Domain,
    [property: Id(2)] bool? IsActive,
    [property: Id(3)] ApplicationSettings? Settings
);
