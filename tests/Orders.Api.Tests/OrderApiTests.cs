using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Orders.Api;

namespace Orders.Api.Tests;

[TestClass]
public sealed class OrderApiTests
{
    private readonly List<RunningHost> _hosts = [];
    private string _directory = null!;
    private string DatabasePath => Path.Combine(_directory, "orders.db");

    [TestInitialize]
    public void Initialize() =>
        _directory = Path.Combine(AppContext.BaseDirectory, "TestArtifacts", Guid.NewGuid().ToString("N"));

    [TestCleanup]
    public async Task Cleanup()
    {
        foreach (var host in _hosts) await host.DisposeAsync();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    [TestMethod, TestCategory("Retry")]
    public async Task SequentialReplayReturnsSameOrderAndOneRow()
    {
        var host = await StartAsync();
        using var first = await Post(host.Client);
        using var replay = await Post(host.Client);
        var original = await Order(first);
        var repeated = await Order(replay);
        Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, replay.StatusCode);
        Assert.AreEqual(original, repeated);
        Assert.AreEqual($"/api/orders/{original.Id}", first.Headers.Location?.ToString());
        Assert.AreEqual(1, (await List(host.Client)).Length);
    }

    [TestMethod, TestCategory("Retry")]
    public async Task DifferentKeysCreateDifferentOrders()
    {
        var host = await StartAsync();
        using var first = await Post(host.Client, key: "key-one");
        using var second = await Post(host.Client, key: "key-two");
        Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, second.StatusCode);
        Assert.AreNotEqual((await Order(first)).Id, (await Order(second)).Id);
        Assert.AreEqual(2, (await List(host.Client)).Length);
    }

    [TestMethod, TestCategory("Concurrency")]
    public async Task ConcurrentRetriesOnOneHostCreateExactlyOneOrder()
    {
        var gate = new RequestRendezvous(8);
        var host = await StartAsync(gate);
        await AssertConcurrentReplay(Enumerable.Repeat(host.Client, 8).ToArray(), gate);
    }

    [TestMethod, TestCategory("Concurrency")]
    public async Task ConcurrentRetriesAcrossIndependentHostsShareDurableAuthority()
    {
        var gate = new RequestRendezvous(8);
        var first = await StartAsync(gate);
        var second = await StartAsync(gate);
        await AssertConcurrentReplay(
            Enumerable.Range(0, 8).Select(i => i % 2 == 0 ? first.Client : second.Client).ToArray(), gate);
        Assert.AreEqual(1, (await List(second.Client)).Length);
    }

    [TestMethod, TestCategory("Payload")]
    [DataRow("OTHER-SKU", 1)]
    [DataRow("DEMO-KEYBOARD", 2)]
    public async Task ChangedPayloadReturnsConflictWithoutMutation(string sku, int quantity)
    {
        var host = await StartAsync();
        using var first = await Post(host.Client);
        var original = await Order(first);
        using var changed = await Post(host.Client, sku: sku, quantity: quantity);
        await AssertProblem(changed, HttpStatusCode.Conflict, "idempotency_conflict");
        var orders = await List(host.Client);
        Assert.AreEqual(1, orders.Length);
        Assert.AreEqual(original, orders[0]);
        using var replay = await Post(host.Client);
        Assert.AreEqual(HttpStatusCode.OK, replay.StatusCode);
        Assert.AreEqual(original, await Order(replay));
    }

    [TestMethod, TestCategory("Isolation")]
    public async Task CustomerScopesApplyToKeysListsAndSingleOrderReads()
    {
        var host = await StartAsync();
        using var aliceResponse = await Post(host.Client, customer: "alice");
        using var bobResponse = await Post(host.Client, customer: "bob");
        var alice = await Order(aliceResponse);
        var bob = await Order(bobResponse);
        Assert.AreEqual(HttpStatusCode.Created, aliceResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Created, bobResponse.StatusCode);
        Assert.AreNotEqual(alice.Id, bob.Id);
        CollectionAssert.AreEqual(new[] { alice }, await List(host.Client, "alice"));
        CollectionAssert.AreEqual(new[] { bob }, await List(host.Client, "bob"));
        Assert.AreEqual(0, (await List(host.Client, "charlie")).Length);
        using var own = await Get(host.Client, $"/api/orders/{alice.Id}", "alice");
        Assert.AreEqual(HttpStatusCode.OK, own.StatusCode);
        Assert.AreEqual(alice, await Order(own));
        using var foreign = await Get(host.Client, $"/api/orders/{alice.Id}", "bob");
        await AssertProblem(foreign, HttpStatusCode.NotFound, "order_not_found");
    }

    [TestMethod, TestCategory("Persistence")]
    public async Task ReplaySurvivesHostRestartWithTheSameDatabase()
    {
        var firstHost = await StartAsync();
        using var first = await Post(firstHost.Client);
        var original = await Order(first);
        Assert.AreEqual(HttpStatusCode.Created, first.StatusCode);
        await firstHost.DisposeAsync();
        var restartedHost = await StartAsync();
        CollectionAssert.AreEqual(new[] { original }, await List(restartedHost.Client));
        using var replay = await Post(restartedHost.Client);
        Assert.AreEqual(HttpStatusCode.OK, replay.StatusCode);
        Assert.AreEqual(original, await Order(replay));
        Assert.AreEqual(1, (await List(restartedHost.Client)).Length);
    }

    [TestMethod, TestCategory("Validation")]
    [DataRow("", "invalid_json")]
    [DataRow("{", "invalid_json")]
    [DataRow("[]", "invalid_json")]
    [DataRow("null", "invalid_body")]
    [DataRow("{}", "invalid_sku")]
    [DataRow("{\"quantity\":1}", "invalid_sku")]
    [DataRow("{\"sku\":null,\"quantity\":1}", "invalid_sku")]
    [DataRow("{\"sku\":\"\",\"quantity\":1}", "invalid_sku")]
    [DataRow("{\"sku\":\"bad sku\",\"quantity\":1}", "invalid_sku")]
    [DataRow("{\"sku\":\"DEMO-KEYBOARD\"}", "invalid_quantity")]
    [DataRow("{\"sku\":\"DEMO-KEYBOARD\",\"quantity\":null}", "invalid_quantity")]
    [DataRow("{\"sku\":\"DEMO-KEYBOARD\",\"quantity\":0}", "invalid_quantity")]
    [DataRow("{\"sku\":\"DEMO-KEYBOARD\",\"quantity\":101}", "invalid_quantity")]
    [DataRow("{\"sku\":\"DEMO-KEYBOARD\",\"quantity\":-1}", "invalid_quantity")]
    [DataRow("{\"sku\":\"DEMO-KEYBOARD\",\"quantity\":1.5}", "invalid_json")]
    [DataRow("{\"sku\":\"DEMO-KEYBOARD\",\"quantity\":\"1\"}", "invalid_json")]
    [DataRow("{\"sku\":\"DEMO-KEYBOARD\",\"quantity\":\"bad\"}", "invalid_json")]
    [DataRow("{\"sku\":\"DEMO-KEYBOARD\",\"quantity\":true}", "invalid_json")]
    [DataRow("{\"sku\":123,\"quantity\":1}", "invalid_json")]
    [DataRow("{\"sku\":\"DEMO-KEYBOARD\",\"quantity\":1,\"extra\":true}", "invalid_json")]
    public async Task InvalidBodiesReturnProblemDetailsAndDoNotCreateOrders(string json, string code)
    {
        var host = await StartAsync();
        using var request = Request(HttpMethod.Post, "/api/orders");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await host.Client.SendAsync(request);
        await AssertProblem(response, HttpStatusCode.BadRequest, code);
        Assert.AreEqual(0, (await List(host.Client)).Length);
    }

    [TestMethod, TestCategory("Validation")]
    [DataRow("X-Customer-Id", null)]
    [DataRow("X-Customer-Id", "")]
    [DataRow("X-Customer-Id", "customer,b")]
    [DataRow("X-Customer-Id", "../customer")]
    [DataRow("X-Customer-Id", "a b")]
    [DataRow("X-Customer-Id", "müşteri")]
    [DataRow("Idempotency-Key", null)]
    [DataRow("Idempotency-Key", "")]
    [DataRow("Idempotency-Key", "key,other")]
    [DataRow("Idempotency-Key", "-key")]
    public async Task InvalidHeadersAreRejected(string header, string? value)
    {
        var host = await StartAsync();
        using var request = Request(HttpMethod.Post, "/api/orders");
        request.Headers.Remove(header);
        if (value is not null) request.Headers.TryAddWithoutValidation(header, value);
        request.Content = JsonContent.Create(new { sku = "DEMO-KEYBOARD", quantity = 1 });
        using var response = await host.Client.SendAsync(request);
        await AssertProblem(response, HttpStatusCode.BadRequest,
            header == "X-Customer-Id" ? "invalid_customer_id" : "invalid_idempotency_key");
        Assert.AreEqual(0, (await List(host.Client)).Length);
    }

    [TestMethod, TestCategory("Validation")]
    [DataRow("X-Customer-Id", 65)]
    [DataRow("Idempotency-Key", 129)]
    public async Task OversizedAndRepeatedHeadersAreRejected(string header, int length)
    {
        var host = await StartAsync();
        foreach (var values in new[] { new[] { new string('a', length) }, new[] { "first", "second" } })
        {
            using var request = Request(HttpMethod.Post, "/api/orders");
            request.Headers.Remove(header);
            request.Headers.TryAddWithoutValidation(header, values);
            request.Content = JsonContent.Create(new { sku = "DEMO-KEYBOARD", quantity = 1 });
            using var response = await host.Client.SendAsync(request);
            await AssertProblem(response, HttpStatusCode.BadRequest,
                header == "X-Customer-Id" ? "invalid_customer_id" : "invalid_idempotency_key");
        }
        Assert.AreEqual(0, (await List(host.Client)).Length);
    }

    [TestMethod, TestCategory("Validation")]
    public async Task BoundaryValuesAreAcceptedAndOversizedSkuIsRejected()
    {
        var host = await StartAsync();
        using var valid = await Post(host.Client, new string('c', 64), new string('k', 128), new string('s', 64), 100);
        Assert.AreEqual(HttpStatusCode.Created, valid.StatusCode);
        using var invalid = await Post(host.Client, sku: new string('s', 65));
        await AssertProblem(invalid, HttpStatusCode.BadRequest, "invalid_sku");
    }

    [TestMethod, TestCategory("Validation")]
    public async Task UnsupportedContentTypeAndOversizedBodyHaveErrorCodes()
    {
        var host = await StartAsync();
        using var textRequest = Request(HttpMethod.Post, "/api/orders");
        textRequest.Content = new StringContent("{}");
        using var textResponse = await host.Client.SendAsync(textRequest);
        await AssertProblem(textResponse, HttpStatusCode.UnsupportedMediaType, "unsupported_media_type");
        using var largeRequest = Request(HttpMethod.Post, "/api/orders");
        largeRequest.Content = JsonContent.Create(new { sku = new string('x', 20_000), quantity = 1 });
        using var largeResponse = await host.Client.SendAsync(largeRequest);
        await AssertProblem(largeResponse, HttpStatusCode.RequestEntityTooLarge, "request_too_large");
        Assert.AreEqual(0, (await List(host.Client)).Length);
    }

    [TestMethod, TestCategory("Validation")]
    public async Task ApiReadsRequireCustomerAndMissingResourcesHaveErrorCodes()
    {
        var host = await StartAsync();
        using var noHeader = await host.Client.GetAsync("/api/orders");
        await AssertProblem(noHeader, HttpStatusCode.BadRequest, "invalid_customer_id");
        using var missing = await Get(host.Client, $"/api/orders/{Guid.NewGuid():D}");
        await AssertProblem(missing, HttpStatusCode.NotFound, "order_not_found");
        using var invalid = await Get(host.Client, "/api/orders/not-an-id");
        await AssertProblem(invalid, HttpStatusCode.BadRequest, "invalid_order_id");
        using var unknown = await host.Client.GetAsync("/not-a-route");
        await AssertProblem(unknown, HttpStatusCode.NotFound, "not_found");
    }

    [TestMethod, TestCategory("Validation")]
    public async Task HealthDoesNotRequireSyntheticIdentity()
    {
        var host = await StartAsync();
        using var response = await host.Client.GetAsync("/health");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual("ok", body.RootElement.GetProperty("status").GetString());
        Assert.AreEqual(Environment.GetEnvironmentVariable("ORDERS_TEST_MODE") ?? "fixed",
            body.RootElement.GetProperty("mode").GetString());
    }

    [TestMethod, TestCategory("Validation")]
    [DataRow("duplicate")]
    [DataRow("memory")]
    [DataRow("payload")]
    [DataRow("unknown")]
    public void UnsafeOrUnknownModesAreRejectedOutsideDevelopment(string mode)
    {
        Assert.ThrowsException<InvalidOperationException>(() => OrdersApplication.Build(
            ["--environment", "Production", "--Orders:Mode", mode, "--Orders:DatabasePath", DatabasePath]));
        Assert.IsFalse(File.Exists(DatabasePath), "The guard must run before touching the database.");
    }

    private async Task<RunningHost> StartAsync(RequestRendezvous? gate = null)
    {
        var app = OrdersApplication.Build([
            "--environment", "Development",
            "--urls", "http://127.0.0.1:0",
            "--Orders:DatabasePath", DatabasePath,
            "--Orders:Mode", Environment.GetEnvironmentVariable("ORDERS_TEST_MODE") ?? "fixed",
            "--Logging:LogLevel:Default", "Error"
        ]);
        if (gate is not null)
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == "POST" && context.Request.Path == "/api/orders")
                    await gate.ArriveAsync(context.RequestAborted);
                await next(context);
            });
        await app.StartAsync();
        var host = new RunningHost(app, new HttpClient(new SocketsHttpHandler
        {
            RequestHeaderEncodingSelector = (_, _) => Encoding.UTF8
        })
        {
            BaseAddress = new Uri(app.Urls.Single()),
            Timeout = TimeSpan.FromSeconds(45)
        });
        _hosts.Add(host);
        return host;
    }

    private static async Task AssertConcurrentReplay(HttpClient[] clients, RequestRendezvous gate)
    {
        var responses = await Task.WhenAll(clients.Select(client => Post(client)));
        try
        {
            Assert.AreEqual(clients.Length, gate.Arrivals,
                "Every HTTP request must reach the server-side gate before any is released.");
            Assert.AreEqual(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
            Assert.AreEqual(clients.Length - 1, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
            var orders = await Task.WhenAll(responses.Select(Order));
            Assert.AreEqual(1, orders.Select(order => order.Id).Distinct().Count());
            Assert.AreEqual(1, (await List(clients[0])).Length);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    private static HttpRequestMessage Request(HttpMethod method, string path, string customer = "demo-customer",
        string key = "retry-key")
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("X-Customer-Id", customer);
        if (method == HttpMethod.Post) request.Headers.Add("Idempotency-Key", key);
        return request;
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string customer = "demo-customer",
        string key = "retry-key", string sku = "DEMO-KEYBOARD", int quantity = 1)
    {
        using var request = Request(HttpMethod.Post, "/api/orders", customer, key);
        request.Content = JsonContent.Create(new { sku, quantity });
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> Get(HttpClient client, string path,
        string customer = "demo-customer")
    {
        using var request = Request(HttpMethod.Get, path, customer);
        return await client.SendAsync(request);
    }

    private static async Task<OrderResponse> Order(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    private static async Task<OrderResponse[]> List(HttpClient client, string customer = "demo-customer")
    {
        using var response = await Get(client, "/api/orders", customer);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse[]>())!;
    }

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.AreEqual(status, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.AreEqual("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.AreEqual((int)status, body.RootElement.GetProperty("status").GetInt32());
        Assert.AreEqual(code, body.RootElement.GetProperty("code").GetString());
    }

    private sealed class RunningHost(WebApplication app, HttpClient client) : IAsyncDisposable
    {
        private bool _disposed;
        public HttpClient Client => client;

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private sealed class RequestRendezvous(int expected)
    {
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public int Arrivals => Volatile.Read(ref _arrivals);

        public async Task ArriveAsync(CancellationToken cancellationToken)
        {
            // Proves all N real HTTP requests are simultaneously in flight at a server-side gate.
            // It does not claim simultaneous SQLite statements: SQLite intentionally serializes writers.
            if (Interlocked.Increment(ref _arrivals) == expected) _release.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
        }
    }
}
