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
        var connectionString = Environment.GetEnvironmentVariable("SEARCH_DB_CONNECTION")
            ?? "Host=localhost;Database=searchdb;Username=postgres;Password=postgres";

        // Parse --connection argument if provided
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--connection")
            {
                connectionString = args[i + 1];
                break;
            }
        }

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsAssembly("TGHarker.Insights.Silo");
        });

        return new SearchDesignTimeContext(optionsBuilder.Options);
    }
}
