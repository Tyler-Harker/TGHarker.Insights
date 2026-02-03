# TGHarker.Insights - Product Requirements Document

## Executive Summary

TGHarker.Insights is a privacy-focused, self-hosted web analytics platform built on Microsoft Orleans for horizontal scalability. It provides website owners with actionable insights into visitor behavior, traffic sources, conversions, and user journeys without relying on third-party services or compromising user privacy.

## Vision

To provide a powerful, scalable, and privacy-respecting alternative to commercial analytics platforms like Google Analytics, enabling organizations to own their data while gaining deep insights into user behavior.

## Target Users

- **Website Owners**: Small to medium businesses wanting analytics without third-party data sharing
- **Developers**: Teams building applications who need integrated analytics
- **Enterprise Organizations**: Companies with strict data privacy requirements (GDPR, HIPAA)
- **SaaS Providers**: Multi-tenant platforms needing per-application analytics

## Core Features

### 1. Multi-Application Dashboard
- Support for multiple websites/applications under a single account
- Easy switching between applications via dropdown
- Unified dashboard experience across all applications
- Application-specific settings and tracking codes

### 2. Real-Time Analytics
- Live visitor count and active pages
- Real-time page view tracking
- Horizontally scalable using sharded grains (16 shards per application)
- 5-minute visitor timeout with automatic cleanup

### 3. Traffic Analytics
- **Overview Dashboard**: Page views, sessions, bounce rate, average duration
- **Page Performance**: Views, unique visitors, time on page, bounce rate per page
- **Traffic Sources**: Referrer tracking, UTM parameter support, source categorization
- **Geographic Data**: Country and city-level analytics (when available)

### 4. Event Tracking
- Custom event tracking with category, action, label, value
- Event categorization and aggregation
- Real-time event streaming
- Event-based goal conversion tracking

### 5. Conversion Goals
- **Page Visit Goals**: Track when users reach specific pages
- **Event Goals**: Track custom interactions
- **Duration Goals**: Track sessions exceeding target duration
- **Pages Per Session Goals**: Track sessions with target page count
- Goal conversion rate calculation
- Per-goal analytics with trend data

### 6. Funnel Analysis
- Multi-step funnel definition
- Support for page visit and event steps
- Path pattern matching with wildcards
- Conversion rate between steps
- Drop-off analysis and visualization
- Pre-computed daily aggregates for scalability

### 7. User Attributes
- Custom attribute assignment via SDK
- Attribute-based visitor segmentation
- Filterable attribute management
- Enable/disable attributes for filtering
- Real-time attribute tracking

### 8. JavaScript SDK
- Lightweight, privacy-focused tracking script
- Automatic page view tracking for SPAs
- Session management with 30-minute timeout
- History API integration for SPA navigation
- Methods: `init()`, `pageview()`, `event()`, `identify()`, `setUserAttributes()`

### 9. Retention Analysis
- Weekly cohort-based retention tracking
- Week-by-week retention percentages
- Configurable cohort depth (default 8 weeks)
- Visual retention matrix display

### 10. Theme Support
- Light and dark mode support
- User preference persistence
- System theme detection

## Technical Architecture

### Backend Stack
- **Framework**: ASP.NET Core 10 with Razor Pages
- **Distributed Computing**: Microsoft Orleans 10.0 (Actor Model)
- **Search/Query**: TGHarker.Orleans.Search with queryable grain state
- **Data Storage**: PostgreSQL (via Orleans Search), Azure Tables
- **Authentication**: OpenID Connect
- **Hosting**: .NET Aspire for local development and orchestration

### Scalability Design

#### Grain Architecture
| Grain Type | Key Format | Purpose |
|------------|-----------|---------|
| ApplicationGrain | `app-{id}` | Application settings, API keys, allowed origins |
| VisitorGrain | `visitor-{appId}-{visitorId}` | Visitor state, attributes |
| SessionGrain | `session-{appId}-{sessionId}` | Session data, page views, events |
| PageViewGrain | `pv-{appId}-{id}` | Individual page view records |
| EventGrain | `event-{appId}-{id}` | Individual event records |
| ConversionGrain | `conversion-{appId}-{id}` | Individual conversion records |
| GoalGrain | `goal-{appId}-{goalId}` | Goal definitions |
| HourlyMetricsGrain | `metrics-hourly-{appId}-{yyyyMMddHH}` | Aggregated hourly metrics |
| DailyMetricsGrain | `metrics-daily-{appId}-{yyyyMMdd}` | Aggregated daily metrics |
| RealTimeShardGrain | `realtime-shard-{appId}-{0-15}` | Sharded real-time tracking |
| RealTimeCoordinatorGrain | `realtime-coordinator-{appId}` | Aggregates shard data |
| RealTimeGrain | `realtime-{appId}` | Real-time coordinator |
| FunnelGrain | `funnel-{appId}-{id}` | Funnel definitions |
| FunnelAnalyticsGrain | `funnel-analytics-{funnelId}-{yyyyMMdd}` | Daily funnel metrics |
| FunnelSummaryGrain | `funnel-summary-{funnelId}` | Aggregated funnel analytics |
| RetentionCohortGrain | `cohort-{appId}-{cohortWeek}` | Weekly retention cohorts |

