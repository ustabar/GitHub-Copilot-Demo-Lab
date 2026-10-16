using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics;
using Orders.Api.Checkpoints;

namespace Orders.Api;

public static class OrdersApplication
{
    private const string CustomerItem = "Orders.DemoCustomer";
    private static readonly JsonSerializerOptions RequestJson = new(JsonSerializerDefaults.Web)
    {
        NumberHandling = JsonNumberHandling.Strict,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        if (string.IsNullOrEmpty(builder.Configuration["urls"]))
            builder.WebHost.UseUrls("http://127.0.0.1:5100");
        builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16 * 1024);

        var mode = builder.Configuration["Orders:Mode"] ?? "fixed";
        if (mode is not ("fixed" or "duplicate" or "memory" or "payload"))
            throw new InvalidOperationException("Orders:Mode must be fixed, duplicate, memory, or payload.");
        if (mode != "fixed" && !builder.Environment.IsDevelopment())
            throw new InvalidOperationException("Intentionally broken checkpoints are Development-only.");

        var databasePath = builder.Configuration["Orders:DatabasePath"]
            ?? Path.Combine(AppContext.BaseDirectory, "data", "orders.db");
        var database = new SqliteOrderDatabase(databasePath);
        builder.Services.AddSingleton(database);
        builder.Services.AddSingleton<IOrderWriter>(mode switch
        {
            "duplicate" => new DuplicateCheckpointWriter(database),
            "memory" => new MemoryCheckpointWriter(database),
            "payload" => new PayloadCheckpointWriter(database),
            _ => new FixedOrderWriter(database)
        });
        builder.Services.AddProblemDetails();

        var app = builder.Build();
        app.Logger.LogWarning(
            "LOCAL DEMO ONLY: X-Customer-Id is caller-supplied synthetic identity, NOT authentication. Mode: {Mode}",
            mode);
        if (mode != "fixed")
            app.Logger.LogWarning("INTENTIONALLY BROKEN Development checkpoint enabled: {Mode}", mode);

        app.UseExceptionHandler(handler => handler.Run(async context =>
        {
            // The exception-handler middleware logs the original exception; do not expose internals.
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var tooLarge = exception is BadHttpRequestException { StatusCode: 413 };
            await ApiErrors.Problem(tooLarge ? 413 : 500,
                tooLarge ? "request_too_large" : "internal_error",
                tooLarge ? "The request body exceeds the demo limit." : "An unexpected server error occurred.")
                .ExecuteAsync(context);
        }));
        app.UseStatusCodePages(async context =>
        {
            var status = context.HttpContext.Response.StatusCode;
            await ApiErrors.Problem(status,
                status == 404 ? "not_found" : status == 405 ? "method_not_allowed" : "http_error",
                "The request could not be completed.").ExecuteAsync(context.HttpContext);
        });
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                // A teaching aid for scoping data, never a security/authentication boundary.
                if (!ReadSafeHeader(context.Request, "X-Customer-Id", 64, out var customer))
                {
                    await ApiErrors.Problem(400, "invalid_customer_id",
                        "Supply one X-Customer-Id with 1–64 ASCII letters, digits, dots, underscores or hyphens; start with a letter or digit.")
                        .ExecuteAsync(context);
                    return;
                }
                context.Items[CustomerItem] = customer;
            }
            await next(context);
        });

        app.MapGet("/health", () => Results.Ok(new { status = "ok", mode }));
        app.MapPost("/api/orders", CreateOrder);
        app.MapGet("/api/orders", (HttpContext context, SqliteOrderDatabase store) =>
            Results.Ok(store.List(Customer(context))));
        app.MapGet("/api/orders/{id}", (string id, HttpContext context, SqliteOrderDatabase store) =>
        {
            if (!Guid.TryParseExact(id, "D", out var parsed))
                return ApiErrors.Problem(400, "invalid_order_id", "Order IDs must be GUIDs.");
            var order = store.Find(Customer(context), parsed.ToString("D"));
            return order is null
                ? ApiErrors.Problem(404, "order_not_found", "No order was found for this customer.")
                : Results.Ok(order);
        });
        app.MapFallback(() => ApiErrors.Problem(404, "not_found", "No endpoint matches this request."));
        return app;
    }

    private static async Task<IResult> CreateOrder(HttpContext context, IOrderWriter writer)
    {
        if (!ReadSafeHeader(context.Request, "Idempotency-Key", 128, out var key))
            return ApiErrors.Problem(400, "invalid_idempotency_key",
                "Supply one Idempotency-Key with 1–128 ASCII letters, digits, dots, underscores or hyphens; start with a letter or digit.");
        if (!context.Request.HasJsonContentType())
            return ApiErrors.Problem(415, "unsupported_media_type", "Use Content-Type: application/json.");

        CreateOrderRequest? request;
        try
        {
            request = await context.Request.ReadFromJsonAsync<CreateOrderRequest>(
                RequestJson, context.RequestAborted);
        }
        catch (JsonException)
        {
            return ApiErrors.Problem(400, "invalid_json", "Supply a valid JSON order object.");
        }
        catch (BadHttpRequestException exception) when (exception.StatusCode is 400 or 413)
        {
            return ApiErrors.Problem(exception.StatusCode,
                exception.StatusCode == 413 ? "request_too_large" : "invalid_body",
                "The request body could not be read within the demo limits.");
        }
        if (request is null)
            return ApiErrors.Problem(400, "invalid_body", "A JSON order object is required.");
        if (!IsSafeIdentifier(request.Sku, 64))
            return ApiErrors.Problem(400, "invalid_sku",
                "SKU must be 1–64 ASCII letters, digits, dots, underscores or hyphens; start with a letter or digit.");
        if (request.Quantity is not (>= 1 and <= 100))
            return ApiErrors.Problem(400, "invalid_quantity", "Quantity must be an integer from 1 to 100.");

        var submission = writer.Submit(Customer(context), key, request.Sku!, request.Quantity.Value);
        if (submission.IsConflict)
            return ApiErrors.Problem(409, "idempotency_conflict",
                "This customer's idempotency key was already used with a different SKU or quantity.");
        return submission.IsReplay
            ? Results.Ok(submission.Order)
            : Results.Created($"/api/orders/{submission.Order.Id}", submission.Order);
    }

    private static string Customer(HttpContext context) => (string)context.Items[CustomerItem]!;

    private static bool ReadSafeHeader(HttpRequest request, string name, int maxLength, out string value)
    {
        var values = request.Headers[name];
        value = values.Count == 1 ? values[0] ?? "" : "";
        return IsSafeIdentifier(value, maxLength);
    }

    private static bool IsSafeIdentifier(string? value, int maxLength) =>
        value is { Length: > 0 } && value.Length <= maxLength && char.IsAsciiLetterOrDigit(value[0])
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-');
}
