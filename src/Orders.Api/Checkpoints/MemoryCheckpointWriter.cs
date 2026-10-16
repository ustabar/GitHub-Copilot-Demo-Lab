namespace Orders.Api.Checkpoints;

// INTENTIONALLY BROKEN, Development-only: orders survive, but the per-host map does not.
// The lock makes replay correct within this host; it cannot coordinate another host/process.
internal sealed class MemoryCheckpointWriter(SqliteOrderDatabase database) : IOrderWriter
{
    private readonly object _gate = new();
    private readonly Dictionary<(string Customer, string Key), OrderResponse> _orders = new();

    public OrderSubmission Submit(string customer, string key, string sku, int quantity)
    {
        lock (_gate)
        {
            if (_orders.TryGetValue((customer, key), out var existing))
                return new(existing, true, existing.Sku != sku || existing.Quantity != quantity);
            var order = database.InsertWithoutIdempotency(customer, sku, quantity);
            _orders.Add((customer, key), order);
            return new(order, false);
        }
    }
}
