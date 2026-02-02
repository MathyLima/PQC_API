// PQC.MODULES.Auth/Validators/LoginValidator.cs
using FluentValidation;
using PQC.MODULES.Authentication.Application.DTOs;

namespace PQC.MODULES.Authentication.Validators
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