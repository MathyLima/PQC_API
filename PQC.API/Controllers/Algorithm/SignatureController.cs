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

        [HttpPost("sign/{DocumentId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SignDocument([FromRoute] Guid DocumentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.Parse(userIdClaim);
            var privateKeyPath = _configuration["Keys:PrivateKeyPath"];

            var algorithmConfig = _configuration.GetSection("Algorithm");
            var executor = new AlgorithmExecutor(
                algorithmConfig["ExecutablePath"],
                algorithmConfig["TempDirectory"],
                privateKeyPath
            );

            var useCase = new SignDocumentUseCase(executor);
            await useCase.Execute(DocumentId, userId);

            return Ok(new { message = "Document signed successfully" });
        }

        [HttpPost("sign-upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SignUploadedDocument(
            [FromForm] SignUploadRequest request
        )
        {
            if (request.File == null || request.File.Length == 0)
            {
                return BadRequest("File is required");
            }

            // Lê o arquivo enviado
            byte[] documentContent;
            using (var ms = new MemoryStream())
            {
                await request.File.CopyToAsync(ms);
                documentContent = ms.ToArray();
            }

            var algorithmConfig = _configuration.GetSection("Algorithm");
            var privateKeyPath = _configuration["Keys:PrivateKeyPath"];

            var executor = new AlgorithmExecutor(
                algorithmConfig["ExecutablePath"],
                algorithmConfig["TempDirectory"],
                privateKeyPath
            );

            var result = await executor.SignDocumentAsync(privateKeyPath);

            if (!result.Success)
            {
                return StatusCode(500, new { error = result.ErrorMessage });
            }

            // Retorna JSON com algoritmo e assinatura em Base64
            var response = new
            {
                algorithm = result.Algorithm ?? "unknown",
                signature = result.Signature != null ? Convert.ToBase64String(result.Signature) : null
            };

            return Ok(response);
        }

    }
}
