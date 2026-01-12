using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PQC.COMMUNICATION.Responses.Documents;
using PQC.MODULES.Documents.Application.Services.UseCases.Delete;
using PQC.MODULES.Documents.Application.Services.UseCases.GetById;
using PQC.MODULES.Documents.Application.Services.UseCases.List;
using PQC.MODULES.Documents.Application.Services.UseCases.Upload;

namespace PQC.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        [HttpPost]
        [ProducesResponseType(typeof(DocumentResponseJson), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(userIdClaim);
            var useCase = new UploadDocumentUseCase();
            var response = await useCase.Execute(file, userId);

            return Created(string.Empty, response);
        }

        [HttpGet]
        [ProducesResponseType(typeof(DocumentListResponseJson), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult List()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(userIdClaim);
            var useCase = new ListDocumentsUseCase();
            var response = useCase.Execute(userId);

            return Ok(response);
        }

        [HttpGet]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Download([FromRoute]Guid id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var userId = Guid.Parse(userIdClaim);
            var useCase = new GetDocumentByIdUseCase();
            var document = useCase.Execute(id, userId);

            return File(document.Content, document.ContentType, document.FileName);
        }

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
            useCase.Execute(id, userId);

            return NoContent();
        }
    }
}