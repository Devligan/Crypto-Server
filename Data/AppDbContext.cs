using Microsoft.EntityFrameworkCore;
using CryptoDataPipeline.Models;
namespace CryptoDataPipeline.Data;

public class AppDbContext : DbContext {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
    }

    public DbSet<CryptoPrice> CryptoPrices { get; set; }
}