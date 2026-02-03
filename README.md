# TGHarker.Insights

A privacy-focused, self-hosted web analytics platform built on Microsoft Orleans for horizontal scalability.

## Features

- **Multi-Application Support**: Track multiple websites from a single dashboard
- **Real-Time Analytics**: Live visitor tracking with sharded architecture
- **Page Analytics**: Views, unique visitors, time on page, bounce rates
- **Traffic Sources**: Referrer tracking, UTM parameters, source categorization
- **Custom Events**: Track any user interaction with category, action, label, value
- **Conversion Goals**: Page visit, event, duration, and pages-per-session goal tracking
- **Funnel Analysis**: Multi-step user journey tracking with drop-off analysis
- **Retention Analysis**: Weekly cohort-based retention tracking
- **User Attributes**: Custom visitor segmentation and filtering
- **Theme Support**: Light and dark mode with user preference persistence
- **Privacy-First**: No third-party data sharing, anonymous visitor IDs

## Architecture

Built on Microsoft Orleans 10.0 for distributed, horizontally scalable analytics processing:

```
┌─────────────────────────────────────────────────────────────────┐
│                        Dashboard UI                              │
│  (Overview, Real-time, Pages, Sources, Events, Conversions,     │
│   Funnels, Settings)                                            │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      ASP.NET Core API                           │
│  (/api/collect, /api/collect/batch, /api/collect/config)        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Orleans Silo Cluster                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   Visitor   │  │   Session   │  │   RealTimeShard (x16)   │  │
│  │   Grains    │  │   Grains    │  │        Grains           │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  PageView   │  │    Event    │  │    HourlyMetrics        │  │
│  │   Grains    │  │   Grains    │  │    Grains (buffered)    │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │   Funnel    │  │   Funnel    │  │     Application         │  │
│  │   Grains    │  │  Analytics  │  │       Grains            │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐  │
│  │  Retention  │  │   Daily     │  │     Conversion          │  │
│  │   Cohort    │  │   Metrics   │  │       Grains            │  │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker (optional, for containerized deployment)

### Running Locally

1. Clone the repository:
```bash
git clone https://github.com/tgharker/TGHarker.Insights.git
cd TGHarker.Insights
```

2. Run with Aspire:
```bash
dotnet run --project TGHarker.Insights.AppHost
```

3. Open the dashboard at `https://localhost:5001`

### Adding Tracking to Your Website

1. Create an application in the dashboard
2. Copy the tracking code from Settings
3. Add to your website's `<head>`:

```html
<script src="https://your-insights-host/sdk/insights.js" data-application="your-app-id"></script>
```

## JavaScript SDK

### Basic Usage

```javascript
// Manual initialization
Insights.init({
    propertyId: 'your-app-id',
    endpoint: 'https://your-insights-host/api/collect',
    autoPageView: true,
    debug: false
});

// Track page views (automatic for SPAs)
Insights.pageview();

// Track custom events
Insights.event('Button', 'Click', 'Sign Up', 1);

// Identify users
Insights.identify('user-123');

// Set custom attributes
Insights.setUserAttributes({
    plan: 'premium',
    company: 'Acme Corp'
});
```

### Event Tracking

```javascript
// Track a purchase
Insights.event('Ecommerce', 'Purchase', 'Product Name', 99.99);

// Track form submission
Insights.event('Form', 'Submit', 'Contact Form');

// Track video playback
Insights.event('Video', 'Play', 'Product Demo');
```

## Dashboard Features

### Overview
High-level metrics including page views, sessions, bounce rate, and average duration with trend charts.

### Real-Time
Live visitor count and active pages, updated in real-time with sharded architecture for scalability.

### Pages
Detailed page performance metrics: views, unique visitors, average time, bounce rate.

### Sources
Traffic source analysis: direct, referral, search, social, with UTM campaign tracking.

### Events
Custom event tracking with category breakdown and action analysis.

### Conversions
Goal tracking for page visits, custom events, session duration, and pages per session with conversion rate analysis.

### Retention
Weekly cohort-based retention analysis showing how users return over time.

