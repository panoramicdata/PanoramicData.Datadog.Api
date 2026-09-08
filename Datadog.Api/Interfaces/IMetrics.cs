using Datadog.Api.Models.Metrics;
using Refit;

namespace Datadog.Api.Interfaces;

/// <summary>
/// Metrics
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/"/>
public interface IMetrics
{
	/// <summary>
	/// Get Metrics
	/// </summary>
	/// <param name="request">The query parameters.</param>
	/// <param name="cancellationToken">The cancellation token</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-active-metrics-list"/>
	[Get("/v1/metrics")]
	Task<ActiveMetricsResponse> GetActiveAsync(
		GetActiveMetricsRequest request,
		CancellationToken cancellationToken);

	/// <summary>
	/// Get Metrics
	/// </summary>
	/// <param name="from">Seconds since the Unix epoch.</param>
	/// <param name="host">Optional: Hostname for filtering the list of metrics returned. If set, metrics retrieved are those with the corresponding hostname tag.</param>
	/// <param name="tagFilter">Filter metrics that have been submitted with the given tags. Supports boolean and wildcard expressions. Cannot be combined with other filters.</param>
	/// <param name="cancellationToken">The cancellation token</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-active-metrics-list"/>
	[Obsolete("Use the GetActiveMetricsRequest overload. This method will be removed in 3.0.")]
	[Get("/v1/metrics")]
	Task<ActiveMetricsResponse> GetActiveAsync(
		[AliasAs("from")] long from,
		[AliasAs("host")] string? host = null,
		[AliasAs("tag_filter")] string? tagFilter = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Get a list of metrics.
	/// Returns all metrics that can be configured in the Metrics Summary page or with Metrics without Limits™ (matching additional filters if specified). This endpoint requires the metrics_read authorization scope.
	/// </summary>
	/// <param name="filterConfigured">Filter custom metrics that have configured tags.</param>
	/// <param name="filterTagsConfigured">Filter tag configurations by configured tags.</param>
	/// <param name="filterMetricType">Filter metrics by metric type.	Allowed enum values : gauge, count, rate, distribution</param>
	/// <param name="filterIncludePercentiles">Filter distributions with additional percentile aggregations enabled or disabled.</param>
	/// <param name="filterQueried">(Beta) Filter custom metrics that have or have not been queried in the specified window[seconds]. If no window is provided or the window is less than 2 hours, a default of 2 hours will be applied.</param>
	/// <param name="filterTags">Filter metrics that have been submitted with the given tags. Supports boolean and wildcard expressions. Can only be combined with the filter[queried] filter.</param>
	/// <param name="windowSeconds">The number of seconds of look back (from now) to apply to a filter[tag] or filter[queried] query. Default value is 3600 (1 hour), maximum value is 2,592,000 (30 days).</param>
	/// <param name="cancellationToken">The cancellation token</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-a-list-of-metrics"/>"
	[Get("/v2/metrics")]
	Task<MetricsResponse> GetMetricsAsync(
		[AliasAs("filter[configured]")] bool? filterConfigured = null,
		[AliasAs("filter[tags_configured]")] string? filterTagsConfigured = null,
		[AliasAs("filter[metric_type]")] MetricType? filterMetricType = null,
		[AliasAs("filter[include_percentiles]")] bool? filterIncludePercentiles = null,
		[AliasAs("filter[queried]")] bool? filterQueried = null,
		[AliasAs("filter[tags]")] string? filterTags = null,
		[AliasAs("window[seconds]")] int? windowSeconds = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Get metric metadata
	/// </summary>
	/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-a-list-of-metrics"/>
	[Get("/v1/metrics/{metricName}")]
	Task<MetricMetadataResponse> GetMetadataAsync(
		[Query("metricName")] string metricName,
		CancellationToken cancellationToken = default);


	/// <summary>
	/// Related Assets to a Metric
	/// Returns dashboards, monitors, notebooks, and SLOs that a metric is stored in, if any. Updated every 24 hours.
	/// </summary>
	/// <param name="metricName">The name of the metric.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/?code-lang=python#related-assets-to-a-metric"/>
	[Get("/v2/metrics/{metricName}/assets")]
	Task<MetricMetadataResponse> GetRelatedAssetsAsync(
		[Query("metricName")] string metricName,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Query timeseries points. This endpoint requires the timeseries_query authorization scope.
	/// </summary>
	[Get("/v1/query")]
	Task<QueryTimeSeriesPointsResponse> QueryTimeSeriesPointsAsync(
		[AliasAs("from")] long from,
		[AliasAs("to")] long until,
		[AliasAs("query")] string query,
		CancellationToken cancellationToken);
}
