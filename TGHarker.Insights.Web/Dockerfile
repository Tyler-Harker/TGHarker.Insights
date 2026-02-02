# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY TGHarker.Insights.slnx ./
COPY TGHarker.Insights.Web/TGHarker.Insights.Web.csproj TGHarker.Insights.Web/
COPY TGHarker.Insights.Abstractions/TGHarker.Insights.Abstractions.csproj TGHarker.Insights.Abstractions/
COPY TGHarker.Insights.ServiceDefaults/TGHarker.Insights.ServiceDefaults.csproj TGHarker.Insights.ServiceDefaults/

# Restore
RUN dotnet restore TGHarker.Insights.Web/TGHarker.Insights.Web.csproj

# Copy everything else
COPY . .

# Build and publish
WORKDIR /src/TGHarker.Insights.Web
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "TGHarker.Insights.Web.dll"]
