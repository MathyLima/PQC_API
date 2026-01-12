using Microsoft.AspNetCore.Mvc;
using PQC.COMMUNICATION.Requests.Users.Create;
using PQC.COMMUNICATION.Requests.Users.List;
using PQC.COMMUNICATION.Requests.Users.Update;
using PQC.COMMUNICATION.Responses;
using PQC.COMMUNICATION.Responses.Users;
using PQC.MODULES.Users.Application.Services.UseCases.Create;
using PQC.MODULES.Users.Application.Services.UseCases.Delete;
using PQC.MODULES.Users.Application.Services.UseCases.List;
using PQC.MODULES.Users.Application.Services.UseCases.Update;

namespace PQC.API.Controllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersManagementController : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(typeof(UserListResponseJson),StatusCodes.Status200OK )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult GetAll([FromQuery] UserListRequestJson? request) {
            var useCase = new ListUsersUseCase();
            var response = useCase.Execute(request);

            return Ok(request);
        }

        [HttpGet]
        [Route("{Id}")]
        [ProducesResponseType(typeof(UserResponseJson), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult GetById([FromRoute]Guid Id)
        {
            var useCase = new GetUserByIdUseCase();
            var response = useCase.Execute(Id);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(UserResponseJson), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseErrorMessagesJson),StatusCodes.Status400BadRequest)]
        public IActionResult Create([FromBody]CreateUserRequestJson request)
        {
            var useCase = new CreateUserUseCase();
            var response = useCase.Execute(request);
            return Created(string.Empty, response);
        }

        [HttpPut]
        [Route("{id}")]
        [ProducesResponseType(typeof(UserResponseJson), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Update([FromRoute]Guid id, [FromBody] UpdateUserRequestJson request)
        {
            var useCase = new UpdateUserUseCase();
            var response = useCase.Execute(id, request);
            return Ok(response);
        }

        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Delete([FromRoute]Guid id)
        {
            var useCase = new DeleteUserUseCase();
            useCase.Execute(id);
            return NoContent(); 
        }


    }
}
