using Datadog.Api.Interfaces;
using Refit;
using System.Net;

namespace Datadog.Api.Test;

/// <summary>
/// Builds a Refit client over a stub handler that records the outgoing request URI instead of
/// calling Datadog. These tests need no API key, so they run in CI.
/// </summary>
public static class RequestUriTestHarness
{
	private sealed class CapturingHandler : HttpMessageHandler
	{
		public string? Uri { get; private set; }

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Uri = System.Uri.UnescapeDataString(request.RequestUri!.PathAndQuery);

			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
			{
				Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
			});
		}
	}

	/// <summary>
	/// Invokes <paramref name="call"/> against a stubbed <typeparamref name="T"/> and returns the
	/// URI it produced. Deserialisation of the stub response is irrelevant and its failure is
	/// ignored: the URI is recorded before any response is read.
	/// </summary>
	public static async Task<string> CaptureUriAsync<T>(Func<T, Task> call) where T : class
	{
		var handler = new CapturingHandler();
		using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.datadoghq.com") };
		var api = RestService.For<T>(httpClient);

		try
		{
			await call(api);
		}
		catch
		{
			// Only the URI matters here; the stub response is deliberately not a valid payload.
		}

		return handler.Uri!;
	}
}

public class RequestUriTestHarnessTests
{
	/// <summary>
	/// Validates the harness itself against an endpoint with no parameters, so a failure here
	/// means the harness is broken rather than an endpoint being wrong.
	/// </summary>
	[Fact]
	public async Task CaptureUriAsync_ParameterlessEndpoint_ReturnsPath()
	{
		var uri = await RequestUriTestHarness.CaptureUriAsync<IHosts>(
			api => api.GetAsync(TestContext.Current.CancellationToken));

		uri.Should().Be("/v1/hosts");
	}
}
