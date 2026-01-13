using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PQC.MODULES.Algorithm.Application.Services;
using PQC.MODULES.Algorithm.Application.Services.UseCases;
using System.Security.Claims;

namespace PQC.API.Controllers.Algorithm
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignatureController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public SignatureController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("sign/{Id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SignDocument([FromRoute] Guid Id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim);

            var algorithmConfig = _configuration.GetSection("Algorithm");
            var executor = new AlgorithmExecutor(
                algorithmConfig["ExecutablePath"],
                algorithmConfig["TempDirectory"]
            );

            var useCase = new SignDocumentUseCase(executor);
            await useCase.Execute(Id, userId);

            return Ok(new { message = "Document signed successfully" });
        }
    }
    
}
