# Request Objects for Filtered Endpoints — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the optional filter parameters on the five filtered Refit endpoints with one request object each, keeping the old signatures working but `[Obsolete]`, so callers get a discoverable API and Codacy's `S2360` disappears when the legacy methods are removed in 3.0.

**Architecture:** Each affected interface method gains an overload taking a single sealed request class whose properties carry the *same* `[AliasAs]` names as today's parameters. Refit flattens the object into the query string. Spike results (Appendix A) prove this produces **byte-identical** URIs, so there is no wire change and no server-visible risk. The legacy method stays, marked `[Obsolete]`, and Refit generates both independently — neither delegates to the other.

**Tech Stack:** .NET 10, C# latest, Refit 15.2.0, xUnit v3, AwesomeAssertions, Nerdbank.GitVersioning, central package management.

**Spec:** This document. The design was agreed in conversation on 2026-09-08; the decisions and their evidence are recorded in "Design Decisions" and Appendix A below.

## Global Constraints

- Target framework: `net10.0`. Do not add target frameworks.
- Refit version: `15.2.0`, declared in `Directory.Packages.props`. Do not add package references with inline versions — central package management is on (`ManagePackageVersionsCentrally=true`).
- `TreatWarningsAsErrors=true` for every project (`Directory.Build.props`). A warning fails the build.
- `Nullable=enable`, `ImplicitUsings=enable`, `GenerateDocumentationFile=true`.
- **Indentation is tabs**, not spaces. File-scoped namespaces. Existing files carry a UTF-8 BOM — preserve it.
- Request classes are `sealed`, use `{ get; init; }`, and use `required` for parameters that have no default today.
- **`[AliasAs]` strings must match the legacy parameter aliases character-for-character.** These are Datadog's bracketed query names (`filter[timestamp][start]`, `page[number]`). A typo silently changes the request.
- **Non-nullable parameters that carry a default today must stay non-nullable with the same default** on the request object (e.g. `int Limit { get; init; } = 500;`). They are always sent on the wire; making them nullable would silently start omitting them.
- Do not change `DatadogClient`, `AuthenticatedHttpClientHandler`, or any response model.
- Do not delete any legacy method in this plan. Removal is a separate 3.0 change.

## Design Decisions

**Request objects, not mandatory parameters.** Making the filters mandatory was considered and rejected: every existing call site passes *none* of them (`Client.Metrics.GetMetricsAsync(cancellationToken: CancellationToken)`, `Client.Teams.GetAsync(cancellationToken: CancellationToken)`), so mandatory filters would turn the dominant usage into `GetMetricsAsync(null, null, null, null, null, null, null, ct)` and put an 8-parameter method at risk of Sonar `S107`.

**No generics.** `GetThingAsync<TIn, TOut>(IRequestDto<TIn>, ct)` was considered and rejected. Refit's docs state that an *unconstrained* generic parameter "still falls back to the reflection request builder, because the concrete type — and its bound properties — are only known at call time", which forfeits the compile-time source generator and hurts trimming/AOT for consumers. It is also *less* type-safe: nothing would tie `TIn` to `TOut`.

**Scope is 3 interfaces, not the library.** 10 of the 13 interfaces already have zero optional parameters and a mandatory `CancellationToken`; that is the house convention this plan restores.

**Codacy will keep reporting `S2360` until 3.0.** The rule fires on the declaration, and the legacy declarations deliberately survive. `.codacy.yml` cannot disable patterns (`exclude_rules` is not a supported key). Suppression for the deprecation window must be done on the Codacy **Code patterns** page — see Task 9.

## Prerequisite (outside this plan)

**CI-13 must be fixed before any release.** nuget.org is on 2.0.25 with 2.0.27 tagged; the `publish` job fails at `NuGet/login@v1` with `No matching trust policy owned by user 'david_n_m_bond' was found`. Nothing here can ship until the nuget.org trusted-publishing policy exists.

---

## File Structure

**Create — request objects (one file per class, matching the existing one-type-per-file convention):**

| File | Responsibility |
| --- | --- |
| `Datadog.Api/Models/Metrics/GetActiveMetricsRequest.cs` | Query for `GET /v1/metrics` |
| `Datadog.Api/Models/Metrics/GetMetricsRequest.cs` | Query for `GET /v2/metrics` |
| `Datadog.Api/Models/Teams/GetTeamsRequest.cs` | Query for `GET /v2/team` |
| `Datadog.Api/Models/Usage/GetHourlyUsageRequest.cs` | Query for `GET /v2/usage/hourly_usage` |
| `Datadog.Api/Models/Usage/GetHourlyUsageAttributionRequest.cs` | Query for `GET /v1/usage/hourly-attribution` |

**Create — tests:**

| File | Responsibility |
| --- | --- |
| `Datadog.Api.Test/RequestUriTestHarness.cs` | Capturing `HttpMessageHandler` + `CaptureUriAsync` helper. No credentials needed. |
| `Datadog.Api.Test/MetricsRequestUriTests.cs` | URI equivalence for `IMetrics` |
| `Datadog.Api.Test/TeamsRequestUriTests.cs` | URI equivalence for `ITeams` |
| `Datadog.Api.Test/UsageRequestUriTests.cs` | URI equivalence for `IUsage` |

**Modify:**

| File | Change |
| --- | --- |
| `Datadog.Api/Interfaces/IMetrics.cs` | 2 new overloads; `[Obsolete]` on 2 legacy methods; mandatory `CancellationToken` on 2 others |
| `Datadog.Api/Interfaces/ITeams.cs` | 1 new overload; `[Obsolete]` on legacy |
| `Datadog.Api/Interfaces/IUsage.cs` | 2 new overloads; `[Obsolete]` on 2 legacy |
| `Datadog.Api.Test/MetricTests.cs` | Migrate to new overloads |
| `Datadog.Api.Test/TeamsTests.cs` | Migrate to new overloads |
| `Datadog.Api.Test/UsageTests.cs` | Migrate to new overloads |
| `version.json` | `"2.0"` → `"2.1"` |
| `Datadog.Api/Datadog.Api.csproj` | `PackageReleaseNotes` |
| `README.md` | Document the request-object style |

**Why a new test file per area:** these are the repository's first credential-free tests. Keeping them separate from the live-API tests (`MetricTests` etc.) means CI can run them without secrets.

---

### Task 1: Credential-free URI capture harness

The whole plan rests on "the new overload produces the same URI as the old one". This task builds the instrument that proves it, and validates the instrument against an endpoint nobody is changing.

**Files:**
- Create: `Datadog.Api.Test/RequestUriTestHarness.cs`
- Test: `Datadog.Api.Test/RequestUriTestHarness.cs` (self-validating test included)

