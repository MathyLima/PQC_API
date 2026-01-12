using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PQC.API.Controllers.Documents
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        private readonly IPdfService _pdfService;

        public PdfController(IPdfService pdfService)
        {
            _pdfService = pdfService;
        }

        [HttpPost("process")]
        [Authorize] // exige JWT
        public async Task<IActionResult> ProcessPdf(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Arquivo inválido");

            // chama o serviço que processa o PDF e retorna o resultado
            var resultUrl = await _pdfService.ProcessPdfAsync(file);

            return Ok(new { url = resultUrl });
        }
    }
}
