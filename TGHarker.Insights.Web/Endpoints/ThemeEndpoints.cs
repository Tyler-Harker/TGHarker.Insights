using TGHarker.Insights.Web.Services;

namespace TGHarker.Insights.Web.Endpoints;

public static class ThemeEndpoints
{
    public static void MapThemeEndpoints(this WebApplication app)
    {
        app.MapPost("/api/theme", (HttpContext context, ThemeService themeService, string theme) =>
        {
            themeService.SetTheme(theme);
            return Results.Ok(new { theme = themeService.GetTheme() });
        }).AllowAnonymous();

        app.MapGet("/api/theme", (ThemeService themeService) =>
        {
            return Results.Ok(new { theme = themeService.GetTheme() });
        }).AllowAnonymous();
    }
}
