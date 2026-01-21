using FluentValidation;
using PQC.COMMUNICATION.Requests.Users.Create;
using PQC.MODULES.Users.Validators.SharedValidators;

namespace PQC.MODULES.Users.Validators
{
    public class CreationValidator:AbstractValidator<CreateUserRequestJson>
    {
        public CreationValidator()
        {
            RuleFor(client => client.Login)
                .NotEmpty().WithMessage("O Login não pode estar vazio.")
                .MinimumLength(4).WithMessage("Nome de usuário deve conter ao menos 4 caracteres");

            RuleFor(client => client.Name)
                .NotEmpty().WithMessage("O nome do usuário não pode estar vazio.")
                .MinimumLength(6).WithMessage("Nome de usuário deve conter ao menos 6 caracteres");

            RuleFor(client => client.Email)
                .NotEmpty().WithMessage("O email é necessário")
                .EmailAddress().WithMessage("O e-mail deve ser válido.");

            RuleFor(client => client.Telefone)
                .Matches(@"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$")
                .WithMessage("Telefone inválido")
                .When(x => !string.IsNullOrWhiteSpace(x.Telefone));

            RuleFor(client => client.Password)
                .NotEmpty().WithMessage("Senha é Obrigatória")
                .MinimumLength(6).WithMessage("A senha deve conter ao menos 6 caracteres");

            RuleFor(client => client.Cpf)
                .NotEmpty().WithMessage("CPF é Obrigatório")
                .Must(CpfValidator.IsValidCpf)
                .WithMessage("CPF Inválido");
        }

       
    }
}
