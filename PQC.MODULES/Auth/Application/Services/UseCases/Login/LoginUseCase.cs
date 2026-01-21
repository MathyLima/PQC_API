using PQC.COMMUNICATION.Requests.Auth.Login;
using PQC.COMMUNICATION.Responses.Auth;
using PQC.EXCEPTIONS.ExceptionsBase;
using PQC.MODULES.Auth.Application.Services.Security;
using PQC.MODULES.Auth.Validators;
using PQC.MODULES.Users.Infraestructure.Repositories;

namespace PQC.MODULES.Auth.Application.Services.UseCases.Login
{
    public class LoginUseCase
    {
        private readonly JwtTokenService _jwtService;
        private readonly IUserRepository _userRepository;

        public LoginUseCase(IUserRepository userRepository, JwtTokenService jwtService)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
        }

        public async Task<LoginResponseJson> ExecuteAsync(LoginRequestJson request)
        {
            Validate(request);

            var user = await _userRepository.GetByLoginAsync(request.Login);

            if (user == null)
            {
                throw new ErrorOnValidationException(new List<string> { "Login ou senha incorretos" });
            }

            bool isPasswordValid = PasswordHasher.VerifyPassword(request.Password, user.Senha);

            if (!isPasswordValid)
            {
                throw new ErrorOnValidationException(new List<string> { "Login ou senha incorretos" });
            }

            // Rehash da senha se necessário (após aumentar o WorkFactor)
            if (PasswordHasher.NeedsRehash(user.Senha))
            {
                user.Senha = PasswordHasher.HashPassword(request.Password);
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();
            }

            // CORREÇÃO: usar _jwtService ao invés de _tokenGenerator
            var token = _jwtService.GenerateToken(user);

            return new LoginResponseJson
            {
                UserId = user.Id.ToString(),
                Token = token,
                Name = user.Nome,
                Email = user.Email
            };
        }

        private void Validate(LoginRequestJson request)
        {
            var validator = new LoginValidator();
            var result = validator.Validate(request);

            if (!result.IsValid)
            {
                var errors = result.Errors.Select(failure => failure.ErrorMessage).ToList();
                throw new ErrorOnValidationException(errors);
            }
        }
    }
}