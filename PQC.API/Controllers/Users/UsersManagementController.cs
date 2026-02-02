using Microsoft.AspNetCore.Mvc;
using PQC.MODULES.Users.Application.UseCases.Create;
using PQC.MODULES.Users.Application.UseCases.Delete;
using PQC.MODULES.Users.Application.UseCases.List;
using PQC.MODULES.Users.Application.UseCases.Update;
using PQC.SHARED.Communication.DTOs.Users.Requests;
using PQC.SHARED.Communication.DTOs.Users.Responses;

namespace PQC.API.Controllers.Users
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersManagementController : ControllerBase
    {
        private readonly CreateUserUseCase _createUserUseCase;
        private readonly UpdateUserUseCase _updateUserUseCase;
        private readonly ListUsersUseCase _listUsersUseCase;
        private readonly GetUserByIdUseCase _getUserByIdUseCase;
        private readonly DeleteUserUseCase _deleteUserUseCase;

        public UsersManagementController(
            CreateUserUseCase createUserUseCase,
            UpdateUserUseCase updateUserUseCase,
            ListUsersUseCase listUsersUseCase,
            GetUserByIdUseCase getUserByIdUseCase,
            DeleteUserUseCase deleteUserUseCase)
        {
            _createUserUseCase = createUserUseCase;
            _updateUserUseCase = updateUserUseCase;
            _listUsersUseCase = listUsersUseCase;
            _getUserByIdUseCase = getUserByIdUseCase;
            _deleteUserUseCase = deleteUserUseCase;
        }


        [HttpGet]
        [ProducesResponseType(typeof(ShortUsersListResponse),StatusCodes.Status200OK )]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetAll() {
            var response = await _listUsersUseCase.Execute();
            return Ok(response);
        }

        [HttpGet]
        [Route("{Id}")]
        [ProducesResponseType(typeof(ShortUserResponseJson), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetById([FromRoute]Guid Id)
        {
            var response = await _getUserByIdUseCase.Execute(Id);
            return Ok(response);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ShortUserResponseJson), StatusCodes.Status201Created)]
        //[ProducesResponseType(typeof(ResponseErrorMessagesJson),StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody]CreateUserRequestJson request)
        {
            var response = await _createUserUseCase.Execute(request);
            return Created(string.Empty, response);
        }

        [HttpPut]
        [Route("{id}")]
        [ProducesResponseType(typeof(ShortUserResponseJson), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update([FromRoute]Guid id, [FromBody] UpdateUserRequestJson request)
        {
            var response = await _updateUserUseCase.Execute(id, request);
 
            return Ok(response);
        }

        [HttpDelete]
        [Route("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete([FromRoute]Guid id)
        {
            await _deleteUserUseCase.Execute(id);
            return NoContent(); 
        }


    }
}
