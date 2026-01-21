using Microsoft.AspNetCore.Mvc;
using PQC.MODULES.Infraestructure.Data;

namespace PQC.API.Controllers.Teste
{
    [ApiController]
    [Route("api/test")]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]   
        public async Task<IActionResult> TestDb()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                return Ok(new { connected = canConnect });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = true,
                    message = ex.Message,
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("raw")]
        public async Task<IActionResult> TestRaw()
        {
            try
            {
                await using var conn = new MySqlConnector.MySqlConnection(
                    "Server=srv1660.hstgr.io;Port=3306;Database=u326530225_pqc_api;Uid=u326530225_pqc;Pwd=CPjx9Yu:;"
                );

                await conn.OpenAsync();

                return Ok("Conectou de verdade");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


    }



}