### Funnels
Multi-step user journey tracking:
- Define steps as page visits or events
- Visualize conversion between steps
- Identify drop-off points
- Support for wildcards in page paths

### Settings
- Application configuration
- Tracking code generation
- User attribute management (enable/disable filtering)
- Allowed origins configuration for CORS
- Danger zone for application deletion

## Scalability

Designed for horizontal scalability:

- **Sharded Real-Time**: 16 shards per application prevent hot spots
- **Buffered Metrics**: 5-second write buffer reduces persistence load
- **Bounded Collections**: Automatic limits prevent unbounded growth
- **Query Limits**: 10,000 record limit on analytics queries
- **Pre-Computed Aggregates**: Daily funnel analytics for fast queries

## Project Structure

```
TGHarker.Insights/
├── TGHarker.Insights.Abstractions/    # Interfaces, DTOs, Models
│   ├── DTOs/                          # Data transfer objects
│   ├── Grains/                        # Grain interfaces
│   └── Models/                        # State models
├── TGHarker.Insights.Grains/          # Grain implementations
│   └── Analytics/                     # Analytics grains
├── TGHarker.Insights.Web/             # Web application
│   ├── Endpoints/                     # API endpoints
│   ├── Pages/                         # Razor pages
│   │   └── Dashboard/                 # Dashboard pages
│   │       └── Application/           # Per-app pages
│   └── wwwroot/                       # Static files
│       └── sdk/                       # JavaScript SDK
├── TGHarker.Insights.AppHost/         # Aspire host
└── TGHarker.Insights.ServiceDefaults/ # Service configuration
```

## Configuration

### Authentication

The dashboard uses OpenID Connect for authentication. Configure your identity provider in `appsettings.json`:

```json
{
  "Authentication": {
    "Authority": "https://your-identity-provider",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ORLEANS_CLUSTERING` | Clustering provider | `Development` |
| `ORLEANS_PERSISTENCE` | Persistence provider | `Memory` |
| `INSIGHTS_SESSION_TIMEOUT` | Session timeout (minutes) | `30` |

## API Reference

### Collect Endpoints

#### POST /api/collect
Single event collection.

**Headers:**
- `X-API-Key`: Application API key

**Body:**
```json
{
  "type": "pageview",
  "propertyId": "app-id",
  "visitorId": "visitor-id",
  "sessionId": "session-id",
  "timestamp": "2024-01-01T00:00:00Z",
  "data": {
    "path": "/page",
    "title": "Page Title"
  },
  "context": {
    "url": "https://example.com/page",
    "userAgent": "Mozilla/5.0..."
  }
}
```

#### POST /api/collect/batch
Batch event collection.

#### GET /api/collect/config/{applicationId}
Get SDK configuration for an application.

### Analytics Endpoints

#### GET /api/applications/{applicationId}/analytics/overview
Get summary metrics (page views, sessions, bounce rate, duration).

#### GET /api/applications/{applicationId}/analytics/realtime
Get current active visitors and page distribution.

#### GET /api/applications/{applicationId}/analytics/pages
Get top pages by view count.

#### GET /api/applications/{applicationId}/analytics/sources
Get traffic source breakdown.

#### GET /api/applications/{applicationId}/analytics/events
Get custom events grouped by category/action.

#### GET /api/applications/{applicationId}/analytics/conversions
Get conversions grouped by goal.

#### GET /api/applications/{applicationId}/analytics/retention
Get retention cohorts with week-by-week breakdown.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

MIT License - see LICENSE file for details.

## Technology Stack

- **.NET 10** - Runtime and framework
- **Microsoft Orleans 10.0** - Distributed actor framework
- **TGHarker.Orleans.Search** - Queryable grain state
- **PostgreSQL** - Search index storage
- **Azure Tables** - Grain state persistence
- **.NET Aspire** - Development orchestration
- **Bootstrap 5** - UI framework
- **Chart.js** - Charting library

## Acknowledgments

- [Microsoft Orleans](https://github.com/dotnet/orleans) - Distributed actor framework
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) - Cloud-native development
- [Bootstrap](https://getbootstrap.com/) - UI framework
- [Chart.js](https://www.chartjs.org/) - Charting library
