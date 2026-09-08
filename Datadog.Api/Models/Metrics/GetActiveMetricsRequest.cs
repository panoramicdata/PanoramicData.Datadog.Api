using Refit;

namespace Datadog.Api.Models.Metrics;

/// <summary>
/// Query parameters for retrieving the active metrics list.
/// </summary>
/// <seealso href="https://docs.datadoghq.com/api/latest/metrics/#get-active-metrics-list"/>
public sealed class GetActiveMetricsRequest
{
	/// <summary>
	/// Seconds since the Unix epoch.
	/// </summary>
	[AliasAs("from")]
	public required long From { get; init; }

	/// <summary>
	/// Hostname for filtering the list of metrics returned. If set, metrics retrieved are those
	/// with the corresponding hostname tag.
	/// </summary>
	[AliasAs("host")]
	public string? Host { get; init; }

	/// <summary>
	/// Filter metrics that have been submitted with the given tags. Supports boolean and wildcard
	/// expressions. Cannot be combined with other filters.
	/// </summary>
	[AliasAs("tag_filter")]
	public string? TagFilter { get; init; }
}
