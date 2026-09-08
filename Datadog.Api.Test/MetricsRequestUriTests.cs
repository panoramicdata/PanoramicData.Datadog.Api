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
}

#pragma warning restore CS0618
