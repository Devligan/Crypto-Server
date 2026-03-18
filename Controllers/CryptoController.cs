using Microsoft.AspNetCore.Mvc;
using CryptoDataPipeline.Data;
using CryptoDataPipeline.Models;

namespace CryptoDataPipeline.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CryptoController : ControllerBase
{
    private readonly AppDbContext _context;

    public CryptoController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult GetPrices()
    {
        return Ok(_context.CryptoPrices.ToList());
    }
}