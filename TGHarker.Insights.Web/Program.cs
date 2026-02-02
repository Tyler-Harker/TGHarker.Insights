using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using TGHarker.Insights.Web.Endpoints;
using TGHarker.Insights.Web.Services;
using TGHarker.Insights.Abstractions.Models.Generated;
using TGHarker.Orleans.Search.Orleans.Extensions;
using TGHarker.Orleans.Search.PostgreSQL.Extensions;
using Orleans.Configuration;
using Azure.Data.Tables;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;

var isAspireManaged = !string.IsNullOrEmpty(builder.Configuration["Orleans:Clustering:ProviderType"]);

if (isAspireManaged)
{
    // Orleans client - Aspire auto-configures clustering from WithReference(orleans.AsClient())
    builder.AddKeyedAzureTableServiceClient("clustering");
    builder.UseOrleansClient();
}
else
{
    // Production: Configure Orleans client manually
    var storageConnectionString = builder.Configuration.GetConnectionString("AzureStorage")
        ?? throw new InvalidOperationException("Azure Storage connection string not configured. Set 'ConnectionStrings:AzureStorage'.");

    builder.UseOrleansClient(clientBuilder =>
    {
        clientBuilder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = builder.Configuration["Orleans:ClusterId"] ?? "identity-cluster";
            options.ServiceId = builder.Configuration["Orleans:ServiceId"] ?? "identity-service";
        });

        clientBuilder.UseAzureStorageClustering(options =>
        {
            options.TableServiceClient = new TableServiceClient(storageConnectionString);
        });
    });
}

// Orleans Search
builder.Services.AddOrleansSearch()
    .UsePostgreSql(builder.Configuration.GetConnectionString("searchdb-insights")!);

// Authentication with Identity.Harker.Dev
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Identity:Authority"] ?? "https://identity.harker.dev/tenant/harker";
    options.ClientId = builder.Configuration["Identity:ClientId"] ?? "insights";
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = false;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");

    // Map organization claims from ID token
    options.ClaimActions.MapJsonKey("organizations", "organizations");
    options.ClaimActions.MapJsonKey("organization", "organization");

    // Pass organization_id to authorize endpoint when switching orgs
    options.Events.OnRedirectToIdentityProvider = context =>
    {
        if (context.Properties.Items.TryGetValue("organization_id", out var orgId))
        {
            context.ProtocolMessage.SetParameter("organization_id", orgId);
        }
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

// Theme service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ThemeService>();

// Razor Pages
builder.Services.AddRazorPages();

// CORS for collect endpoints (browser SDK calls from customer domains)
builder.Services.AddCors();

// API
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Handle forwarded headers from reverse proxy (required for HTTPS redirect URIs behind proxy)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseStaticFiles();
app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapDefaultEndpoints();

// Map API endpoints
app.MapCollectEndpoints();
app.MapApplicationEndpoints();
app.MapAnalyticsEndpoints();
app.MapGoalEndpoints();
app.MapThemeEndpoints();

// Map Razor Pages
app.MapRazorPages();

// Login/logout endpoints
app.MapGet("/login", (HttpContext context) =>
{
    return Results.Challenge(new() { RedirectUri = "/dashboard" }, [OpenIdConnectDefaults.AuthenticationScheme]);
});

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapGet("/switch-org", async (HttpContext context, string organization_id) =>
{
    // Challenge with the new organization_id
    var properties = new AuthenticationProperties
    {
        RedirectUri = "/dashboard",
        Items = { { "organization_id", organization_id } }
    };

    return Results.Challenge(properties, [OpenIdConnectDefaults.AuthenticationScheme]);
});

app.Run();
