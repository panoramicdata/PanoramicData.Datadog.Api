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
