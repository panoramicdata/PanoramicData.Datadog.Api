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
}

#pragma warning restore CS0618
