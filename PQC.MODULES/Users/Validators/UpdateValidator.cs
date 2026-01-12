using FluentValidation;
using PQC.COMMUNICATION.Requests.Users.Update;

namespace PQC.MODULES.Users.Validators
{
    public class UpdateValidator : AbstractValidator<UpdateUserRequestJson>
    {
        public UpdateValidator()
        {
            RuleFor(client => client.Name)
                .NotEmpty().WithMessage("O nome do usuário não pode estar vazio.")
                .MinimumLength(3).WithMessage("Nome de usuário deve conter ao menos 3 caracteres");
            RuleFor(client => client.Email)
                .NotEmpty().WithMessage("O email é necessário")
                .EmailAddress().WithMessage("O e-mail deve ser válido.");
        }
    }
}