**Interfaces:**
- Consumes: nothing.
- Produces: `RequestUriTestHarness.CaptureUriAsync<T>(Func<T, Task> call)` returning `Task<string>` — the unescaped `PathAndQuery` of the single request the call made. Used by Tasks 2–6.

- [ ] **Step 1: Write the failing test**

Create `Datadog.Api.Test/RequestUriTestHarness.cs`:

```csharp
using Datadog.Api.Interfaces;
using Refit;
using System.Net;

namespace Datadog.Api.Test;

/// <summary>
/// Builds a Refit client over a stub handler that records the outgoing request URI instead of
/// calling Datadog. These tests need no API key, so they run in CI.
/// </summary>
public static class RequestUriTestHarness
{
	private sealed class CapturingHandler : HttpMessageHandler
	{
		public string? Uri { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Uri = System.Uri.UnescapeDataString(request.RequestUri!.PathAndQuery);

			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
			});
		}
	}

	/// <summary>
	/// Invokes <paramref name="call"/> against a stubbed <typeparamref name="T"/> and returns the
	/// URI it produced. Deserialisation of the stub response is irrelevant and its failure is
	/// ignored: the URI is recorded before any response is read.
	/// </summary>
	public static async Task<string> CaptureUriAsync<T>(Func<T, Task> call) where T : class
	{
		var handler = new CapturingHandler();
		using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.datadoghq.com") };
		var api = RestService.For<T>(httpClient);

		try
		{
			await call(api);
		}
		catch
		{
			// Only the URI matters here; the stub response is deliberately not a valid payload.
		}

		return handler.Uri!;
	}
}

public class RequestUriTestHarnessTests
{
	/// <summary>
	/// Validates the harness itself against an endpoint with no parameters, so a failure here
	/// means the harness is broken rather than an endpoint being wrong.
	/// </summary>
	[Fact]
	public async Task CaptureUriAsync_ParameterlessEndpoint_ReturnsPath()
	{
		var uri = await RequestUriTestHarness.CaptureUriAsync<IHosts>(
			api => api.GetAsync(TestContext.Current.CancellationToken));

		uri.Should().Be("/v1/hosts");
	}
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~RequestUriTestHarnessTests"`

Expected: the build fails first (the file is new but the assertion is already correct, so the *first* run should actually PASS). If it fails, the harness is wrong — fix before continuing. This task is the exception to red-green: it builds the instrument.

- [ ] **Step 3: Run the whole credential-free suite**

Run: `dotnet build Datadog.Api.slnx -v:minimal`

Expected: `0 Warning(s) 0 Error(s)`. A `catch { }` with no filter is used above; the same pattern already exists in `XunitLoggerProvider.cs`, so it does not trip analysis.

- [ ] **Step 4: Commit**

```bash
git add Datadog.Api.Test/RequestUriTestHarness.cs
git commit -m "test: add credential-free request URI capture harness"
```

---

### Task 2: `IMetrics.GetActiveAsync` request object

**Files:**
- Create: `Datadog.Api/Models/Metrics/GetActiveMetricsRequest.cs`
- Create: `Datadog.Api.Test/MetricsRequestUriTests.cs`
- Modify: `Datadog.Api/Interfaces/IMetrics.cs:20-25`

**Interfaces:**
- Consumes: `RequestUriTestHarness.CaptureUriAsync<T>` (Task 1).
- Produces: `Datadog.Api.Models.Metrics.GetActiveMetricsRequest` with `required long From`, `string? Host`, `string? TagFilter`; and `IMetrics.GetActiveAsync(GetActiveMetricsRequest, CancellationToken)`.

- [ ] **Step 1: Write the failing test**

Create `Datadog.Api.Test/MetricsRequestUriTests.cs`:

```csharp
using Datadog.Api.Interfaces;
using Datadog.Api.Models.Metrics;

namespace Datadog.Api.Test;

#pragma warning disable CS0618 // legacy overloads are deliberately exercised for equivalence

public class MetricsRequestUriTests
{
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

	[Fact]
	public async Task GetActive_RequestObject_MatchesLegacyUri()
	{
		var viaObject = await RequestUriTestHarness.CaptureUriAsync<IMetrics>(
			api => api.GetActiveAsync(
				new GetActiveMetricsRequest { From = 1_757_000_000, Host = "web-01", TagFilter = "env:prod" },
				CancellationToken));

		var viaLegacy = await RequestUriTestHarness.CaptureUriAsync<IMetrics>(
			api => api.GetActiveAsync(1_757_000_000, "web-01", "env:prod", CancellationToken));

		viaObject.Should().Be("/v1/metrics?from=1757000000&host=web-01&tag_filter=env:prod");
		viaObject.Should().Be(viaLegacy);
	}

	[Fact]
	public async Task GetActive_RequestObject_OmitsUnsetFilters()
	{
		var viaObject = await RequestUriTestHarness.CaptureUriAsync<IMetrics>(
			api => api.GetActiveAsync(new GetActiveMetricsRequest { From = 1_757_000_000 }, CancellationToken));

		var viaLegacy = await RequestUriTestHarness.CaptureUriAsync<IMetrics>(
			api => api.GetActiveAsync(1_757_000_000, null, null, CancellationToken));

		viaObject.Should().Be("/v1/metrics?from=1757000000");
		viaObject.Should().Be(viaLegacy);
	}
}

#pragma warning restore CS0618
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~MetricsRequestUriTests"`

Expected: FAIL — compile error, `GetActiveMetricsRequest` could not be found.

- [ ] **Step 3: Write the request object**

Create `Datadog.Api/Models/Metrics/GetActiveMetricsRequest.cs`:

```csharp
using Refit;

namespace Datadog.Api.Models.Metrics;

/// <summary>
/// Query parameters for retrieving the active metrics list.
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-active-metrics-list"/>
public sealed class GetActiveMetricsRequest
{
	/// <summary>
	/// Seconds since the Unix epoch.
	/// </summary>
	[AliasAs("from")]
	public required long From { get; init; }

	/// <summary>
	/// Hostname for filtering the list of metrics returned. If set, metrics retrieved are those
	/// with the corresponding hostname tag.
	/// </summary>
	[AliasAs("host")]
	public string? Host { get; init; }

	/// <summary>
	/// Filter metrics that have been submitted with the given tags. Supports boolean and wildcard
	/// expressions. Cannot be combined with other filters.
	/// </summary>
	[AliasAs("tag_filter")]
	public string? TagFilter { get; init; }
}
```

- [ ] **Step 4: Add the overload and deprecate the legacy method**

In `Datadog.Api/Interfaces/IMetrics.cs`, add the new overload immediately above the existing `GetActiveAsync`, then add `[Obsolete]` to the existing one:

