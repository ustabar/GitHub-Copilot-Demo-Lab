namespace Orders.Api.Checkpoints;

// INTENTIONALLY BROKEN, Development-only: durable replay silently accepts a changed payload.
internal sealed class PayloadCheckpointWriter(SqliteOrderDatabase database) : IOrderWriter
{
    public OrderSubmission Submit(string customer, string key, string sku, int quantity) =>
        database.SubmitAtomic(customer, key, sku, quantity, comparePayload: false);
}
