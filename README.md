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