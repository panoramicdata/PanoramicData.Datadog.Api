using Refit;

namespace Datadog.Api.Models.Usage;

/// <summary>
/// Query parameters for retrieving hourly usage attribution.
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/?code-lang=curl#get-hourly-usage-attribution"/>
public sealed class GetHourlyUsageAttributionRequest
{
	/// <summary>
	/// Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage beginning at this hour.
	/// </summary>
	[AliasAs("start_hr")]
	public required string StartHour { get; init; }

	/// <summary>
	/// Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage ending before this hour.
	/// </summary>
	[AliasAs("end_hr")]
	public string? EndHour { get; init; }

	/// <summary>
	/// Usage type to retrieve.
	/// </summary>
	[AliasAs("usage_type")]
	public UsageType UsageType { get; init; } = UsageType.ApiUsage;

	/// <summary>
	/// List following results with a next_record_id provided in the previous query.
	/// </summary>
	[AliasAs("next_record_id")]
	public string? NextRecordId { get; init; }

	/// <summary>
	/// Comma separated list of tags used to group usage. If no value is provided the usage will
	/// not be broken down by tags.
	/// </summary>
	[AliasAs("tag_breakdown_keys")]
	public string? TagBreakdownKeys { get; init; }

	/// <summary>
	/// Include child org usage in the response. Defaults to true.
	/// </summary>
	[AliasAs("include_descendants")]
	public bool IncludeDescendants { get; init; } = true;
}
