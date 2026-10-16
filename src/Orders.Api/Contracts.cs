using Microsoft.AspNetCore.Mvc;

namespace Orders.Api;

public sealed record CreateOrderRequest(string? Sku, int? Quantity);

public sealed record OrderResponse(
    string Id, string CustomerId, string Sku, int Quantity, DateTimeOffset CreatedAt);

internal sealed record OrderSubmission(OrderResponse Order, bool IsReplay, bool IsConflict = false);

internal static class ApiErrors
{
    public static IResult Problem(int status, string code, string detail) =>
        Results.Problem(Create(status, code, detail));

    public static ProblemDetails Create(int status, string code, string detail) => new()
    {
        Status = status,
        Title = status switch
        {
            400 => "Invalid request",
            404 => "Not found",
            405 => "Method not allowed",
            409 => "Idempotency conflict",
            413 => "Request too large",
            415 => "Unsupported media type",
            _ => "Request failed"
        },
        Detail = detail,
        Extensions = { ["code"] = code }
    };
}
