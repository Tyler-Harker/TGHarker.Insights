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
            state.Settings,
            state.OrganizationId
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
            OrganizationId = request.OrganizationId,
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

    public Task<bool> ValidateOriginAsync(string origin)
    {
        if (!_state.State.IsActive)
            return Task.FromResult(false);

        // If no allowed origins and no domain configured, allow all (for backwards compatibility / ease of setup)
        if (_state.State.AllowedOrigins.Count == 0 && string.IsNullOrEmpty(_state.State.Domain))
            return Task.FromResult(true);

        // Check if origin matches the configured domain
        if (!string.IsNullOrEmpty(_state.State.Domain) && MatchesOrigin(_state.State.Domain, origin))
            return Task.FromResult(true);

        // Check if origin matches any allowed origin
        var isValid = _state.State.AllowedOrigins.Any(allowed =>
            string.Equals(allowed, "*", StringComparison.OrdinalIgnoreCase) ||
            MatchesOrigin(allowed, origin));

        return Task.FromResult(isValid);
    }

    private static bool MatchesOrigin(string allowed, string origin)
    {
        if (string.IsNullOrEmpty(origin))
            return false;

        // Direct match
        if (string.Equals(allowed, origin, StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            var originUri = new Uri(origin);
            var host = originUri.Host;

            // Wildcard subdomain match (e.g., "*.example.com" matches "sub.example.com")
            if (allowed.StartsWith("*."))
            {
                var suffix = allowed[1..]; // ".example.com"
                return host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(host, allowed[2..], StringComparison.OrdinalIgnoreCase); // also match "example.com"
            }

            // Check if origin's host matches the allowed domain
            // e.g., allowed="example.com" should match origin="https://example.com"
            return string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public Task<List<string>> GetAllowedOriginsAsync()
    {
        return Task.FromResult(_state.State.AllowedOrigins.ToList());
    }

    public async Task SetAllowedOriginsAsync(List<string> origins)
    {
        _state.State.AllowedOrigins = origins;
        await _state.WriteStateAsync();
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
