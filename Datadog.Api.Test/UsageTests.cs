using Datadog.Api.Models.Usage;
using System.Globalization;

namespace Datadog.Api.Test;

public class UsageTests(DatadogClientFixture fixture, ITestOutputHelper output) : BaseTest(fixture, output)
{
	[Fact]
	public async Task Get_HourlyUsage_Succeeds()
	{
		// Arrange
		// Get the last 30 days of usage data, at the midnight boundary
		// Create start and end dates for the last 30 days as strings in the format "YYYY-MM-DDThh"
		var currentDate = DateTimeOffset.UtcNow;
		var startDate = currentDate.AddDays(-30).ToString("yyyy-MM-ddT00", CultureInfo.InvariantCulture);
		var endDate = currentDate.ToString("yyyy-MM-ddT00", CultureInfo.InvariantCulture);

		// Act
		var result = await ExecuteApiCallAsync(
			() => Client.Usage.GetHourlyUsageAsync(
				new GetHourlyUsageRequest
				{
					StartHour = startDate,
					EndHour = endDate,
					ProductFamilies = [ProductFamily.All]
				},
				CancellationToken),
			nameof(Get_HourlyUsage_Succeeds));

		// Assert
		result.Should().NotBeNull();
	}

	[Fact]
	public async Task Get_HourlyUsageAttribution_Succeeds()
	{
		// Arrange
		// Get the last 30 days of usage data, at the midnight boundary
		// Create start and end dates for the last 30 days as strings in the format "YYYY-MM-DDThh"
		var currentDate = DateTimeOffset.UtcNow;
		var startDate = currentDate.AddDays(-1).ToString("yyyy-MM-ddT00", CultureInfo.InvariantCulture);
		var endDate = currentDate.ToString("yyyy-MM-ddT00", CultureInfo.InvariantCulture);

		// Act
		var result = await ExecuteApiCallAsync(
			() => Client.Usage.GetHourlyUsageAttributionAsync(
				new GetHourlyUsageAttributionRequest
				{
					StartHour = startDate,
					EndHour = endDate,
					UsageType = UsageType.ApiUsage
				},
				CancellationToken),
			nameof(Get_HourlyUsageAttribution_Succeeds));

		// Assert
		result.Should().NotBeNull();
	}
}
