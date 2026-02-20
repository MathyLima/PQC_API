namespace PQC.MODULES.Documents.Application.Interfaces.PasswordHaser
{
    namespace PQC.INFRAESTRUCTURE.Security.Hashing.Interfaces
    {
        /// <summary>
        /// Interface para hash e verificação de senhas
        /// </summary>
        public interface IPasswordHasher
        {
            /// <summary>
            /// Gera hash seguro de uma senha
            /// </summary>
            /// <param name="password">Senha em texto plano</param>
            /// <returns>Hash da senha</returns>
            string HashPassword(string password);

            /// <summary>
            /// Verifica se uma senha corresponde ao hash armazenado
            /// </summary>
            /// <param name="password">Senha em texto plano</param>
            /// <param name="hash">Hash armazenado</param>
            /// <returns>True se a senha corresponder ao hash</returns>
            bool VerifyPassword(string password, string hash);

            /// <summary>
            /// Verifica se um hash precisa ser recalculado (upgrade de segurança)
            /// </summary>
            /// <param name="hash">Hash a verificar</param>
            /// <returns>True se o hash precisa ser atualizado</returns>
            bool NeedsRehash(string hash);
        }
    }
}