```csharp
	/// <summary>
	/// Get Metrics
	/// </summary>
	/// <param name="request">The query parameters.</param>
	/// <param name="cancellationToken">The cancellation token</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-active-metrics-list"/>
	[Get("/v1/metrics")]
	Task<ActiveMetricsResponse> GetActiveAsync(
		GetActiveMetricsRequest request,
		CancellationToken cancellationToken);

	/// <summary>
	/// Get Metrics
	/// </summary>
	/// <param name="from">Seconds since the Unix epoch.</param>
	/// <param name="host">Optional: Hostname for filtering the list of metrics returned. If set, metrics retrieved are those with the corresponding hostname tag.</param>
	/// <param name="tagFilter">Filter metrics that have been submitted with the given tags. Supports boolean and wildcard expressions. Cannot be combined with other filters.</param>
	/// <param name="cancellationToken">The cancellation token</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-active-metrics-list"/>
	[Obsolete("Use the GetActiveMetricsRequest overload. This method will be removed in 3.0.")]
	[Get("/v1/metrics")]
	Task<ActiveMetricsResponse> GetActiveAsync(
		[AliasAs("from")] long from,
		[AliasAs("host")] string? host = null,
		[AliasAs("tag_filter")] string? tagFilter = null,
		CancellationToken cancellationToken = default);
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~MetricsRequestUriTests"`

Expected: PASS, 2 tests. Then `dotnet build Datadog.Api.slnx -v:minimal` must report `0 Warning(s) 0 Error(s)` — if `MetricTests.cs` now emits CS0618, do **not** fix it here; Task 8 migrates it. If the build fails on that, add `#pragma warning disable CS0618` at the top of `MetricTests.cs` and note it for Task 8.

- [ ] **Step 6: Commit**

```bash
git add Datadog.Api/Models/Metrics/GetActiveMetricsRequest.cs Datadog.Api/Interfaces/IMetrics.cs Datadog.Api.Test/MetricsRequestUriTests.cs
git commit -m "feat: add GetActiveMetricsRequest overload to IMetrics"
```

---

### Task 3: `IMetrics.GetMetricsAsync` request object

This is the worst offender — 7 optional filters, 10 of the 28 Codacy findings.

**Files:**
- Create: `Datadog.Api/Models/Metrics/GetMetricsRequest.cs`
- Modify: `Datadog.Api/Interfaces/IMetrics.cs` (the `GetMetricsAsync` block)
- Modify: `Datadog.Api.Test/MetricsRequestUriTests.cs`

**Interfaces:**
- Consumes: `RequestUriTestHarness.CaptureUriAsync<T>` (Task 1); `MetricType` from `Datadog.Api.Models.Metrics`.
- Produces: `GetMetricsRequest`; `IMetrics.GetMetricsAsync(GetMetricsRequest, CancellationToken)`.

- [ ] **Step 1: Write the failing test**

Append to `MetricsRequestUriTests.cs`, inside the class:

```csharp
	[Fact]
	public async Task GetMetrics_RequestObject_MatchesLegacyUri()
	{
		var viaObject = await RequestUriTestHarness.CaptureUriAsync<IMetrics>(
			api => api.GetMetricsAsync(
				new GetMetricsRequest
				{
					FilterConfigured = true,
					FilterTagsConfigured = "env",
					FilterMetricType = MetricType.Gauge,
					FilterIncludePercentiles = false,
					FilterQueried = true,
					FilterTags = "env:prod",
					WindowSeconds = 3600
				},
				CancellationToken));

		var viaLegacy = await RequestUriTestHarness.CaptureUriAsync<IMetrics>(
			api => api.GetMetricsAsync(true, "env", MetricType.Gauge, false, true, "env:prod", 3600, CancellationToken));

		viaObject.Should().Be(
			"/v2/metrics?filter[configured]=True&filter[tags_configured]=env&filter[metric_type]=gauge" +
			"&filter[include_percentiles]=False&filter[queried]=True&filter[tags]=env:prod&window[seconds]=3600");
		viaObject.Should().Be(viaLegacy);
	}

	[Fact]
	public async Task GetMetrics_EmptyRequestObject_SendsNoQuery()
	{
		var viaObject = await RequestUriTestHarness.CaptureUriAsync<IMetrics>(
			api => api.GetMetricsAsync(new GetMetricsRequest(), CancellationToken));

		var viaLegacy = await RequestUriTestHarness.CaptureUriAsync<IMetrics>(
			api => api.GetMetricsAsync(cancellationToken: CancellationToken));

		viaObject.Should().Be("/v2/metrics");
		viaObject.Should().Be(viaLegacy);
	}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~MetricsRequestUriTests"`

Expected: FAIL — compile error, `GetMetricsRequest` could not be found.

- [ ] **Step 3: Write the request object**

Create `Datadog.Api/Models/Metrics/GetMetricsRequest.cs`:

```csharp
using Refit;

namespace Datadog.Api.Models.Metrics;

/// <summary>
/// Query parameters for listing metrics that can be configured in the Metrics Summary page or
/// with Metrics without Limits.
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-a-list-of-metrics"/>
public sealed class GetMetricsRequest
{
	/// <summary>
	/// Filter custom metrics that have configured tags.
	/// </summary>
	[AliasAs("filter[configured]")]
	public bool? FilterConfigured { get; init; }

	/// <summary>
	/// Filter tag configurations by configured tags.
	/// </summary>
	[AliasAs("filter[tags_configured]")]
	public string? FilterTagsConfigured { get; init; }

	/// <summary>
	/// Filter metrics by metric type.
	/// </summary>
	[AliasAs("filter[metric_type]")]
	public MetricType? FilterMetricType { get; init; }

	/// <summary>
	/// Filter distributions with additional percentile aggregations enabled or disabled.
	/// </summary>
	[AliasAs("filter[include_percentiles]")]
	public bool? FilterIncludePercentiles { get; init; }

	/// <summary>
	/// (Beta) Filter custom metrics that have or have not been queried in the specified
	/// window[seconds]. If no window is provided or the window is less than 2 hours, a default of
	/// 2 hours will be applied.
	/// </summary>
	[AliasAs("filter[queried]")]
	public bool? FilterQueried { get; init; }

	/// <summary>
	/// Filter metrics that have been submitted with the given tags. Supports boolean and wildcard
	/// expressions. Can only be combined with the filter[queried] filter.
	/// </summary>
	[AliasAs("filter[tags]")]
	public string? FilterTags { get; init; }

	/// <summary>
	/// The number of seconds of look back (from now) to apply to a filter[tag] or filter[queried]
	/// query. Default value is 3600 (1 hour), maximum value is 2,592,000 (30 days).
	/// </summary>
	[AliasAs("window[seconds]")]
	public int? WindowSeconds { get; init; }
}
```

