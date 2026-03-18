namespace CryptoDataPipeline.Models;

public class CryptoPrice
{
    public int Id { get; set; }
    public string Symbol { get; set; } = "";
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}