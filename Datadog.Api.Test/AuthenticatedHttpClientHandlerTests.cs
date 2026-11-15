using Microsoft.Extensions.Logging;
using System.Net;

namespace Datadog.Api.Test;

public sealed class AuthenticatedHttpClientHandlerTests : IDisposable
{
	private const string FakeApiKey = "not-a-real-api-key-used-only-in-tests";
	private const string FakeApplicationKey = "not-a-real-application-key-used-only-in-tests";

	private readonly HttpListener _listener = new();
	private readonly string _baseUrl;
	private readonly List<IDisposable> _owned = [];

	public AuthenticatedHttpClientHandlerTests()
	{
		var port = GetFreePort();
		_baseUrl = $"http://localhost:{port}/";
		_listener.Prefixes.Add(_baseUrl);
		_listener.Start();
	}

	public void Dispose()
	{
		_listener.Close();
		foreach (var owned in _owned)
		{
			owned.Dispose();
		}
	}

	private static int GetFreePort()
	{
		var tcp = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
		tcp.Start();
		var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
		tcp.Stop();
		return port;
	}

	private HttpClient CreateClient(DatadogClientOptions options)
	{
		var handler = new AuthenticatedHttpClientHandler(options);
		var client = new HttpClient(handler);
		_owned.Add(client);
		return client;
	}

	private async Task<System.Collections.Specialized.NameValueCollection> ServeOnceAsync(string responseBody, Func<Task> clientCall)
	{
		var serverTask = Task.Run(async () =>
		{
			var context = await _listener.GetContextAsync();
			var headers = new System.Collections.Specialized.NameValueCollection(context.Request.Headers);
			var bytes = System.Text.Encoding.UTF8.GetBytes(responseBody);
			context.Response.ContentType = "application/json";
			await context.Response.OutputStream.WriteAsync(bytes, TestContext.Current.CancellationToken);
			context.Response.Close();
			return headers;
		}, TestContext.Current.CancellationToken);

		await clientCall();
		return await serverTask;
	}

	[Fact]
	public async Task SendAsync_AddsAuthenticationAndUserAgentHeaders()
	{
		var client = CreateClient(new DatadogClientOptions
		{
			ApiKey = FakeApiKey,
			ApplicationKey = FakeApplicationKey,
			UserAgent = "unit-test-agent"
		});

		var headers = await ServeOnceAsync("{}", async () =>
		{
			using var response = await client.GetAsync(_baseUrl + "v1/hosts", TestContext.Current.CancellationToken);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
		});

		headers["DD-API-KEY"].Should().Be(FakeApiKey);
		headers["DD-APPLICATION-KEY"].Should().Be(FakeApplicationKey);
		headers["User-Agent"].Should().Be("unit-test-agent");
		headers["Accept"].Should().Contain("application/json");
	}

	[Fact]
	public async Task SendAsync_NoApplicationKeyOrUserAgent_OmitsThoseHeaders()
	{
		var client = CreateClient(new DatadogClientOptions { ApiKey = FakeApiKey });

		var headers = await ServeOnceAsync("{}", async () =>
		{
			using var response = await client.GetAsync(_baseUrl + "v1/hosts", TestContext.Current.CancellationToken);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
		});

		headers["DD-API-KEY"].Should().Be(FakeApiKey);
		headers["DD-APPLICATION-KEY"].Should().BeNull();
	}

	[Fact]
	public async Task SendAsync_ResponseContainingType_RewritesItToDollarType()
	{
		var client = CreateClient(new DatadogClientOptions { ApiKey = FakeApiKey });
		string? content = null;

		await ServeOnceAsync("""{"type":"host"}""", async () =>
		{
			using var response = await client.GetAsync(_baseUrl + "v1/hosts", TestContext.Current.CancellationToken);
			content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		});

		content.Should().Be("""{"$type":"host"}""");
	}

	[Theory]
	[InlineData("""{"a":1}""")]
	[InlineData("[1,2]")]
	[InlineData("not json")]
	public async Task SendAsync_DebugLogging_LogsRequestAndResponseWithoutKeys(string responseBody)
	{
		var logger = new ListLogger();
		var client = CreateClient(new DatadogClientOptions
		{
			ApiKey = FakeApiKey,
			ApplicationKey = FakeApplicationKey,
			Logger = logger
		});

		await ServeOnceAsync(responseBody, async () =>
		{
			using var content = new StringContent("{}");
			using var response = await client.PostAsync(_baseUrl + "v1/hosts", content, TestContext.Current.CancellationToken);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
		});

		logger.Messages.Should().HaveCount(2);
		logger.Messages[0].Should().Contain("REQUEST").And.Contain("v1/hosts");
		logger.Messages[1].Should().Contain("RESPONSE");
		logger.Messages.Should().AllSatisfy(m =>
			m.Should().NotContain(FakeApiKey).And.NotContain(FakeApplicationKey));
	}

	[Fact]
	public async Task SendAsync_InfoLoggerOnly_LogsNothing()
	{
		var logger = new ListLogger(LogLevel.Information);
		var client = CreateClient(new DatadogClientOptions { ApiKey = FakeApiKey, Logger = logger });

		await ServeOnceAsync("{}", async () =>
		{
			using var response = await client.GetAsync(_baseUrl + "v1/hosts", TestContext.Current.CancellationToken);
			response.StatusCode.Should().Be(HttpStatusCode.OK);
		});

		logger.Messages.Should().BeEmpty();
	}

	private sealed class ListLogger(LogLevel minimum = LogLevel.Debug) : ILogger
	{
		public List<string> Messages { get; } = [];

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			Messages.Add($"scope {state}");
			return null;
		}

		public bool IsEnabled(LogLevel logLevel) => logLevel >= minimum;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
			=> Messages.Add($"{logLevel} {eventId.Id}: {formatter(state, exception)}{exception?.Message}");
	}
}

public class DatadogClientTests
{
	[Fact]
	public void Constructor_CreatesEveryApiInterface()
	{
		using var client = new DatadogClient(new DatadogClientOptions { ApiKey = "not-a-real-api-key-used-only-in-tests" });

		client.Aws.Should().NotBeNull();
		client.Containers.Should().NotBeNull();
		client.Dashboards.Should().NotBeNull();
		client.DashboardLists.Should().NotBeNull();
		client.Downtimes.Should().NotBeNull();
		client.Hosts.Should().NotBeNull();
		client.IpAllowLists.Should().NotBeNull();
		client.Metrics.Should().NotBeNull();
		client.Roles.Should().NotBeNull();
		client.Teams.Should().NotBeNull();
		client.Usage.Should().NotBeNull();
		client.Users.Should().NotBeNull();
		client.WebhooksIntegrations.Should().NotBeNull();
	}

	[Fact]
	public void Dispose_CalledTwice_DoesNotThrow()
	{
		var client = new DatadogClient(new DatadogClientOptions { ApiKey = "not-a-real-api-key-used-only-in-tests" });

		client.Dispose();
		var act = client.Dispose;

		act.Should().NotThrow();
	}
}
