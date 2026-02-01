using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Abstractions.Grains;

public interface IApplicationGrain : IGrainWithStringKey
{
    Task<ApplicationInfo> GetInfoAsync();
    Task CreateAsync(CreateApplicationRequest request);
    Task UpdateAsync(UpdateApplicationRequest request);
    Task<bool> ValidateApiKeyAsync(string apiKey);
    Task RegenerateApiKeyAsync();
    Task DeleteAsync();
    Task RegisterUserAttributeKeysAsync(IEnumerable<string> keys);
    Task SetUserAttributeFilterableAsync(string key, bool isFilterable);
    Task<List<UserAttributeDefinition>> GetUserAttributesAsync();
}