#### Scalability Features
- **Sharded Real-Time Tracking**: 16 shards per application to distribute load
- **Buffered Writes**: HourlyMetricsGrain buffers updates for 5 seconds before persisting
- **Bounded Collections**: Max 10,000 unique visitor IDs tracked per hour per grain
- **Query Limits**: Analytics queries limited to 10,000 records to prevent memory issues
- **Pre-Computed Aggregates**: Daily funnel analytics computed incrementally

### Data Flow

```
[Browser] --> [JavaScript SDK] --> [Collect API] --> [Orleans Grains]
                                        |
                                        +--> RealTimeShardGrain (sharded by visitor)
                                        +--> SessionGrain
                                        +--> PageViewGrain / EventGrain
                                        +--> HourlyMetricsGrain (buffered)
                                        +--> VisitorGrain
```

## API Endpoints

### Collect API
- `POST /api/collect` - Single event collection
- `POST /api/collect/batch` - Batch event collection
- `GET /api/collect/config/{applicationId}` - SDK configuration

### Event Types
| Type | Purpose |
|------|---------|
| `session_start` | Begin new session |
| `pageview` | Page view tracking |
| `event` | Custom event |
| `session_end` | End session |
| `identify` | Link visitor to user ID |
| `set_attributes` | Set custom visitor attributes |

## Dashboard Pages

| Page | Route | Purpose |
|------|-------|---------|
| Dashboard Index | `/dashboard` | Application list and selection |
| Overview | `/dashboard/{appId}` | High-level metrics summary |
| Real-time | `/dashboard/{appId}/realtime` | Live visitor tracking |
| Pages | `/dashboard/{appId}/pages` | Page performance analytics |
| Sources | `/dashboard/{appId}/sources` | Traffic source analysis |
| Events | `/dashboard/{appId}/events` | Custom event tracking |
| Conversions | `/dashboard/{appId}/conversions` | Goal tracking |
| Funnels | `/dashboard/{appId}/funnels` | Funnel analysis |
| Settings | `/dashboard/{appId}/settings` | App config, tracking code, attributes, allowed origins |

## Security

- API key authentication for data collection
- OpenID Connect authentication for dashboard
- Organization-based multi-tenancy
- Per-application CORS origin allowlists
- CSRF protection on forms
- No personally identifiable information stored by default
- Visitor IDs are anonymous, randomly generated client-side

## Future Roadmap

### Phase 2
- [ ] Email tracking module
- [ ] API analytics module
- [ ] Custom dashboards and reports
- [ ] Data export (CSV, JSON)
- [ ] Webhooks for goal completions

### Phase 3
- [ ] A/B testing integration
- [ ] Heatmaps and session recordings
- [ ] Advanced segmentation
- [x] Cohort analysis (retention cohorts implemented)
- [ ] Predictive analytics

### Phase 4
- [ ] White-label support
- [ ] Multi-tenant SaaS mode
- [ ] Enterprise SSO integration
- [ ] Advanced data retention policies
- [ ] Real-time alerts and notifications

## Success Metrics

- **Performance**: Dashboard pages load in < 500ms
- **Scalability**: Handle 100,000+ events/second per cluster
- **Reliability**: 99.9% uptime for collection endpoints
- **Accuracy**: < 1% variance in metrics calculations

## Dependencies

- .NET 10
- Microsoft Orleans 10.0
- TGHarker.Orleans.Search 1.0.11
- TGHarker.Orleans.Search.PostgreSQL 1.0.11
- Aspire.Azure.Data.Tables 13.1.0
- Aspire.Npgsql.EntityFrameworkCore.PostgreSQL 13.1.0
- Bootstrap 5
- Chart.js

## Deployment

- Self-hosted via Docker or Kubernetes
- Aspire-based development environment
- Configurable persistence providers (Azure, SQL, etc.)
