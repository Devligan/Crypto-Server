namespace CryptoDataPipeline.Models;

public class CryptoPrice
{
    public int Id { get; set; }
    public string Symbol { get; set; } = "";
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public DateTime Date { get; set; }
}