- [ ] **Step 4: Add the overload and deprecate the legacy method**

In `Datadog.Api/Interfaces/IMetrics.cs`, add above the existing `GetMetricsAsync`:

```csharp
	/// <summary>
	/// Get a list of metrics.
	/// Returns all metrics that can be configured in the Metrics Summary page or with Metrics without Limits™ (matching additional filters if specified). This endpoint requires the metrics_read authorization scope.
	/// </summary>
	/// <param name="request">The query parameters.</param>
	/// <param name="cancellationToken">The cancellation token</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-a-list-of-metrics"/>
	[Get("/v2/metrics")]
	Task<MetricsResponse> GetMetricsAsync(
		GetMetricsRequest request,
		CancellationToken cancellationToken);
```

and add `[Obsolete("Use the GetMetricsRequest overload. This method will be removed in 3.0.")]` directly above the existing `[Get("/v2/metrics")]` on the legacy method. Leave the legacy signature otherwise untouched.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~MetricsRequestUriTests"`

Expected: PASS, 4 tests.

- [ ] **Step 6: Commit**

```bash
git add Datadog.Api/Models/Metrics/GetMetricsRequest.cs Datadog.Api/Interfaces/IMetrics.cs Datadog.Api.Test/MetricsRequestUriTests.cs
git commit -m "feat: add GetMetricsRequest overload to IMetrics"
```

---

### Task 4: `ITeams.GetAsync` request object

**Files:**
- Create: `Datadog.Api/Models/Teams/GetTeamsRequest.cs`
- Create: `Datadog.Api.Test/TeamsRequestUriTests.cs`
- Modify: `Datadog.Api/Interfaces/ITeams.cs:9-18`

**Interfaces:**
- Consumes: `RequestUriTestHarness.CaptureUriAsync<T>` (Task 1).
- Produces: `Datadog.Api.Models.Teams.GetTeamsRequest`; `ITeams.GetAsync(GetTeamsRequest, CancellationToken)`.

Note `Fields` keeps type `ICollection<string>?` — the legacy parameter's type — so the comma-joining behaviour is unchanged.

- [ ] **Step 1: Write the failing test**

Create `Datadog.Api.Test/TeamsRequestUriTests.cs`:

```csharp
using Datadog.Api.Interfaces;
using Datadog.Api.Models.Teams;

namespace Datadog.Api.Test;

#pragma warning disable CS0618 // legacy overloads are deliberately exercised for equivalence

public class TeamsRequestUriTests
{
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

	[Fact]
	public async Task Get_RequestObject_MatchesLegacyUri()
	{
		string[] fields = ["name", "handle"];

		var viaObject = await RequestUriTestHarness.CaptureUriAsync<ITeams>(
			api => api.GetAsync(
				new GetTeamsRequest
				{
					Page = 2,
					PageSize = 50,
					Sort = "name",
					FilterKeyword = "platform",
					FilterMe = true,
					Fields = fields
				},
				CancellationToken));

		var viaLegacy = await RequestUriTestHarness.CaptureUriAsync<ITeams>(
			api => api.GetAsync(2, 50, "name", "platform", true, fields, CancellationToken));

		viaObject.Should().Be(viaLegacy);
		viaObject.Should().Contain("page[number]=2").And.Contain("fields[team]=name,handle");
	}

	[Fact]
	public async Task Get_EmptyRequestObject_SendsNoQuery()
	{
		var viaObject = await RequestUriTestHarness.CaptureUriAsync<ITeams>(
			api => api.GetAsync(new GetTeamsRequest(), CancellationToken));

		var viaLegacy = await RequestUriTestHarness.CaptureUriAsync<ITeams>(
			api => api.GetAsync(cancellationToken: CancellationToken));

		viaObject.Should().Be("/v2/team");
		viaObject.Should().Be(viaLegacy);
	}
}

#pragma warning restore CS0618
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~TeamsRequestUriTests"`

Expected: FAIL — compile error, `GetTeamsRequest` could not be found.

- [ ] **Step 3: Write the request object**

Create `Datadog.Api/Models/Teams/GetTeamsRequest.cs`:

```csharp
using Refit;

namespace Datadog.Api.Models.Teams;

/// <summary>
/// Query parameters for listing teams.
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/teams/"/>
public sealed class GetTeamsRequest
{
	/// <summary>
	/// Specific page number to return.
	/// </summary>
	[AliasAs("page[number]")]
	public int? Page { get; init; }

	/// <summary>
	/// Size for a given page.
	/// </summary>
	[AliasAs("page[size]")]
	public int? PageSize { get; init; }

	/// <summary>
	/// Specifies the order of the returned teams.
	/// </summary>
	[AliasAs("sort")]
	public string? Sort { get; init; }

	/// <summary>
	/// Search query; matches against team name, handle and description.
	/// </summary>
	[AliasAs("filter[keyword]")]
	public string? FilterKeyword { get; init; }

	/// <summary>
	/// When true, only returns teams the current user belongs to.
	/// </summary>
	[AliasAs("filter[me]")]
	public bool? FilterMe { get; init; }

	/// <summary>
	/// Team fields to include in the response.
	/// </summary>
	[AliasAs("fields[team]")]
	public ICollection<string>? Fields { get; init; }
}
```

- [ ] **Step 4: Add the overload and deprecate the legacy method**

Rewrite `Datadog.Api/Interfaces/ITeams.cs` as:

```csharp
using Datadog.Api.Models;
using Datadog.Api.Models.Teams;
using Refit;

namespace Datadog.Api.Interfaces;

public interface ITeams
{
	[Get("/v2/team")]
	Task<GuidIdentifiedResponse<Team>> GetAsync(
		GetTeamsRequest request,
		CancellationToken cancellationToken);

	[Obsolete("Use the GetTeamsRequest overload. This method will be removed in 3.0.")]
	[Get("/v2/team")]
	Task<GuidIdentifiedResponse<Team>> GetAsync(
		[AliasAs("page[number]")] int? page = null,
		[AliasAs("page[size]")] int? pageSize = null,
		[AliasAs("sort")] string? sort = null,
		[AliasAs("filter[keyword]")] string? filterKeyword = null,
		[AliasAs("filter[me]")] bool? filterMe = null,
		[AliasAs("fields[team]")] ICollection<string>? fields = null,
		CancellationToken cancellationToken = default
		);
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~TeamsRequestUriTests"`

Expected: PASS, 2 tests.

- [ ] **Step 6: Commit**

```bash
git add Datadog.Api/Models/Teams/GetTeamsRequest.cs Datadog.Api/Interfaces/ITeams.cs Datadog.Api.Test/TeamsRequestUriTests.cs
git commit -m "feat: add GetTeamsRequest overload to ITeams"
```

