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

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseJson), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Login([FromBody] LoginRequestJson request)
        {
            var jwtSettings = _configuration.GetSection("Jwt");

            var jwtService = new JwtTokenService(
                jwtSettings["SecretKey"],
                jwtSettings["Issuer"],
                jwtSettings["Audience"],
                int.Parse(jwtSettings["ExpirationHours"])
            );

            var useCase = new LoginUseCase(jwtService);
            var response = useCase.Execute(request);

            return Ok(response);
        }
    }
}