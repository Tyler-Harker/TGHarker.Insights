using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TGHarker.Insights.Silo.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationIdToAllStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "VisitorStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "SessionStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "RetentionCohortStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "PageViewStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "HourlyMetricsStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "GoalStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "FunnelStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "EventStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "DailyMetricsStates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrganizationId",
                table: "ConversionStates",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VisitorState_OrganizationId",
                table: "VisitorStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_SessionState_OrganizationId",
                table: "SessionStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_RetentionCohortState_OrganizationId",
                table: "RetentionCohortStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_PageViewState_OrganizationId",
                table: "PageViewStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_HourlyMetricsState_OrganizationId",
                table: "HourlyMetricsStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalState_OrganizationId",
                table: "GoalStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FunnelState_OrganizationId",
                table: "FunnelStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_EventState_OrganizationId",
                table: "EventStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyMetricsState_OrganizationId",
                table: "DailyMetricsStates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionState_OrganizationId",
                table: "ConversionStates",
                column: "OrganizationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VisitorState_OrganizationId",
                table: "VisitorStates");

            migrationBuilder.DropIndex(
                name: "IX_SessionState_OrganizationId",
                table: "SessionStates");

            migrationBuilder.DropIndex(
                name: "IX_RetentionCohortState_OrganizationId",
                table: "RetentionCohortStates");

            migrationBuilder.DropIndex(
                name: "IX_PageViewState_OrganizationId",
                table: "PageViewStates");

            migrationBuilder.DropIndex(
                name: "IX_HourlyMetricsState_OrganizationId",
                table: "HourlyMetricsStates");

            migrationBuilder.DropIndex(
                name: "IX_GoalState_OrganizationId",
                table: "GoalStates");

            migrationBuilder.DropIndex(
                name: "IX_FunnelState_OrganizationId",
                table: "FunnelStates");

            migrationBuilder.DropIndex(
                name: "IX_EventState_OrganizationId",
                table: "EventStates");

            migrationBuilder.DropIndex(
                name: "IX_DailyMetricsState_OrganizationId",
                table: "DailyMetricsStates");

            migrationBuilder.DropIndex(
                name: "IX_ConversionState_OrganizationId",
                table: "ConversionStates");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "VisitorStates");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "SessionStates");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "RetentionCohortStates");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "PageViewStates");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "HourlyMetricsStates");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "GoalStates");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "FunnelStates");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "EventStates");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "DailyMetricsStates");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "ConversionStates");
        }
    }
}
