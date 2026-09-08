using Refit;

namespace Datadog.Api.Models.Usage;

/// <summary>
/// Query parameters for retrieving hourly usage by product family.
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/?code-lang=curl#get-hourly-usage-by-product-family"/>
public sealed class GetHourlyUsageRequest
{
	/// <summary>
	/// Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage beginning at this hour.
	/// </summary>
	[AliasAs("filter[timestamp][start]")]
	public required string StartHour { get; init; }

	/// <summary>
	/// Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage ending before this hour.
	/// </summary>
	[AliasAs("filter[timestamp][end]")]
	public required string EndHour { get; init; }

	/// <summary>
	/// Product families to retrieve.
	/// </summary>
	[AliasAs("filter[product_families]")]
	public required IReadOnlyCollection<ProductFamily> ProductFamilies { get; init; }

	/// <summary>
	/// Include child organization usage in the response. Defaults to false.
	/// </summary>
	[AliasAs("filter[include_descendants]")]
	public bool IncludeDescendants { get; init; }

	/// <summary>
	/// Include breakdown of usage by subcategories where applicable (for product family logs only). Defaults to false.
	/// </summary>
	[AliasAs("filter[include_breakdown]")]
	public bool IncludeBreakdown { get; init; }

	/// <summary>
	/// Comma separated list of product family versions to use in the format product_family:version.
	/// </summary>
	[AliasAs("filter[versions]")]
	public string? Versions { get; init; }

	/// <summary>
	/// Maximum number of results to return (between 1 and 500).
	/// </summary>
	[AliasAs("page[limit]")]
	public int Limit { get; init; } = 500;

	/// <summary>
	/// List following results with a next_record_id provided in the previous query.
	/// </summary>
	[AliasAs("page[next_record_id]")]
	public string? NextRecordId { get; init; }
}
