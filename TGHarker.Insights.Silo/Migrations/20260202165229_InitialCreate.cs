using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TGHarker.Insights.Silo.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    OwnerId = table.Column<string>(type: "text", nullable: true),
                    OrganizationId = table.Column<string>(type: "text", nullable: true),
                    Domain = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SearchVector = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationStates", x => x.GrainId);
                });

            migrationBuilder.CreateTable(
                name: "ConversionStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: true),
                    GoalId = table.Column<string>(type: "text", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    VisitorId = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    UtmCampaign = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversionStates", x => x.GrainId);
                });

            migrationBuilder.CreateTable(
                name: "DailyMetricsStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: true),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PageViews = table.Column<int>(type: "integer", nullable: false),
                    Sessions = table.Column<int>(type: "integer", nullable: false),
                    UniqueVisitors = table.Column<int>(type: "integer", nullable: false),
                    Events = table.Column<int>(type: "integer", nullable: false),
                    Conversions = table.Column<int>(type: "integer", nullable: false),
                    ConversionValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Bounces = table.Column<int>(type: "integer", nullable: false),
                    TotalDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyMetricsStates", x => x.GrainId);
                });

            migrationBuilder.CreateTable(
                name: "EventStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    VisitorId = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: true),
                    Label = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SearchVector = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventStates", x => x.GrainId);
                });

            migrationBuilder.CreateTable(
                name: "FunnelStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SearchVector = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunnelStates", x => x.GrainId);
                });

            migrationBuilder.CreateTable(
                name: "GoalStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TotalConversions = table.Column<int>(type: "integer", nullable: false),
                    SearchVector = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalStates", x => x.GrainId);
                });

            migrationBuilder.CreateTable(
                name: "HourlyMetricsStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: true),
                    HourStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PageViews = table.Column<int>(type: "integer", nullable: false),
                    Sessions = table.Column<int>(type: "integer", nullable: false),
                    UniqueVisitors = table.Column<int>(type: "integer", nullable: false),
                    Events = table.Column<int>(type: "integer", nullable: false),
                    Conversions = table.Column<int>(type: "integer", nullable: false),
                    ConversionValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Bounces = table.Column<int>(type: "integer", nullable: false),
                    TotalDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HourlyMetricsStates", x => x.GrainId);
                });

            migrationBuilder.CreateTable(
                name: "PageViewStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: true),
                    SessionId = table.Column<string>(type: "text", nullable: true),
                    VisitorId = table.Column<string>(type: "text", nullable: true),
                    PagePath = table.Column<string>(type: "text", nullable: true),
                    PageTitle = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimeOnPageSeconds = table.Column<int>(type: "integer", nullable: false),
                    ScrollDepthPercent = table.Column<int>(type: "integer", nullable: false),
                    SearchVector = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageViewStates", x => x.GrainId);
                });

            migrationBuilder.CreateTable(
                name: "RetentionCohortStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: true),
                    CohortWeek = table.Column<string>(type: "text", nullable: true),
                    TotalVisitors = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionCohortStates", x => x.GrainId);
                });

            migrationBuilder.CreateTable(
                name: "SessionStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: true),
                    VisitorId = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PageViewCount = table.Column<int>(type: "integer", nullable: false),
                    EventCount = table.Column<int>(type: "integer", nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    ReferrerUrl = table.Column<string>(type: "text", nullable: true),
                    ReferrerDomain = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    UtmSource = table.Column<string>(type: "text", nullable: true),
                    UtmMedium = table.Column<string>(type: "text", nullable: true),
                    UtmCampaign = table.Column<string>(type: "text", nullable: true),
                    LandingPage = table.Column<string>(type: "text", nullable: true),
                    ExitPage = table.Column<string>(type: "text", nullable: true),
                    IsBounce = table.Column<bool>(type: "boolean", nullable: false),
                    HasConversion = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionStates", x => x.GrainId);
                });

            migrationBuilder.CreateTable(
                name: "VisitorStates",
                columns: table => new
                {
                    GrainId = table.Column<string>(type: "text", nullable: false),
                    ApplicationId = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalSessions = table.Column<int>(type: "integer", nullable: false),
                    TotalPageViews = table.Column<int>(type: "integer", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitorStates", x => x.GrainId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationState_Domain",
                table: "ApplicationStates",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationState_IsActive",
                table: "ApplicationStates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationState_Name",
                table: "ApplicationStates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationState_OrganizationId",
                table: "ApplicationStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationState_OwnerId",
                table: "ApplicationStates",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationState_SearchVector",
                table: "ApplicationStates",
                column: "SearchVector");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionState_ApplicationId",
                table: "ConversionStates",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionState_GoalId",
                table: "ConversionStates",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionState_SessionId",
                table: "ConversionStates",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionState_Source",
                table: "ConversionStates",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionState_Timestamp",
                table: "ConversionStates",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionState_UtmCampaign",
                table: "ConversionStates",
                column: "UtmCampaign");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionState_VisitorId",
                table: "ConversionStates",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_ApplicationId",
                table: "DailyMetricsStates",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_Bounces",
                table: "DailyMetricsStates",
                column: "Bounces");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_Conversions",
                table: "DailyMetricsStates",
                column: "Conversions");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_ConversionValue",
                table: "DailyMetricsStates",
                column: "ConversionValue");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_Date",
                table: "DailyMetricsStates",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_Events",
                table: "DailyMetricsStates",
                column: "Events");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_PageViews",
                table: "DailyMetricsStates",
                column: "PageViews");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_Sessions",
                table: "DailyMetricsStates",
                column: "Sessions");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_TotalDurationSeconds",
                table: "DailyMetricsStates",
                column: "TotalDurationSeconds");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_UniqueVisitors",
                table: "DailyMetricsStates",
                column: "UniqueVisitors");

            migrationBuilder.CreateIndex(
                name: "IX_EventState_Action",
                table: "EventStates",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_EventState_ApplicationId",
                table: "EventStates",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventState_Category",
                table: "EventStates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_EventState_Label",
                table: "EventStates",
                column: "Label");

            migrationBuilder.CreateIndex(
                name: "IX_EventState_SearchVector",
                table: "EventStates",
                column: "SearchVector");

            migrationBuilder.CreateIndex(
                name: "IX_EventState_SessionId",
                table: "EventStates",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_EventState_Timestamp",
                table: "EventStates",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_EventState_VisitorId",
                table: "EventStates",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_FunnelState_ApplicationId",
                table: "FunnelStates",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_FunnelState_IsActive",
                table: "FunnelStates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FunnelState_Name",
                table: "FunnelStates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_FunnelState_SearchVector",
                table: "FunnelStates",
                column: "SearchVector");

            migrationBuilder.CreateIndex(
                name: "IX_GoalState_ApplicationId",
                table: "GoalStates",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalState_IsActive",
                table: "GoalStates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_GoalState_Name",
                table: "GoalStates",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_GoalState_SearchVector",
                table: "GoalStates",
                column: "SearchVector");

            migrationBuilder.CreateIndex(
                name: "IX_GoalState_TotalConversions",
                table: "GoalStates",
                column: "TotalConversions");

            migrationBuilder.CreateIndex(
                name: "IX_GoalState_Type",
                table: "GoalStates",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_ApplicationId",
                table: "HourlyMetricsStates",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_Bounces",
                table: "HourlyMetricsStates",
                column: "Bounces");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_Conversions",
                table: "HourlyMetricsStates",
                column: "Conversions");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_ConversionValue",
                table: "HourlyMetricsStates",
                column: "ConversionValue");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_Events",
                table: "HourlyMetricsStates",
                column: "Events");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_HourStart",
                table: "HourlyMetricsStates",
                column: "HourStart");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_PageViews",
                table: "HourlyMetricsStates",
                column: "PageViews");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_Sessions",
                table: "HourlyMetricsStates",
                column: "Sessions");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_TotalDurationSeconds",
                table: "HourlyMetricsStates",
                column: "TotalDurationSeconds");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_UniqueVisitors",
                table: "HourlyMetricsStates",
                column: "UniqueVisitors");

            migrationBuilder.CreateIndex(
                name: "IX_PageViewState_ApplicationId",
                table: "PageViewStates",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PageViewState_PagePath",
                table: "PageViewStates",
                column: "PagePath");

            migrationBuilder.CreateIndex(
                name: "IX_PageViewState_PageTitle",
                table: "PageViewStates",
                column: "PageTitle");

            migrationBuilder.CreateIndex(
                name: "IX_PageViewState_ScrollDepthPercent",
                table: "PageViewStates",
                column: "ScrollDepthPercent");

            migrationBuilder.CreateIndex(
                name: "IX_PageViewState_SearchVector",
                table: "PageViewStates",
                column: "SearchVector");

            migrationBuilder.CreateIndex(
                name: "IX_PageViewState_SessionId",
                table: "PageViewStates",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PageViewState_TimeOnPageSeconds",
                table: "PageViewStates",
                column: "TimeOnPageSeconds");

            migrationBuilder.CreateIndex(
                name: "IX_PageViewState_Timestamp",
                table: "PageViewStates",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_PageViewState_VisitorId",
                table: "PageViewStates",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionCohortState_ApplicationId",
                table: "RetentionCohortStates",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionCohortState_CohortWeek",
                table: "RetentionCohortStates",
                column: "CohortWeek");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionCohortState_TotalVisitors",
                table: "RetentionCohortStates",
                column: "TotalVisitors");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_ApplicationId",
                table: "SessionStates",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_DurationSeconds",
                table: "SessionStates",
                column: "DurationSeconds");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_EventCount",
                table: "SessionStates",
                column: "EventCount");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_ExitPage",
                table: "SessionStates",
                column: "ExitPage");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_HasConversion",
                table: "SessionStates",
                column: "HasConversion");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_IsBounce",
                table: "SessionStates",
                column: "IsBounce");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_LandingPage",
                table: "SessionStates",
                column: "LandingPage");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_PageViewCount",
                table: "SessionStates",
                column: "PageViewCount");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_ReferrerDomain",
                table: "SessionStates",
                column: "ReferrerDomain");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_ReferrerUrl",
                table: "SessionStates",
                column: "ReferrerUrl");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_Source",
                table: "SessionStates",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_StartedAt",
                table: "SessionStates",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_UtmCampaign",
                table: "SessionStates",
                column: "UtmCampaign");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_UtmMedium",
                table: "SessionStates",
                column: "UtmMedium");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_UtmSource",
                table: "SessionStates",
                column: "UtmSource");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_VisitorId",
                table: "SessionStates",
                column: "VisitorId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorState_ApplicationId",
                table: "VisitorStates",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorState_City",
                table: "VisitorStates",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorState_Country",
                table: "VisitorStates",
                column: "Country");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorState_FirstSeen",
                table: "VisitorStates",
                column: "FirstSeen");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorState_LastSeen",
                table: "VisitorStates",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorState_TotalPageViews",
                table: "VisitorStates",
                column: "TotalPageViews");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorState_TotalSessions",
                table: "VisitorStates",
                column: "TotalSessions");

            migrationBuilder.CreateIndex(
                name: "IX_VisitorState_UserId",
                table: "VisitorStates",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationStates");

            migrationBuilder.DropTable(
                name: "ConversionStates");

            migrationBuilder.DropTable(
                name: "DailyMetricsStates");

            migrationBuilder.DropTable(
                name: "EventStates");

            migrationBuilder.DropTable(
                name: "FunnelStates");

            migrationBuilder.DropTable(
                name: "GoalStates");

            migrationBuilder.DropTable(
                name: "HourlyMetricsStates");

            migrationBuilder.DropTable(
                name: "PageViewStates");

            migrationBuilder.DropTable(
                name: "RetentionCohortStates");

            migrationBuilder.DropTable(
                name: "SessionStates");

            migrationBuilder.DropTable(
                name: "VisitorStates");
        }
    }
}
