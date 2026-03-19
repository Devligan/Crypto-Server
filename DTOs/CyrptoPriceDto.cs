namespace CryptoDataPipeline.DTOs;

public class CryptoPriceDto
{
    public string Symbol { get; set; } = "";
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public DateTime Date { get; set; }
}