---

### Task 5: `IUsage.GetHourlyUsageAsync` request object

**Careful:** `includeDescendants`, `includeBreakdown` and `limit` are **non-nullable with defaults** today, so they are *always* sent (`filter[include_descendants]=False`, `page[limit]=500`). The request object must keep them non-nullable with the same defaults or the query silently loses parameters. Appendix A confirms this preserves the URI exactly.

**Files:**
- Create: `Datadog.Api/Models/Usage/GetHourlyUsageRequest.cs`
- Create: `Datadog.Api.Test/UsageRequestUriTests.cs`
- Modify: `Datadog.Api/Interfaces/IUsage.cs:27-37`

**Interfaces:**
- Consumes: `RequestUriTestHarness.CaptureUriAsync<T>` (Task 1); `ProductFamily` from `Datadog.Api.Models.Usage`.
- Produces: `GetHourlyUsageRequest`; `IUsage.GetHourlyUsageAsync(GetHourlyUsageRequest, CancellationToken)`.

- [ ] **Step 1: Write the failing test**

Create `Datadog.Api.Test/UsageRequestUriTests.cs`:

```csharp
using Datadog.Api.Interfaces;
using Datadog.Api.Models.Usage;

namespace Datadog.Api.Test;

#pragma warning disable CS0618 // legacy overloads are deliberately exercised for equivalence

public class UsageRequestUriTests
{
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

	[Fact]
	public async Task GetHourlyUsage_RequestObjectDefaults_MatchesLegacyUri()
	{
		ProductFamily[] families = [ProductFamily.AnalyzedLogs];

		var viaObject = await RequestUriTestHarness.CaptureUriAsync<IUsage>(
			api => api.GetHourlyUsageAsync(
				new GetHourlyUsageRequest
				{
					StartHour = "2026-09-01T00",
					EndHour = "2026-09-02T00",
					ProductFamilies = families
				},
				CancellationToken));

		var viaLegacy = await RequestUriTestHarness.CaptureUriAsync<IUsage>(
			api => api.GetHourlyUsageAsync("2026-09-01T00", "2026-09-02T00", families, cancellationToken: CancellationToken));

		viaObject.Should().Be(viaLegacy);

		// The defaulted, non-nullable parameters must still be on the wire.
		viaObject.Should().Contain("page[limit]=500").And.Contain("filter[include_descendants]=False");
	}

	[Fact]
	public async Task GetHourlyUsage_RequestObjectAllSet_MatchesLegacyUri()
	{
		ProductFamily[] families = [ProductFamily.All];

		var viaObject = await RequestUriTestHarness.CaptureUriAsync<IUsage>(
			api => api.GetHourlyUsageAsync(
				new GetHourlyUsageRequest
				{
					StartHour = "2026-09-01T00",
					EndHour = "2026-09-02T00",
					ProductFamilies = families,
					IncludeDescendants = true,
					IncludeBreakdown = true,
					Versions = "logs:v2",
					Limit = 100,
					NextRecordId = "abc"
				},
				CancellationToken));

		var viaLegacy = await RequestUriTestHarness.CaptureUriAsync<IUsage>(
			api => api.GetHourlyUsageAsync("2026-09-01T00", "2026-09-02T00", families, true, true, "logs:v2", 100, "abc", CancellationToken));

		viaObject.Should().Be(viaLegacy);
	}
}

#pragma warning restore CS0618
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~UsageRequestUriTests"`

Expected: FAIL — compile error, `GetHourlyUsageRequest` could not be found.

- [ ] **Step 3: Write the request object**

Create `Datadog.Api/Models/Usage/GetHourlyUsageRequest.cs`:

```csharp
using Refit;

namespace Datadog.Api.Models.Usage;

/// <summary>
/// Query parameters for retrieving hourly usage by product family.
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/?code-lang=curl#get-hourly-usage-by-product-family"/>
public sealed class GetHourlyUsageRequest
{
	/// <summary>
	/// Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage beginning at this hour.
	/// </summary>
	[AliasAs("filter[timestamp][start]")]
	public required string StartHour { get; init; }

	/// <summary>
	/// Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage ending before this hour.
	/// </summary>
	[AliasAs("filter[timestamp][end]")]
	public required string EndHour { get; init; }

	/// <summary>
	/// Product families to retrieve.
	/// </summary>
	[AliasAs("filter[product_families]")]
	public required IReadOnlyCollection<ProductFamily> ProductFamilies { get; init; }

	/// <summary>
	/// Include child organization usage in the response. Defaults to false.
	/// </summary>
	[AliasAs("filter[include_descendants]")]
	public bool IncludeDescendants { get; init; }

	/// <summary>
	/// Include breakdown of usage by subcategories where applicable (for product family logs only). Defaults to false.
	/// </summary>
	[AliasAs("filter[include_breakdown]")]
	public bool IncludeBreakdown { get; init; }

	/// <summary>
	/// Comma separated list of product family versions to use in the format product_family:version.
	/// </summary>
	[AliasAs("filter[versions]")]
	public string? Versions { get; init; }

	/// <summary>
	/// Maximum number of results to return (between 1 and 500).
	/// </summary>
	[AliasAs("page[limit]")]
	public int Limit { get; init; } = 500;

	/// <summary>
	/// List following results with a next_record_id provided in the previous query.
	/// </summary>
	[AliasAs("page[next_record_id]")]
	public string? NextRecordId { get; init; }
}
```

- [ ] **Step 4: Add the overload and deprecate the legacy method**

In `Datadog.Api/Interfaces/IUsage.cs`, add above the existing `GetHourlyUsageAsync`:

```csharp
	/// <summary>
	/// Get hourly usage by product family.
	/// This endpoint requires the usage_read authorization scope.
	/// </summary>
	/// <param name="request">The query parameters.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/?code-lang=curl#get-hourly-usage-by-product-family"/>
	[Get("/v2/usage/hourly_usage")]
	Task<StringIdentifiedResponse<HourlyUsage>> GetHourlyUsageAsync(
		GetHourlyUsageRequest request,
		CancellationToken cancellationToken);
```

and add `[Obsolete("Use the GetHourlyUsageRequest overload. This method will be removed in 3.0.")]` directly above the legacy method's `[Get("/v2/usage/hourly_usage")]`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~UsageRequestUriTests"`

Expected: PASS, 2 tests.

- [ ] **Step 6: Commit**

```bash
git add Datadog.Api/Models/Usage/GetHourlyUsageRequest.cs Datadog.Api/Interfaces/IUsage.cs Datadog.Api.Test/UsageRequestUriTests.cs
git commit -m "feat: add GetHourlyUsageRequest overload to IUsage"
```

---

### Task 6: `IUsage.GetHourlyUsageAttributionAsync` request object

