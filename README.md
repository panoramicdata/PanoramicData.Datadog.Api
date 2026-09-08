[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

[![NuGet version](https://img.shields.io/nuget/v/PanoramicData.Datadog.Api.svg)](https://www.nuget.org/packages/PanoramicData.Datadog.Api/)

[![Codacy Badge](https://app.codacy.com/project/badge/grade/Datadog.Api)](https://app.codacy.com/gh/panoramicdata/PanoramicData.Datadog.Api/dashboard)

# PanoramicData.Datadog.Api

A .NET API for Datadog

## Unit Tests

To run unit tests, set up your unit test User secrets to match the usersecrets.example.json file.

## Usage

```csharp
using Datadog.Api;

var client = new DatadogClient(new()
{
	ApiKey = "API_KEY",
	ApplicationKey = "APPLICATION_KEY"
});

var users = await client.Users.GetAllAsync();
```

## Filtered requests

Endpoints that accept query filters take a request object:

```csharp
var metrics = await client.Metrics.GetMetricsAsync(
    new GetMetricsRequest
    {
        FilterConfigured = true,
        FilterMetricType = MetricType.Gauge,
        WindowSeconds = 3600
    },
    cancellationToken);
```

The older overloads that took each filter as an optional parameter are obsolete and will be
removed in 3.0. They produce identical requests, so migration is mechanical.
