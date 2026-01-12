using PQC.COMMUNICATION.Requests.Auth.Login;
using PQC.COMMUNICATION.Responses.Auth;
using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Auth.Application.Services.Security;
using PQC.MODULES.Users.Infrastructure.InMemory;

namespace PQC.MODULES.Auth.Application.Services.UseCases.Login
{
    public class LoginUseCase
    {
        private readonly JwtTokenService _jwtService;

        public LoginUseCase(JwtTokenService jwtService)
        {
            _jwtService = jwtService;
        }

        public LoginResponseJson Execute(LoginRequestJson request)
        {
            // Busca usuário
            var user = UserInMemoryDatabase.Users
                .FirstOrDefault(u => u.Email == request.Email);

            if (user == null)
            {
                throw new UnauthorizedException("Email não encontrado");
            }

            // Verifica senha
            bool isPasswordValid = PasswordHasher.VerifyPassword(
                request.Password,
                user.PasswordHash
            );

            if (!isPasswordValid)
            {
                throw new UnauthorizedException("Email ou Senha Inválidos");
            }

            // Gera token
            var token = _jwtService.GenerateToken(user);

            return new LoginResponseJson
            {
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email
            };
        }
    }
}