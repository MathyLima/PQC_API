using Microsoft.AspNetCore.Mvc;
using PQC.MODULES.Infraestructure.Data;

[ApiController]
[Route("api/test")]
public class TestController : ControllerBase
{
    private readonly AppDbContext _context;

    public TestController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("db")]
    public async Task<IActionResult> TestDb()
    {
        var canConnect = await _context.Database.CanConnectAsync();
        return Ok(new { connected = canConnect });
    }
}
