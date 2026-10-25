using System.Data.SqlClient;

namespace ReviewBotSamples;

public class OrderService
{
    private const string ConnectionString =
        "Server=prod-db.internal;Database=Shop;User Id=sa;Password=P@ssw0rd123;";

    private readonly IHttpClientFactory _httpFactory;

    public OrderService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    public async Task<string?> GetOrderAsync(string orderId)
    {
        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand("SELECT * FROM Orders WHERE Id = '" + orderId + "'", conn);
        using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? reader.GetString(0) : null;
    }

    public string GetExchangeRate(string currency)
    {
        var client = new HttpClient();
        return client.GetStringAsync("https://api.example.com/rates/" + currency).Result;
    }

    public async Task SaveOrderAsync(string orderJson)
    {
        try
        {
            await File.WriteAllTextAsync("orders.json", orderJson);
            WriteAuditLogAsync(orderJson);
        }
        catch (Exception)
        {
        }
    }

    private static async Task WriteAuditLogAsync(string entry)
    {
        await File.AppendAllTextAsync("audit.log", entry + Environment.NewLine);
    }

    public static decimal ApplyDiscount(decimal total, decimal percent)
    {
        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent));

        return total - (total * percent / 100);
    }
}