**Careful:** `usageType` defaults to `UsageType.ApiUsage` and `includeDescendants` to `true` — both always sent. `[EnumMember]` aliasing is honoured on request-object properties (Appendix A), so `usage_type=api_usage` is preserved.

**Files:**
- Create: `Datadog.Api/Models/Usage/GetHourlyUsageAttributionRequest.cs`
- Modify: `Datadog.Api/Interfaces/IUsage.cs` (the `GetHourlyUsageAttributionAsync` block)
- Modify: `Datadog.Api.Test/UsageRequestUriTests.cs`

**Interfaces:**
- Consumes: `RequestUriTestHarness.CaptureUriAsync<T>` (Task 1); `UsageType` from `Datadog.Api.Models.Usage`.
- Produces: `GetHourlyUsageAttributionRequest`; `IUsage.GetHourlyUsageAttributionAsync(GetHourlyUsageAttributionRequest, CancellationToken)`.

- [ ] **Step 1: Write the failing test**

Append to `UsageRequestUriTests.cs`, inside the class:

```csharp
	[Fact]
	public async Task GetHourlyUsageAttribution_RequestObjectDefaults_MatchesLegacyUri()
	{
		var viaObject = await RequestUriTestHarness.CaptureUriAsync<IUsage>(
			api => api.GetHourlyUsageAttributionAsync(
				new GetHourlyUsageAttributionRequest { StartHour = "2026-09-01T00" },
				CancellationToken));

		var viaLegacy = await RequestUriTestHarness.CaptureUriAsync<IUsage>(
			api => api.GetHourlyUsageAttributionAsync("2026-09-01T00", cancellationToken: CancellationToken));

		viaObject.Should().Be(viaLegacy);

		// [EnumMember] aliasing and the defaulted bool must both survive.
		viaObject.Should().Contain("usage_type=api_usage").And.Contain("include_descendants=True");
	}

	[Fact]
	public async Task GetHourlyUsageAttribution_RequestObjectAllSet_MatchesLegacyUri()
	{
		var viaObject = await RequestUriTestHarness.CaptureUriAsync<IUsage>(
			api => api.GetHourlyUsageAttributionAsync(
				new GetHourlyUsageAttributionRequest
				{
					StartHour = "2026-09-01T00",
					EndHour = "2026-09-02T00",
					UsageType = UsageType.All,
					NextRecordId = "abc",
					TagBreakdownKeys = "team",
					IncludeDescendants = false
				},
				CancellationToken));

		var viaLegacy = await RequestUriTestHarness.CaptureUriAsync<IUsage>(
			api => api.GetHourlyUsageAttributionAsync("2026-09-01T00", "2026-09-02T00", UsageType.All, "abc", "team", false, CancellationToken));

		viaObject.Should().Be(viaLegacy);
	}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~UsageRequestUriTests"`

Expected: FAIL — compile error, `GetHourlyUsageAttributionRequest` could not be found.

- [ ] **Step 3: Write the request object**

Create `Datadog.Api/Models/Usage/GetHourlyUsageAttributionRequest.cs`:

```csharp
using Refit;

namespace Datadog.Api.Models.Usage;

/// <summary>
/// Query parameters for retrieving hourly usage attribution.
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/?code-lang=curl#get-hourly-usage-attribution"/>
public sealed class GetHourlyUsageAttributionRequest
{
	/// <summary>
	/// Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage beginning at this hour.
	/// </summary>
	[AliasAs("start_hr")]
	public required string StartHour { get; init; }

	/// <summary>
	/// Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage ending before this hour.
	/// </summary>
	[AliasAs("end_hr")]
	public string? EndHour { get; init; }

	/// <summary>
	/// Usage type to retrieve.
	/// </summary>
	[AliasAs("usage_type")]
	public UsageType UsageType { get; init; } = UsageType.ApiUsage;

	/// <summary>
	/// List following results with a next_record_id provided in the previous query.
	/// </summary>
	[AliasAs("next_record_id")]
	public string? NextRecordId { get; init; }

	/// <summary>
	/// Comma separated list of tags used to group usage. If no value is provided the usage will
	/// not be broken down by tags.
	/// </summary>
	[AliasAs("tag_breakdown_keys")]
	public string? TagBreakdownKeys { get; init; }

	/// <summary>
	/// Include child org usage in the response. Defaults to true.
	/// </summary>
	[AliasAs("include_descendants")]
	public bool IncludeDescendants { get; init; } = true;
}
```

- [ ] **Step 4: Add the overload and deprecate the legacy method**

In `Datadog.Api/Interfaces/IUsage.cs`, add above the existing `GetHourlyUsageAttributionAsync`:

```csharp
	/// <summary>
	/// Get hourly usage attribution. Multi-region data is available starting March 1, 2023.
	/// This endpoint requires the usage_read authorization scope.
	/// </summary>
	/// <param name="request">The query parameters.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/?code-lang=curl#get-hourly-usage-attribution"/>
	[Get("/v1/usage/hourly-attribution")]
	Task<HourlyUsageAttributionResponse> GetHourlyUsageAttributionAsync(
		GetHourlyUsageAttributionRequest request,
		CancellationToken cancellationToken);
```

and add `[Obsolete("Use the GetHourlyUsageAttributionRequest overload. This method will be removed in 3.0.")]` directly above the legacy method's `[Get("/v1/usage/hourly-attribution")]`.

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~UsageRequestUriTests"`

Expected: PASS, 4 tests.

- [ ] **Step 6: Commit**

```bash
git add Datadog.Api/Models/Usage/GetHourlyUsageAttributionRequest.cs Datadog.Api/Interfaces/IUsage.cs Datadog.Api.Test/UsageRequestUriTests.cs
git commit -m "feat: add GetHourlyUsageAttributionRequest overload to IUsage"
```

---

### Task 7: Mandatory `CancellationToken` on the two metadata endpoints

`GetMetadataAsync` and `GetRelatedAssetsAsync` have no filters — their only optional parameter is `CancellationToken cancellationToken = default`. Making it mandatory matches the other 10 interfaces and clears the remaining `S2360` findings on those two lines. **This is a source-breaking change** with no deprecation path, which is why it is its own task and its own reviewer gate.

**Files:**
- Modify: `Datadog.Api/Interfaces/IMetrics.cs` (the `GetMetadataAsync` and `GetRelatedAssetsAsync` blocks)
- Modify: `Datadog.Api.Test/MetricsRequestUriTests.cs`

**Interfaces:**
- Consumes: `RequestUriTestHarness.CaptureUriAsync<T>` (Task 1).
- Produces: `IMetrics.GetMetadataAsync(string, CancellationToken)` and `IMetrics.GetRelatedAssetsAsync(string, CancellationToken)`, both with a mandatory token.

