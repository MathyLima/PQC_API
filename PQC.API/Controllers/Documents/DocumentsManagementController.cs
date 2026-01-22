using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PQC.COMMUNICATION.Requests.Documents.Create;
using PQC.COMMUNICATION.Responses;
using PQC.COMMUNICATION.Responses.Documents;
using PQC.MODULES.Documents.Application.Services.UseCases.Delete;
using PQC.MODULES.Documents.Application.Services.UseCases.Upload;
using System.Security.Claims;

namespace PQC.API.Controllers.Documents
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(DocumentResponseJson), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorMessagesJson), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseErrorMessagesJson),StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Upload([FromForm] CreateDocumentRequestJson request)
        { 
            byte[] content;
            using (var ms = new MemoryStream())
            {
                await request.File!.CopyToAsync(ms);
                content = ms.ToArray();
            }

            var useCaseInput = new CreateDocumentRequestJson
            {
                File = request.File,
                FileName = request.FileName,
                Name = request.Name,
                IdUsuario = request.IdUsuario
            };
            var useCase = new UploadDocumentUseCase();
            var response = await useCase.Execute(request.File, Guid.Parse(useCaseInput.IdUsuario));

            return Created(string.Empty, response);
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
    }
}