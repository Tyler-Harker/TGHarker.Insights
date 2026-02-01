using Orleans;
using Orleans.Runtime;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;

namespace TGHarker.Insights.Grains.Analytics;

public class ApplicationGrain : Grain, IApplicationGrain
{
    private readonly IPersistentState<ApplicationState> _state;

    public ApplicationGrain(
        [PersistentState("application", "Default")] IPersistentState<ApplicationState> state)
    {
        _state = state;
    }

    public Task<ApplicationInfo> GetInfoAsync()
    {
        var state = _state.State;
        return Task.FromResult(new ApplicationInfo(
            state.Id,
            state.Name,
            state.OwnerId,
            state.Domain,
            state.ApiKey,
            state.CreatedAt,
            state.IsActive,
            state.Settings
        ));
    }

    public async Task CreateAsync(CreateApplicationRequest request)
    {
        _state.State = new ApplicationState
        {
            Id = this.GetPrimaryKeyString(),
            Name = request.Name,
            OwnerId = request.OwnerId,
            Domain = request.Domain,
            ApiKey = GenerateApiKey(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Settings = new ApplicationSettings()
        };

        await _state.WriteStateAsync();
    }

    public async Task UpdateAsync(UpdateApplicationRequest request)
    {
        if (request.Name is not null)
            _state.State.Name = request.Name;

        if (request.Domain is not null)
            _state.State.Domain = request.Domain;

        if (request.IsActive.HasValue)
            _state.State.IsActive = request.IsActive.Value;

        if (request.Settings is not null)
            _state.State.Settings = request.Settings;

        await _state.WriteStateAsync();
    }

    public Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        return Task.FromResult(_state.State.ApiKey == apiKey && _state.State.IsActive);
    }

    public async Task RegenerateApiKeyAsync()
    {
        _state.State.ApiKey = GenerateApiKey();
        await _state.WriteStateAsync();
    }

    public async Task DeleteAsync()
    {
        await _state.ClearStateAsync();
    }

    public async Task RegisterUserAttributeKeysAsync(IEnumerable<string> keys)
    {
        var now = DateTime.UtcNow;
        var changed = false;

        foreach (var key in keys)
        {
            if (_state.State.UserAttributes.TryGetValue(key, out var existing))
            {
                existing.LastSeen = now;
                changed = true;
            }
            else
            {
                _state.State.UserAttributes[key] = new UserAttributeDefinition
                {
                    Key = key,
                    IsFilterable = true,
                    FirstSeen = now,
                    LastSeen = now
                };
                changed = true;
            }
        }

        if (changed)
        {
            await _state.WriteStateAsync();
        }
    }

    public async Task SetUserAttributeFilterableAsync(string key, bool isFilterable)
    {
        if (_state.State.UserAttributes.TryGetValue(key, out var attr))
        {
            attr.IsFilterable = isFilterable;
            await _state.WriteStateAsync();
        }
    }

    public Task<List<UserAttributeDefinition>> GetUserAttributesAsync()
    {
        return Task.FromResult(_state.State.UserAttributes.Values.ToList());
    }

    private static string GenerateApiKey()
    {
        return $"ins_{Guid.NewGuid():N}";
    }
}
