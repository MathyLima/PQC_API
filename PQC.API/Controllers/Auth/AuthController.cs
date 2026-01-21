// PQC.API/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using PQC.COMMUNICATION.Requests.Auth.Login;
using PQC.COMMUNICATION.Responses.Auth;
using PQC.MODULES.Auth.Application.Services.Security;
using PQC.MODULES.Auth.Application.Services.UseCases.Login;

namespace PQC.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly LoginUseCase _loginUseCase;
        public AuthController(IConfiguration configuration, LoginUseCase loginUseCase)
        {
            _configuration = configuration;
            _loginUseCase = loginUseCase;
        }

        [HttpPost]
        [ProducesResponseType(typeof(LoginResponseJson), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestJson request)
        {
           
            var response = await _loginUseCase.ExecuteAsync(request);
            return Ok(response);            
        }
    }
}