- [ ] **Step 1: Write the failing test**

Append to `MetricsRequestUriTests.cs`, inside the class:

```csharp
	[Fact]
	public async Task GetMetadata_RequiresCancellationToken_AndBuildsPath()
	{
		var uri = await RequestUriTestHarness.CaptureUriAsync<IMetrics>(
			api => api.GetMetadataAsync("system.cpu.idle", CancellationToken));

		uri.Should().StartWith("/v1/metrics/system.cpu.idle");
	}

	[Fact]
	public async Task GetRelatedAssets_RequiresCancellationToken_AndBuildsPath()
	{
		var uri = await RequestUriTestHarness.CaptureUriAsync<IMetrics>(
			api => api.GetRelatedAssetsAsync("system.cpu.idle", CancellationToken));

		uri.Should().StartWith("/v2/metrics/system.cpu.idle/assets");
	}
```

- [ ] **Step 2: Run the tests to verify they pass against the current signature**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~MetricsRequestUriTests"`

Expected: PASS — these compile against the optional token too. They exist to prove the *path* is unchanged by Step 3, which is the actual risk.

- [ ] **Step 3: Make the token mandatory**

In `Datadog.Api/Interfaces/IMetrics.cs`, change both signatures, removing only `= default`:

```csharp
	[Get("/v1/metrics/{metricName}")]
	Task<MetricMetadataResponse> GetMetadataAsync(
		[Query("metricName")] string metricName,
		CancellationToken cancellationToken);
```

```csharp
	[Get("/v2/metrics/{metricName}/assets")]
	Task<MetricMetadataResponse> GetRelatedAssetsAsync(
		[Query("metricName")] string metricName,
		CancellationToken cancellationToken);
```

Leave the `[Query("metricName")]` attributes exactly as they are. They look wrong next to a `{metricName}` path placeholder, but changing them would alter the request; that question is out of scope here and is recorded in "Follow-ups" below.

- [ ] **Step 4: Run the tests and the build**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~MetricsRequestUriTests"`

Expected: PASS, 8 tests — identical URIs to Step 2.

Run: `dotnet build Datadog.Api.slnx -v:minimal`

Expected: `0 Error(s)`. If `MetricTests.cs` fails to compile because it relied on the optional token, add the token at those call sites now — they already have `CancellationToken` in scope from `BaseTest`.

- [ ] **Step 5: Commit**

```bash
git add Datadog.Api/Interfaces/IMetrics.cs Datadog.Api.Test/MetricsRequestUriTests.cs Datadog.Api.Test/MetricTests.cs
git commit -m "feat!: require CancellationToken on GetMetadataAsync and GetRelatedAssetsAsync"
```

---

### Task 8: Migrate the live-API tests off the obsolete overloads

The existing tests must stop using the deprecated methods, or the deprecation is untested and the `#pragma` suppressions become permanent.

**Files:**
- Modify: `Datadog.Api.Test/MetricTests.cs`
- Modify: `Datadog.Api.Test/TeamsTests.cs`
- Modify: `Datadog.Api.Test/UsageTests.cs`

**Interfaces:**
- Consumes: every request object from Tasks 2–6.
- Produces: nothing consumed by later tasks.

- [ ] **Step 1: Find every remaining obsolete call site**

Run: `dotnet build Datadog.Api.slnx -v:minimal -p:TreatWarningsAsErrors=false 2>&1 | grep CS0618`

Expected: a list of call sites in `MetricTests.cs`, `TeamsTests.cs`, `UsageTests.cs`. Record it — this list is the task's checklist.

- [ ] **Step 2: Migrate each call site**

Replace the legacy calls with the request-object overloads. For example, in `MetricTests.cs`:

```csharp
		// before
		() => Client.Metrics.GetActiveAsync(oneHourAgoUnixTimestamp, cancellationToken: CancellationToken),

		// after
		() => Client.Metrics.GetActiveAsync(
			new GetActiveMetricsRequest { From = oneHourAgoUnixTimestamp },
			CancellationToken),
```

and in `TeamsTests.cs`:

```csharp
		// before
		() => Client.Teams.GetAsync(cancellationToken: CancellationToken),

		// after
		() => Client.Teams.GetAsync(new GetTeamsRequest(), CancellationToken),
```

and in `UsageTests.cs` — note `startDate`/`endDate` are already in scope in each test:

```csharp
		// before
		() => Client.Usage.GetHourlyUsageAsync(
			startDate,
			endDate,
			[ProductFamily.All],
			cancellationToken: CancellationToken),

		// after
		() => Client.Usage.GetHourlyUsageAsync(
			new GetHourlyUsageRequest
			{
				StartHour = startDate,
				EndHour = endDate,
				ProductFamilies = [ProductFamily.All]
			},
			CancellationToken),
```

```csharp
		// before
		() => Client.Usage.GetHourlyUsageAttributionAsync(
			startDate,
			endDate,
			UsageType.ApiUsage,
			cancellationToken: CancellationToken),

		// after
		() => Client.Usage.GetHourlyUsageAttributionAsync(
			new GetHourlyUsageAttributionRequest
			{
				StartHour = startDate,
				EndHour = endDate,
				UsageType = UsageType.ApiUsage
			},
			CancellationToken),
```

Add `using Datadog.Api.Models.Metrics;`, `using Datadog.Api.Models.Teams;` and `using Datadog.Api.Models.Usage;` to the respective files as needed. Do **not** add `#pragma warning disable CS0618` to these files — the point is that they no longer need it.

- [ ] **Step 3: Verify no obsolete usage remains outside the equivalence tests**

Run: `dotnet build Datadog.Api.slnx -v:minimal`

Expected: `0 Warning(s) 0 Error(s)`.

Run: `grep -rn "CS0618" Datadog.Api.Test/`

Expected: matches only in `MetricsRequestUriTests.cs`, `TeamsRequestUriTests.cs` and `UsageRequestUriTests.cs`, where exercising the legacy path is the point.

- [ ] **Step 4: Run the full credential-free suite**

Run: `dotnet test Datadog.Api.Test/Datadog.Api.Test.csproj --filter "FullyQualifiedName~RequestUriTests|FullyQualifiedName~HttpExtensionsTests|FullyQualifiedName~RequestUriTestHarnessTests"`

Expected: PASS. The live-API tests need credentials and are expected to be run separately by someone holding a Datadog API key.

- [ ] **Step 5: Commit**

```bash
git add Datadog.Api.Test/MetricTests.cs Datadog.Api.Test/TeamsTests.cs Datadog.Api.Test/UsageTests.cs
git commit -m "test: migrate live-API tests to request-object overloads"
```

---

### Task 9: Release metadata, docs, and the Codacy pattern

