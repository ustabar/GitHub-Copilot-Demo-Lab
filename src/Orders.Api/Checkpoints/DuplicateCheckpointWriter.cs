namespace Orders.Api.Checkpoints;

// INTENTIONALLY BROKEN, Development-only: ignores the key and creates an order on every retry.
internal sealed class DuplicateCheckpointWriter(SqliteOrderDatabase database) : IOrderWriter
{
    public OrderSubmission Submit(string customer, string key, string sku, int quantity) =>
        new(database.InsertWithoutIdempotency(customer, sku, quantity), false);
}
