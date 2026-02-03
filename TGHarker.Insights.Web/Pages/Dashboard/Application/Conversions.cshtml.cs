using Microsoft.AspNetCore.Mvc;
using Orleans;
using TGHarker.Insights.Abstractions.DTOs;
using TGHarker.Insights.Abstractions.Enums;
using TGHarker.Insights.Abstractions.Grains;
using TGHarker.Insights.Abstractions.Models;
using TGHarker.Orleans.Search.Core.Extensions;
using TGHarker.Orleans.Search.Generated;
using TGHarker.Insights.Web.Pages.Dashboard;

namespace TGHarker.Insights.Web.Pages.Dashboard.Application;

public class ConversionsModel : DashboardPageModel
{
    public ConversionsModel(IClusterClient client) : base(client)
    {
    }

    public int TotalConversions { get; set; }
    public double OverallConversionRate { get; set; }
    public List<GoalStat> Goals { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string Range { get; set; } = "today";

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentPage = "conversions";

        var result = await LoadApplicationDataAsync();
        if (result != null)
            return result;

        var to = DateTime.UtcNow;
        var from = Range switch
        {
            "7d" => DateTime.UtcNow.Date.AddDays(-7),
            "today" => DateTime.UtcNow.Date,
            _ => DateTime.UtcNow.Date.AddDays(-30) // default 30d
        };

        // Get total sessions for conversion rate calculation
        var sessionGrains = await Client.Search<ISessionGrain>()
            .Where(s => s.ApplicationId == ApplicationId && s.StartedAt >= from && s.StartedAt <= to)
            .ToListAsync();
        var totalSessions = sessionGrains.Count;

        // Get goals for this property - fetch all in parallel
        var goalGrains = await Client.Search<IGoalGrain>()
            .Where(g => g.ApplicationId == ApplicationId)
            .ToListAsync();

        var goalInfoTasks = goalGrains.Select(g => g.GetInfoAsync());
        var goalInfos = await Task.WhenAll(goalInfoTasks);

        Goals = goalInfos.Select(goalInfo => new GoalStat
        {
            Name = goalInfo.Name,
            Type = goalInfo.Type.ToString(),
            Conversions = goalInfo.TotalConversions,
            ConversionRate = totalSessions > 0 ? (double)goalInfo.TotalConversions / totalSessions * 100 : 0
        }).ToList();

        TotalConversions = Goals.Sum(g => g.Conversions);
        OverallConversionRate = totalSessions > 0 ? (double)TotalConversions / totalSessions * 100 : 0;

        return Page();
    }

    public async Task<IActionResult> OnPostCreateGoalAsync(string goalName, string goalType, string? pagePath, string? eventName)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var propertyGrain = Client.GetGrain<IApplicationGrain>($"app-{ApplicationId}");
        var property = await propertyGrain.GetInfoAsync();

        if (string.IsNullOrEmpty(property.Id) || property.OwnerId != userId)
            return Forbid();

        var goalId = Guid.NewGuid().ToString("N")[..8];
        var goalGrain = Client.GetGrain<IGoalGrain>($"goal-{ApplicationId}-{goalId}");

        var type = goalType == "Event" ? GoalType.Event : GoalType.PageView;
        var condition = new GoalCondition();

        if (type == GoalType.Event)
        {
            condition.EventCategory = eventName;
        }
        else
        {
            condition.UrlPattern = pagePath;
        }

        await goalGrain.CreateAsync(new CreateGoalRequest(
            ApplicationId: ApplicationId,
            Name: goalName,
            Type: type,
            Condition: condition,
            MonetaryValue: null,
            OrganizationId: property.OrganizationId
        ));

        return RedirectToPage(new { applicationId = ApplicationId });
    }

    public class GoalStat
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Conversions { get; set; }
        public double ConversionRate { get; set; }
    }
}
