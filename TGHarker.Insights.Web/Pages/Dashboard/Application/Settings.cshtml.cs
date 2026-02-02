using Microsoft.AspNetCore.Mvc;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;
using TGHarker.Insights.Web.Pages.Dashboard;

namespace TGHarker.Insights.Web.Pages.Dashboard.Application;

public class SettingsModel : DashboardPageModel
{
    public SettingsModel(IClusterClient client) : base(client)
    {
    }

    public string BaseUrl => $"{Request.Scheme}://{Request.Host}";
    public List<UserAttributeDefinition> UserAttributes { get; set; } = [];
    public List<string> AllowedOrigins { get; set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentPage = "settings";

        var result = await LoadApplicationDataAsync();
        if (result != null)
            return result;

        var propertyGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        UserAttributes = await propertyGrain.GetUserAttributesAsync();
        AllowedOrigins = await propertyGrain.GetAllowedOriginsAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateSettingsAsync(string name, string domain)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var propertyGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        var property = await propertyGrain.GetInfoAsync();

        if (string.IsNullOrEmpty(property.Id) || property.OwnerId != userId)
            return Forbid();

        await propertyGrain.UpdateAsync(new UpdateApplicationRequest(
            Name: name,
            Domain: domain,
            IsActive: null,
            Settings: null
        ));

        return RedirectToPage(new { applicationId = ApplicationId });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var propertyGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        var property = await propertyGrain.GetInfoAsync();

        if (string.IsNullOrEmpty(property.Id) || property.OwnerId != userId)
            return Forbid();

        await propertyGrain.DeleteAsync();

        return RedirectToPage("/Dashboard/Index");
    }

    public async Task<IActionResult> OnPostToggleAttributeAsync(string attributeKey, bool isFilterable)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var propertyGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        var property = await propertyGrain.GetInfoAsync();

        if (string.IsNullOrEmpty(property.Id) || property.OwnerId != userId)
            return Forbid();

        await propertyGrain.SetUserAttributeFilterableAsync(attributeKey, isFilterable);

        return RedirectToPage(new { applicationId = ApplicationId });
    }

    public async Task<IActionResult> OnPostAddOriginAsync(string origin)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(origin))
            return RedirectToPage(new { applicationId = ApplicationId });

        var propertyGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        var property = await propertyGrain.GetInfoAsync();

        if (string.IsNullOrEmpty(property.Id) || property.OwnerId != userId)
            return Forbid();

        var origins = await propertyGrain.GetAllowedOriginsAsync();
        var trimmedOrigin = origin.Trim();

        if (!origins.Contains(trimmedOrigin, StringComparer.OrdinalIgnoreCase))
        {
            origins.Add(trimmedOrigin);
            await propertyGrain.SetAllowedOriginsAsync(origins);
        }

        return RedirectToPage(new { applicationId = ApplicationId });
    }

    public async Task<IActionResult> OnPostRemoveOriginAsync(string origin)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var propertyGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        var property = await propertyGrain.GetInfoAsync();

        if (string.IsNullOrEmpty(property.Id) || property.OwnerId != userId)
            return Forbid();

        var origins = await propertyGrain.GetAllowedOriginsAsync();
        origins.RemoveAll(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase));
        await propertyGrain.SetAllowedOriginsAsync(origins);

        return RedirectToPage(new { applicationId = ApplicationId });
    }
}
