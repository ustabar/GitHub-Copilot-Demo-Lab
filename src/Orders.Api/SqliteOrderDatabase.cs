using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Orders.Api;

internal sealed class SqliteOrderDatabase
{
    private readonly string _connectionString;

    public SqliteOrderDatabase(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == ":memory:")
            throw new InvalidOperationException("Orders:DatabasePath must name a persistent local SQLite file.");
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            Pooling = false,
            DefaultTimeout = 30
        }.ToString();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            CREATE TABLE IF NOT EXISTS orders (
                id TEXT NOT NULL PRIMARY KEY,
                customer_id TEXT NOT NULL,
                sku TEXT NOT NULL,
                quantity INTEGER NOT NULL CHECK (quantity BETWEEN 1 AND 100),
                created_at TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_orders_customer ON orders(customer_id);
            CREATE TABLE IF NOT EXISTS idempotency_records (
                customer_id TEXT NOT NULL,
                idempotency_key TEXT NOT NULL,
                order_id TEXT NOT NULL REFERENCES orders(id),
                PRIMARY KEY (customer_id, idempotency_key)
            );
            """;
        command.ExecuteNonQuery();
    }

    public OrderSubmission SubmitAtomic(string customer, string key, string sku, int quantity,
        bool comparePayload = true)
    {
        using var connection = Open();
        // BEGIN IMMEDIATE serializes writers across connections AND processes. The durable unique
        // key and the order are committed together; no in-process cache is authoritative.
        using var transaction = connection.BeginTransaction(deferred: false);
        using var lookup = connection.CreateCommand();
        lookup.Transaction = transaction;
        lookup.CommandText = """
            SELECT o.id, o.customer_id, o.sku, o.quantity, o.created_at
            FROM idempotency_records i JOIN orders o ON o.id = i.order_id
            WHERE i.customer_id = $customer AND i.idempotency_key = $key;
            """;
        lookup.Parameters.AddWithValue("$customer", customer);
        lookup.Parameters.AddWithValue("$key", key);
        OrderResponse? existing;
        using (var reader = lookup.ExecuteReader())
            existing = reader.Read() ? ReadOrder(reader) : null;
        if (existing is not null)
        {
            transaction.Commit();
            return new(existing, true,
                comparePayload && (existing.Sku != sku || existing.Quantity != quantity));
        }

        var order = Insert(connection, transaction, customer, sku, quantity);
        using var remember = connection.CreateCommand();
        remember.Transaction = transaction;
        remember.CommandText = """
            INSERT INTO idempotency_records(customer_id, idempotency_key, order_id)
            VALUES ($customer, $key, $order);
            """;
        remember.Parameters.AddWithValue("$customer", customer);
        remember.Parameters.AddWithValue("$key", key);
        remember.Parameters.AddWithValue("$order", order.Id);
        remember.ExecuteNonQuery();
        transaction.Commit();
        return new(order, false);
    }

    public OrderResponse InsertWithoutIdempotency(string customer, string sku, int quantity)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        var order = Insert(connection, transaction, customer, sku, quantity);
        transaction.Commit();
        return order;
    }

    public IReadOnlyList<OrderResponse> List(string customer)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, customer_id, sku, quantity, created_at FROM orders
            WHERE customer_id = $customer ORDER BY created_at, id;
            """;
        command.Parameters.AddWithValue("$customer", customer);
        using var reader = command.ExecuteReader();
        var orders = new List<OrderResponse>();
        while (reader.Read()) orders.Add(ReadOrder(reader));
        return orders;
    }

    public OrderResponse? Find(string customer, string id)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, customer_id, sku, quantity, created_at FROM orders
            WHERE customer_id = $customer AND id = $id;
            """;
        command.Parameters.AddWithValue("$customer", customer);
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ReadOrder(reader) : null;
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static OrderResponse Insert(SqliteConnection connection, SqliteTransaction transaction,
        string customer, string sku, int quantity)
    {
        var order = new OrderResponse(Guid.NewGuid().ToString("D"), customer, sku, quantity, DateTimeOffset.UtcNow);
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO orders(id, customer_id, sku, quantity, created_at)
            VALUES ($id, $customer, $sku, $quantity, $created);
            """;
        command.Parameters.AddWithValue("$id", order.Id);
        command.Parameters.AddWithValue("$customer", customer);
        command.Parameters.AddWithValue("$sku", sku);
        command.Parameters.AddWithValue("$quantity", quantity);
        command.Parameters.AddWithValue("$created", order.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
        return order;
    }

    private static OrderResponse ReadOrder(SqliteDataReader reader) => new(
        reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3),
        DateTimeOffset.Parse(reader.GetString(4), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
}
