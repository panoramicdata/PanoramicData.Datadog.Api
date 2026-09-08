using Datadog.Api.Models.Teams;

namespace Datadog.Api.Test;

public class TeamsTests(DatadogClientFixture fixture, ITestOutputHelper output) : BaseTest(fixture, output)
{
	[Fact]
	public async Task Get_Page_Succeeds()
	{
		// Act
		var result = await ExecuteApiCallAsync(
			() => Client.Teams.GetAsync(new GetTeamsRequest(), CancellationToken),
			nameof(Get_Page_Succeeds));

		// Assert
		result.Should().NotBeNull();
	}
}
