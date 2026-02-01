namespace TGHarker.Insights.Web.Services;

public class ThemeService
{
    private const string ThemeCookieName = "insights_theme";
    private const string DefaultTheme = "dark";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ThemeService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetTheme()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return DefaultTheme;

        if (context.Request.Cookies.TryGetValue(ThemeCookieName, out var theme))
        {
            return theme == "light" ? "light" : "dark";
        }

        return DefaultTheme;
    }

    public void SetTheme(string theme)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return;

        var validTheme = theme == "light" ? "light" : "dark";

        context.Response.Cookies.Append(ThemeCookieName, validTheme, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = false, // Allow JavaScript to read for toggle animation
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    public bool IsDarkMode => GetTheme() == "dark";
}
