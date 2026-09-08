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
