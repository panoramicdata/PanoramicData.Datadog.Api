using Refit;

namespace Datadog.Api.Models.Metrics;

/// <summary>
/// Query parameters for listing metrics that can be configured in the Metrics Summary page or
/// with Metrics without Limits.
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-a-list-of-metrics"/>
public sealed class GetMetricsRequest
{
	/// <summary>
	/// Filter custom metrics that have configured tags.
	/// </summary>
	[AliasAs("filter[configured]")]
	public bool? FilterConfigured { get; init; }

	/// <summary>
	/// Filter tag configurations by configured tags.
	/// </summary>
	[AliasAs("filter[tags_configured]")]
	public string? FilterTagsConfigured { get; init; }

	/// <summary>
	/// Filter metrics by metric type.
	/// </summary>
	[AliasAs("filter[metric_type]")]
	public MetricType? FilterMetricType { get; init; }

	/// <summary>
	/// Filter distributions with additional percentile aggregations enabled or disabled.
	/// </summary>
	[AliasAs("filter[include_percentiles]")]
	public bool? FilterIncludePercentiles { get; init; }

	/// <summary>
	/// (Beta) Filter custom metrics that have or have not been queried in the specified
	/// window[seconds]. If no window is provided or the window is less than 2 hours, a default of
	/// 2 hours will be applied.
	/// </summary>
	[AliasAs("filter[queried]")]
	public bool? FilterQueried { get; init; }

	/// <summary>
	/// Filter metrics that have been submitted with the given tags. Supports boolean and wildcard
	/// expressions. Can only be combined with the filter[queried] filter.
	/// </summary>
	[AliasAs("filter[tags]")]
	public string? FilterTags { get; init; }

	/// <summary>
	/// The number of seconds of look back (from now) to apply to a filter[tag] or filter[queried]
	/// query. Default value is 3600 (1 hour), maximum value is 2,592,000 (30 days).
	/// </summary>
	[AliasAs("window[seconds]")]
	public int? WindowSeconds { get; init; }
}