**Files:**
- Modify: `version.json:3`
- Modify: `Datadog.Api/Datadog.Api.csproj` (`PackageReleaseNotes`)
- Modify: `README.md`

**Interfaces:**
- Consumes: everything above.
- Produces: nothing.

- [ ] **Step 1: Bump the minor version**

The release is additive apart from Task 7, so the minor version moves. In `version.json`:

```json
  "version": "2.1",
```

- [ ] **Step 2: Update the package release notes**

In `Datadog.Api/Datadog.Api.csproj`, replace the `PackageReleaseNotes` element:

```xml
    <PackageReleaseNotes>Filtered endpoints on IMetrics, ITeams and IUsage now take a request object; the old optional-parameter overloads still work but are obsolete and will be removed in 3.0. GetMetadataAsync and GetRelatedAssetsAsync now require a CancellationToken.</PackageReleaseNotes>
```

- [ ] **Step 3: Document the new style in the README**

Append to `README.md` the following section (the outer fence below is four backticks so the inner C# fence is part of the content you paste):

````markdown
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
````

- [ ] **Step 4: Verify the build and the version**

Run: `dotnet build Datadog.Api.slnx -v:minimal`

Expected: `0 Warning(s) 0 Error(s)`, and the generated package name shows `2.1.x`.

- [ ] **Step 5: Commit**

```bash
git add version.json Datadog.Api/Datadog.Api.csproj README.md
git commit -m "docs: document request objects and bump to 2.1"
```

- [ ] **Step 6: Disable `S2360` on the Codacy Code patterns page (manual, not code)**

This cannot be done from the repository. Someone with Codacy permissions must open **Code patterns → SonarC#** and disable `S2360` for the organisation (or for this repository), because:

- the legacy overloads deliberately remain until 3.0, so all 28 findings persist regardless of this work;
- `.codacy.yml` cannot disable patterns — `exclude_rules` is not a supported key, and the existing block in `Joker.Api/.codacy.yml` is inert;
- `GlobalSuppressions.cs` cannot either, because `SonarAnalyzer.CSharp` is not a package reference, so no analyzer in the build knows `S2360`.

While doing so, delete the two inert artefacts so they stop implying protection that does not exist:

```bash
# remove the dead engines block from .codacy.yml, keeping exclude_paths
# remove the S2360 SuppressMessage from Datadog.Api/GlobalSuppressions.cs
git commit -am "chore: remove inert S2360 suppressions"
```

---

## Follow-ups (deliberately out of scope)

- **`[Query("metricName")]` on a path placeholder.** `GetMetadataAsync` and `GetRelatedAssetsAsync` decorate a `{metricName}` route parameter with `[Query]`. This may be appending a redundant query parameter. Worth a separate spike using the Task 1 harness — it can now be checked without credentials.
- **3.0: delete the obsolete overloads.** That is the change that actually clears `S2360`. Every equivalence test in this plan asserts against a literal expectation *as well as* against the legacy call, so when the `viaLegacy` half is deleted the tests still cover the behaviour. Tasks 2, 3 and 4 assert a full literal URI; Tasks 5 and 6 assert literal fragments (`page[limit]=500`, `usage_type=api_usage`) because their full URIs are long — when the legacy calls go, strengthen those two to full literals by pasting in the URI the test then produces.
- **Dependabot PR #1** (`coverlet.collector` 8.0.1 → 10.0.1, open 63 days) references a package no longer in `Directory.Packages.props`; it was replaced by `Microsoft.Testing.Extensions.CodeCoverage`. It should be closed rather than merged.

---

## Appendix A: Spike results (2026-09-08, Refit 15.2.0, net10.0)

Three spikes ran before this plan was written. All passed. The full spike source is not retained; these are the outcomes the plan depends on.

**Spike 1 — `[AliasAs]` on flattened query-object properties.** Bracketed Datadog names survive intact and match parameter-level aliases byte-for-byte:

```
metrics  OBJECT : /v2/metrics?filter%5Bconfigured%5D=True&filter%5Btags_configured%5D=env%3Aprod&window%5Bseconds%5D=3600
metrics  PARAMS : /v2/metrics?filter%5Bconfigured%5D=True&filter%5Btags_configured%5D=env%3Aprod&window%5Bseconds%5D=3600
teams    OBJECT : /v2/team?page%5Bnumber%5D=2&fields%5Bteam%5D=name%2Chandle
teams    PARAMS : /v2/team?page%5Bnumber%5D=2&fields%5Bteam%5D=name%2Chandle
partial  OBJECT : /v2/metrics?window%5Bseconds%5D=60
partial  PARAMS : /v2/metrics?window%5Bseconds%5D=60
```

Unset properties are omitted, exactly as `null` parameters are. Collections comma-join identically.

**Spike 2 — `[Obsolete]` on an interface method.** Refit generated an implementation for an `[Obsolete]` interface method and the project compiled with `TreatWarningsAsErrors=true` and `0 Error(s)`. Callers need `#pragma warning disable CS0618`; the generated code does not.

**Spike 3 — `required` members, `[EnumMember]` enums, and defaulted non-nullable properties.**

```
OBJECT : /v2/usage/hourly_usage?filter[timestamp][start]=2026-09-01T00&filter[timestamp][end]=2026-09-02T00&filter[product_families]=analyzed_logs&filter[include_descendants]=False&page[limit]=500&usage_type=api_usage
PARAMS : /v2/usage/hourly_usage?filter[timestamp][start]=2026-09-01T00&filter[timestamp][end]=2026-09-02T00&filter[product_families]=analyzed_logs&filter[include_descendants]=False&page[limit]=500&usage_type=api_usage
identical : True
EnumMember honoured : True
enum collection     : True
```

`required` + `init` works with Refit's flattening. `[EnumMember(Value = "api_usage")]` is honoured on request-object properties. A non-nullable property with an initialiser is still sent, which is why Tasks 5 and 6 must keep those properties non-nullable.

**Spike 4 — the exact literals asserted in Tasks 2–4.** Run to confirm the plan's hard-coded expectations rather than guess them:

```
active full  : [/v1/metrics?from=1757000000&host=web-01&tag_filter=env:prod]
active min   : [/v1/metrics?from=1757000000]
EMPTY object : [/v2/metrics]
metrics full : [/v2/metrics?filter[configured]=True&filter[metric_type]=gauge&window[seconds]=3600]

empty has no '?' : True
```

Three things this pins down: a request object with **every** property unset produces no query string and **no trailing `?`** (so the `Should().Be("/v2/metrics")` and `Should().Be("/v2/team")` assertions in Tasks 3 and 4 are correct); `bool` renders capitalised as `True`/`False`; and query parameter order follows property declaration order, which is why each request object above declares its properties in the same order as the legacy parameter list.
