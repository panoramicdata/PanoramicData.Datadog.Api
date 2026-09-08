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
