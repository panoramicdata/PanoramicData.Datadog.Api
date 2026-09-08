using Datadog.Api.Models;
using Datadog.Api.Models.Usage;
using Refit;

namespace Datadog.Api.Interfaces;

/// <summary>
/// Usage API
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/"/>
public interface IUsage
{
	/// <summary>
	/// Get hourly usage by product family.
	/// This endpoint requires the usage_read authorization scope.
	/// </summary>
	/// <param name="request">The query parameters.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/?code-lang=curl#get-hourly-usage-by-product-family"/>
	[Get("/v2/usage/hourly_usage")]
	Task<StringIdentifiedResponse<HourlyUsage>> GetHourlyUsageAsync(
		GetHourlyUsageRequest request,
		CancellationToken cancellationToken);

	/// <summary>
	/// Get hourly usage attribution. Multi-region data is available starting March 1, 2023.
	/// This endpoint requires the usage_read authorization scope.
	/// </summary>
	/// <param name="request">The query parameters.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/?code-lang=curl#get-hourly-usage-attribution"/>
	[Get("/v1/usage/hourly-attribution")]
	Task<HourlyUsageAttributionResponse> GetHourlyUsageAttributionAsync(
		GetHourlyUsageAttributionRequest request,
		CancellationToken cancellationToken);

	/// <summary>
	/// Get hourly usage by product family.
	/// This endpoint requires the usage_read authorization scope.
	/// </summary>
	/// <param name="startHour">Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage beginning at this hour.</param>
	/// <param name="endHour">Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage ending before this hour.</param>
	/// <param name="productFamilies">Comma separated list of product families to retrieve.</param>
	/// <param name="includeDescendants">Include child organization usage in the response. Defaults to false.</param>
	/// <param name="includeBreakdown">Include breakdown of usage by subcategories where applicable (for product family logs only). Defaults to false.</param>
	/// <param name="versions">Comma separated list of product family versions to use in the format product_family:version.</param>
	/// <param name="limit">Maximum number of results to return (between 1 and 500) - defaults to 500 if limit not specified.</param>
	/// <param name="nextRecordId">List following results with a next_record_id provided in the previous query.</param>
	/// <param name="cancellationToken"></param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/?code-lang=curl#get-hourly-usage-by-product-family"/>
	[Obsolete("Use the GetHourlyUsageRequest overload. This method will be removed in 3.0.")]
	[Get("/v2/usage/hourly_usage")]
	Task<StringIdentifiedResponse<HourlyUsage>> GetHourlyUsageAsync(
		[AliasAs("filter[timestamp][start]")] string startHour,
		[AliasAs("filter[timestamp][end]")] string endHour,
		[AliasAs("filter[product_families]")] IReadOnlyCollection<ProductFamily> productFamilies,
		[AliasAs("filter[include_descendants]")] bool includeDescendants = false,
		[AliasAs("filter[include_breakdown]")] bool includeBreakdown = false,
		[AliasAs("filter[versions]")] string? versions = null,
		[AliasAs("page[limit]")] int limit = 500,
		[AliasAs("page[next_record_id]")] string? nextRecordId = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Get hourly usage attribution. Multi-region data is available starting March 1, 2023.
	/// This endpoint requires the usage_read authorization scope.
	/// </summary>
	/// <param name="startHour">Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage beginning at this hour.</param>
	/// <param name="endHour">Datetime in ISO-8601 format, UTC, precise to hour: [YYYY-MM-DDThh] for usage ending before this hour.</param>
	/// <param name="usageType">Usage type to retrieve.</param>
	/// <param name="nextRecordId">List following results with a next_record_id provided in the previous query.</param>
	/// <param name="tagBreakdownKeys">Comma separated list of tags used to group usage. If no value is provided the usage will not be broken down by tags.</param>
	/// <param name="includeDescendants">Include child org usage in the response. Defaults to true.</param>
	/// <param name="cancellationToken"></param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/usage-metering/?code-lang=curl#get-hourly-usage-attribution"/>
	[Obsolete("Use the GetHourlyUsageAttributionRequest overload. This method will be removed in 3.0.")]
	[Get("/v1/usage/hourly-attribution")]
	public Task<HourlyUsageAttributionResponse> GetHourlyUsageAttributionAsync(
		[AliasAs("start_hr")] string startHour,
		[AliasAs("end_hr")] string? endHour = null,
		[AliasAs("usage_type")] UsageType usageType = UsageType.ApiUsage,
		[AliasAs("next_record_id")] string? nextRecordId = null,
		[AliasAs("tag_breakdown_keys")] string? tagBreakdownKeys = null,
		[AliasAs("include_descendants")] bool includeDescendants = true,
		CancellationToken cancellationToken = default);
}
