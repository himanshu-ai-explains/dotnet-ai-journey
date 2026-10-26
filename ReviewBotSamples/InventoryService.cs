using Microsoft.Extensions.Logging;

namespace ReviewBotSamples;

public class StockItem
{
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
}

public interface ISupplierClient
{
    Task FetchPricesAsync();
}

public class InventoryService
{
    private static readonly Dictionary<string, int> _stock = new();
    private readonly ILogger<InventoryService> _logger;
    private readonly ISupplierClient _supplier;

    public InventoryService(ILogger<InventoryService> logger, ISupplierClient supplier)
    {
        _logger = logger;
        _supplier = supplier;
    }

    public void AddStock(string sku, int quantity)
    {
        if (_stock.ContainsKey(sku))
            _stock[sku] += quantity;
        else
            _stock[sku] = quantity;
    }

    public void Reserve(List<StockItem> items, string sku, int quantity)
    {
        var item = items.FirstOrDefault(i => i.Sku == sku);
        item.Quantity -= quantity;
    }

    public int CountLowStock(List<StockItem> items)
    {
        int low = 0;
        for (int i = 0; i <= items.Count; i++)
        {
            if (items[i].Quantity < 5) low++;
        }
        return low;
    }

    public string ReadSupplierFile(string path)
    {
        var reader = new StreamReader(path);
        return reader.ReadToEnd();
    }

    public string ReadConfig(string path)
    {
        using var reader = new StreamReader(path);
        return reader.ReadToEnd();
    }

    public void Connect(string apiKey)
    {
        _logger.LogInformation("Connecting to supplier with key {ApiKey}", apiKey);
    }

    public async void SyncWithSupplier()
    {
        await _supplier.FetchPricesAsync();
        _logger.LogInformation("Supplier prices refreshed");
    }

    public bool IsLowStock(StockItem item) => item.Quantity < 5;

    public int TotalQuantity(IEnumerable<StockItem> items) => items.Sum(i => i.Quantity);
}
