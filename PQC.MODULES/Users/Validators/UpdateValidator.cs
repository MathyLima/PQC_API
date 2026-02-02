using FluentValidation;
using PQC.SHARED.Communication.DTOs.Users.Requests;

namespace PQC.MODULES.Users.Validators
{
    public class UpdateValidator : AbstractValidator<UpdateUserRequestJson>
    {
        public UpdateValidator()
        {
            RuleFor(x => x.Nome)
                .MinimumLength(3)
                .WithMessage("Nome de usuário deve conter ao menos 3 caracteres")
                .When(x => !string.IsNullOrWhiteSpace(x.Nome));

            RuleFor(x => x.Email)
                .EmailAddress()
                .WithMessage("O e-mail deve ser válido.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.Senha)
                .MinimumLength(6)
                .WithMessage("A senha deve conter ao menos 6 caracteres")
                .When(x => !string.IsNullOrWhiteSpace(x.Senha));
            RuleFor(x => x.Login)
                .MinimumLength(4)
                .WithMessage("Nome de usuário deve conter ao menos 4 caracteres")
                .When(x => !string.IsNullOrWhiteSpace(x.Login));
            RuleFor(x => x.Telefone)
                .Matches(@"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$")
                .WithMessage("Telefone inválido")
                .When(x => !string.IsNullOrWhiteSpace(x.Telefone));
        }
    }
}
