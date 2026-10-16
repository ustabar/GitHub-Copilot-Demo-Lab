namespace Orders.Api;

internal interface IOrderWriter
{
    OrderSubmission Submit(string customer, string key, string sku, int quantity);
}

internal sealed class FixedOrderWriter(SqliteOrderDatabase database) : IOrderWriter
{
    public OrderSubmission Submit(string customer, string key, string sku, int quantity) =>
        database.SubmitAtomic(customer, key, sku, quantity);
}
