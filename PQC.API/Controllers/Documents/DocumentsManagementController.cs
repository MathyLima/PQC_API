using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PQC.API.Models;
using PQC.MODULES.Documents.Application.DTOs;
using PQC.MODULES.Documents.Application.UseCases.Sign;
using PQC.MODULES.Documents.Application.UseCases.Validation;

namespace PQC.API.Controllers.Documents
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
     
            private readonly SignDocumentUseCase _signDocumentUseCase;
            private readonly ValidateDocumentUseCase _validateDocumentUseCase;

        public DocumentsController(
                SignDocumentUseCase signDocumentUseCase,
                ValidateDocumentUseCase validateUseCase
              )
            {
                _signDocumentUseCase = signDocumentUseCase;
                _validateDocumentUseCase = validateUseCase;
        }

        [HttpPost("sign")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        //  [ProducesResponseType(typeof(ResponseErrorMessagesJson), StatusCodes.Status400BadRequest)]
        //  [ProducesResponseType(typeof(ResponseErrorMessagesJson), StatusCodes.Status401Unauthorized)]
        //[Authorize]
        public async Task<IActionResult> SignDocument([FromForm] CreateDocumentRequestJson request)
        {
            byte[] content;
            using (var ms = new MemoryStream())
            {
                await request.File!.CopyToAsync(ms);
                content = ms.ToArray();
            }

            var useCaseInput = new DocumentUploadRequest
            {
                UserId = request.UserId!,
                Content = content,
                FileName = request.FileName,
                ContentType = request.File.ContentType,
            };

            var response = await _signDocumentUseCase.Execute(useCaseInput);

            // Retorna o arquivo assinado para download
            return File(
                response.SignedContent,
                response.ContentType,
                response.DocumentName
            );
        }

        [HttpPost("verify")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> VerifyDocument([FromForm] VerifyDocumentRequestJson request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Nenhum arquivo foi enviado");

            byte[] content;
            using (var ms = new MemoryStream())
            {
                await request.File.CopyToAsync(ms);
                content = ms.ToArray();
            }

            var response = await _validateDocumentUseCase.Execute(content);

            return Ok(response);
        }
        /*
        [HttpGet]
        [ProducesResponseType(typeof(DocumentListResponseJson), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorMessagesJson),StatusCodes.Status401Unauthorized)]
        public IActionResult List()
        {

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var useCase = new ListDocumentsUseCase();
            var response = useCase.Execute(userIdClaim);

            return Ok(response);
        }
        */
        /*
        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(typeof(DocumentResponseJson),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseErrorMessagesJson),StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseErrorMessagesJson),StatusCodes.Status401Unauthorized)]
        public IActionResult Download([FromRoute]Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(userIdClaim);
            var useCase = new GetDocumentByIdUseCase();
            var document = useCase.Execute(id.ToString(), userId.ToString());

            return File(document.Content, document.ContentType, document.Nome);
        }
        */
        /*
        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Delete([FromRoute]Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(userIdClaim);
            var useCase = new DeleteDocumentUseCase();
            useCase.Execute(id.ToString(), userId.ToString());

            return NoContent();
        }
        */
    }
}