using FluentValidation;
using PQC.COMMUNICATION.Requests.Users.Create;

namespace PQC.MODULES.Users.Validators
{
    public class CreationValidator:AbstractValidator<CreateUserRequestJson>
    {
        public CreationValidator()
        {
            RuleFor(client => client.Name)
                .NotEmpty().WithMessage("O nome do usuário não pode estar vazio.")
                .MinimumLength(3).WithMessage("Nome de usuário deve conter ao menos 3 caracteres");
            RuleFor(client => client.Email)
                .NotEmpty().WithMessage("O email é necessário")
                .EmailAddress().WithMessage("O e-mail deve ser válido.");
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required")
                .MinimumLength(6).WithMessage("Password must be at least 6 characters");
        }
    }
}
