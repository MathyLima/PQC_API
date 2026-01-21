// PQC.MODULES.Auth/Validators/LoginValidator.cs
using FluentValidation;
using PQC.COMMUNICATION.Requests.Auth;
using PQC.COMMUNICATION.Requests.Auth.Login;

namespace PQC.MODULES.Auth.Validators
{
    public class LoginValidator : AbstractValidator<LoginRequestJson>
    {
        public LoginValidator()
        {
            RuleFor(client => client.Login)
                .NotEmpty().WithMessage("Login é obrigatório");

            RuleFor(client => client.Password)
                .NotEmpty().WithMessage("Senha é Obrigatória")
                .MinimumLength(6).WithMessage("A senha deve conter ao menos 6 caracteres");
        }
    }
}