using Datadog.Api.Models;
using Datadog.Api.Models.Teams;
using Refit;
using System.Runtime.InteropServices;

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
		[Optional] [AliasAs("page[number]")] int? page,
		[Optional] [AliasAs("page[size]")] int? pageSize,
		[Optional] [AliasAs("sort")] string? sort,
		[Optional] [AliasAs("filter[keyword]")] string? filterKeyword,
		[Optional] [AliasAs("filter[me]")] bool? filterMe,
		[Optional] [AliasAs("fields[team]")] ICollection<string>? fields,
		[Optional] CancellationToken cancellationToken
		);
}
