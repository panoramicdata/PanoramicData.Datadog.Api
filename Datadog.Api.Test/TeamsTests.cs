namespace Datadog.Api.Test;

#pragma warning disable CS0618 // TODO Task 8: migrate to request-object overloads

public class TeamsTests(DatadogClientFixture fixture, ITestOutputHelper output) : BaseTest(fixture, output)
{
	[Fact]
	public async Task Get_Page_Succeeds()
	{
		// Act
		var result = await ExecuteApiCallAsync(
			() => Client.Teams.GetAsync(cancellationToken: CancellationToken),
			nameof(Get_Page_Succeeds));

		// Assert
		result.Should().NotBeNull();
	}
}

#pragma warning restore CS0618
