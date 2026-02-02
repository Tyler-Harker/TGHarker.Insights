using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TGHarker.Insights.Abstractions.Models.Generated;
using TGHarker.Orleans.Search.PostgreSQL;

namespace TGHarker.Insights.Silo;

/// <summary>
/// Design-time factory for creating the search context during EF Core migrations.
/// This overrides the generated factory to implement the correct interface.
/// </summary>
public class SearchDesignTimeContextFactory : IDesignTimeDbContextFactory<SearchDesignTimeContext>
{
    public SearchDesignTimeContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgreSqlSearchContext>();

        // Default connection string for design-time (migrations)
        // Override by setting environment variable or passing --connection argument
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Host=localhost;Database=searchdb;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("TGHarker.Insights.Silo");
        });

        return new SearchDesignTimeContext(optionsBuilder.Options);
    }
}
