using PQC.MODULES.Authentication.Application.DTOs;
using PQC.MODULES.Authentication.Validators;
using PQC.MODULES.Documents.Application.Interfaces.PasswordHaser.PQC.INFRAESTRUCTURE.Security.Hashing.Interfaces;
using PQC.SHARED.Exceptions.Domain;

namespace PQC.MODULES.Authentication.Application.UseCases.Login
{
    public class LoginUseCase
    {
        private readonly Token _jwtService;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        public LoginUseCase(IUserRepository userRepository, Token jwtService, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
        }

        public async Task<LoginResponseJson> ExecuteAsync(LoginRequestJson request)
        {
            Validate(request);

            var user = await _userRepository.GetByLoginAsync(request.Login);

            if (user == null)
            {
                throw new ErrorOnValidationException( "Login ou senha incorretos" );
            }

            bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.Senha);

            if (!isPasswordValid)
            {
                throw new ErrorOnValidationException("Login ou senha incorretos");
            }

            // Rehash da senha se necessário (após aumentar o WorkFactor)
            if (_passwordHasher.NeedsRehash(user.Senha))
            {
                user.Senha = _passwordHasher.HashPassword(request.Password);
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();
            }

            // CORREÇÃO: usar _jwtService ao invés de _tokenGenerator
            var token = _jwtService.GenerateToken(user);

            return new LoginResponseJson
            {
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