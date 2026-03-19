namespace CryptoDataPipeline.Models;

public class CryptoPrice
{
    public int Id { get; set; }
    public string Symbol { get; set; } = "";
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public DateOnly Date { get; set; }
}