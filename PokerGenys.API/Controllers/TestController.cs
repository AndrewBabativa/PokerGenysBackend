using MongoDB.Driver;
using Microsoft.AspNetCore.Mvc;
using PokerGenys.Infrastructure.Data;
using PokerGenys.Domain.Models;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly MongoContext _context;

    public TestController(MongoContext context)
    {
        _context = context;
    }

    [HttpGet("ping")]
    public async Task<IActionResult> Ping()
    {
        // Usamos Builders<Tournament>.Filter.Empty para "todos los documentos"
        var count = await _context.Tournaments.CountDocumentsAsync(Builders<Tournament>.Filter.Empty);

        return Ok(new { message = "MongoDB connected!", tournaments = count });
    }
}
