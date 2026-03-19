using Microsoft.EntityFrameworkCore;
using CryptoDataPipeline.Data;
using CryptoDataPipeline.Services;

var builder = WebApplication.CreateBuilder(args);

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseNpgsql(connStr));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SupportNonNullableReferenceTypes();
    options.UseInlineDefinitionsForEnums();
});

builder.Services.AddScoped<CoinGeckoService>();
builder.Services.AddHttpClient<CoinGeckoService>();

// Add a hosted service to run backfill after app starts
builder.Services.AddHostedService<BackfillHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Crypto Data Pipeline API v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();