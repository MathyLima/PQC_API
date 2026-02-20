using PQC.MODULES.Documents.Application.Interfaces.PasswordHaser.PQC.INFRAESTRUCTURE.Security.Hashing.Interfaces;
using PQC.MODULES.Documents.Application.Interfaces.PQCsigner;
using PQC.MODULES.Users.Domain.Entities;
using PQC.SHARED.Communication.DTOs.Enums;
using PQC.SHARED.Communication.DTOs.Users.Requests;
using PQC.SHARED.Communication.DTOs.Users.Responses;
using PQC.SHARED.Communication.Interfaces;
namespace PQC.MODULES.Users.Application.UseCases.Create
{
    public class CreateUserUseCase
    {
        private readonly IUserRepository _repository;
        private readonly INativePostQuantumSigner _keyGenerator;
        private readonly IKeyWriter _keyWriter;
        private readonly IPasswordHasher _passwordHasher;

        public CreateUserUseCase(
            IUserRepository repository,
            INativePostQuantumSigner keyGenerator,
            IKeyWriter keyWriter,
            IPasswordHasher passwordHasher)
        {
            _repository = repository;
            _keyGenerator = keyGenerator;
            _keyWriter = keyWriter;
            _passwordHasher = passwordHasher;
        }

        public async Task<ShortUserResponseJson> Execute(CreateUserRequestJson request)
        {
            Validate(request);

            var passwordHash = _passwordHasher.HashPassword(request.Password!);

            var entity = new User
            {
                Id = Guid.NewGuid().ToString(),
                Nome = request.Name!,
                Email = request.Email!,
                Senha = passwordHash!,
                Cpf = request.Cpf!,
                Telefone = request.Telefone!,
                Login = request.Login!,
                CodigoAlgoritmo = request.SignatureAlgorithm.ToEnumString(),

            };

            // Gerar chaves (implementação pode ser qualquer uma!)
            var (publicKey, privateKey) = await _keyGenerator.GenerateKeyPairAsync(entity.CodigoAlgoritmo);

            // Armazenar chaves (implementação pode ser qualquer uma!)
            entity.PublicKeyReference = await _keyWriter.SavePublicKeyAsync(entity.Id, publicKey);
            entity.PrivateKeyReference = await _keyWriter.SavePrivateKeyAsync(entity.Id, privateKey);

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return new ShortUserResponseJson
            {
                Id = Guid.Parse(entity.Id),
                Name = entity.Nome,
                Email = entity.Email,
            };
        }

        private void Validate(CreateUserRequestJson request) { /* ... */ }
    }
}