using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Azure Storage for Orleans clustering and grain state
var storage = builder.AddAzureStorage("storage");
var clusteringTable = storage.AddTables("clustering");
var grainStorage = storage.AddBlobs("grainstate");

// PostgreSQL for TGHarker.Orleans.Search
var postgres = builder.AddPostgres("postgres");
var searchDb = postgres.AddDatabase("searchdb-insights");

// Orleans cluster configuration
var orleans = builder.AddOrleans("insights-cluster")
    .WithClustering(clusteringTable)
    .WithGrainStorage("Default-inner", grainStorage);

// Orleans Silo
var silo = builder.AddProject<Projects.TGHarker_Insights_Silo>("silo")
    .WithReference(orleans)
    .WithReference(searchDb)
    .WaitFor(storage)
    .WaitFor(searchDb);

// Identity configuration
// var identityClientSecret = builder.AddParameter("identity-client-secret", secret: true);

// Web API + Dashboard (Orleans Client)
var web = builder.AddProject<Projects.TGHarker_Insights_Web>("web")
    .WithReference(orleans.AsClient())
    .WithReference(searchDb)
    .WaitFor(silo)
    .WithExternalHttpEndpoints()
    .WithEnvironment("Identity__ClientId", "insights");
    // .WithEnvironment("Identity__ClientSecret", identityClientSecret);

// Development-only configuration
if (builder.Environment.IsDevelopment())
{
    storage.RunAsEmulator();

    // pgAdmin for database inspection
    builder.AddContainer("pgadmin", "dpage/pgadmin4")
        .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "admin@admin.com")
        .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "devpassword123")
        .WithEnvironment("PGADMIN_CONFIG_SERVER_MODE", "False")
        .WithHttpEndpoint(port: 5050, targetPort: 80)
        .WaitFor(postgres);
}

builder.Build().Run();
