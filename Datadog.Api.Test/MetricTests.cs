using Datadog.Api.Models.Metrics;

namespace Datadog.Api.Test;

public class MetricTests(DatadogClientFixture fixture, ITestOutputHelper output) : BaseTest(fixture, output)
{
	[Fact]
	public async Task GetActiveMetrics_Succeeds()
	{
		// Arrange
		var oneHourAgoUnixTimestamp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();

		// Act
		var result = await ExecuteApiCallAsync(
			() => Client.Metrics.GetActiveAsync(
				new GetActiveMetricsRequest { From = oneHourAgoUnixTimestamp },
				CancellationToken),
			nameof(GetActiveMetrics_Succeeds));

		// Assert
		result.Should().NotBeNull();
	}

	[Fact]
	public async Task GetMetrics_Succeeds()
	{
		// Act
		var result = await ExecuteApiCallAsync(
			() => Client.Metrics.GetMetricsAsync(new GetMetricsRequest(), CancellationToken),
			nameof(GetMetrics_Succeeds));

		// Assert
		result.Should().NotBeNull();
	}

	[Fact]
	public async Task GetMetadata_Succeeds()
	{
		// Arrange
		var oneHourAgoUnixTimestamp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
		var metricsResponse = await ExecuteApiCallAsync(
			() => Client.Metrics.GetActiveAsync(
				new GetActiveMetricsRequest { From = oneHourAgoUnixTimestamp },
				CancellationToken),
			nameof(GetMetadata_Succeeds));

		// Act
		foreach (var metricName in metricsResponse!.MetricNames.Take(10))
		{
			var result = await ExecuteApiCallAsync(
				() => Client.Metrics.GetMetadataAsync(metricName, CancellationToken),
				$"{nameof(GetMetadata_Succeeds)}/{metricName}");

			// Assert
			result.Should().NotBeNull();
		}
	}


	[Fact]
	public async Task GetRelatedAssets_Succeeds()
	{
		// Arrange
		var oneHourAgoUnixTimestamp = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
		var metricsResponse = await ExecuteApiCallAsync(
			() => Client.Metrics.GetActiveAsync(
				new GetActiveMetricsRequest { From = oneHourAgoUnixTimestamp },
				CancellationToken),
			nameof(GetRelatedAssets_Succeeds));

		// Act
		foreach (var metricName in metricsResponse!.MetricNames.Take(10))
		{
			var result = await ExecuteApiCallAsync(
				() => Client.Metrics.GetRelatedAssetsAsync(metricName, CancellationToken),
				$"{nameof(GetRelatedAssets_Succeeds)}/{metricName}");

			// Assert
			result.Should().NotBeNull();
		}
	}


	[Fact]
	public async Task QueryTimeSeriesPoints_Succeeds()
	{
		// Arrange
		DateTimeOffset utcNow = DateTimeOffset.UtcNow;
		var twentyFiveHoursAgoUnixTimestamp = utcNow.AddHours(-25).ToUnixTimeSeconds();
		var oneHourAgoUnixTimestamp = utcNow.AddHours(-1).ToUnixTimeSeconds();
		var metricsResponse = await ExecuteApiCallAsync(
			() => Client.Metrics.GetActiveAsync(
				new GetActiveMetricsRequest { From = oneHourAgoUnixTimestamp },
				CancellationToken),
			nameof(QueryTimeSeriesPoints_Succeeds));

		// Act
		foreach (var metricName in metricsResponse!.MetricNames.Take(10))
		{
			var queryString = $"{metricName}{{*}}";

			var result = await ExecuteApiCallAsync(
				() => Client.Metrics.QueryTimeSeriesPointsAsync(
					twentyFiveHoursAgoUnixTimestamp,
					oneHourAgoUnixTimestamp,
					queryString,
					CancellationToken),
				$"{nameof(QueryTimeSeriesPoints_Succeeds)}/{metricName}");

			// Assert
			result.Should().NotBeNull();
		}
	}
}
