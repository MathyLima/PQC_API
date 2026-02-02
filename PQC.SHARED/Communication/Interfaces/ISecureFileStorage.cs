namespace PQC.SHARED.Communication.Interfaces
{
    namespace PQC.SHARED.Interfaces
    {
        /// <summary>
        /// Interface para armazenamento seguro de arquivos
        /// </summary>
        public interface ISecureFileStorage
        {
            /// <summary>
            /// Salva dados em um caminho específico
            /// </summary>
            /// <param name="path">Caminho relativo do arquivo (ex: "keys/user123/private.key")</param>
            /// <param name="data">Dados em bytes a serem salvos</param>
            /// <returns>Caminho completo onde o arquivo foi salvo</returns>
            Task<string> SaveAsync(string path, byte[] data);

            /// <summary>
            /// Recupera dados de um caminho específico
            /// </summary>
            /// <param name="path">Caminho relativo do arquivo</param>
            /// <returns>Dados em bytes</returns>
            /// <exception cref="FileNotFoundException">Quando o arquivo não existe</exception>
            Task<byte[]> GetAsync(string path);

            /// <summary>
            /// Deleta um arquivo
            /// </summary>
            /// <param name="path">Caminho relativo do arquivo</param>
            Task DeleteAsync(string path);

            /// <summary>
            /// Verifica se um arquivo existe
            /// </summary>
            /// <param name="path">Caminho relativo do arquivo</param>
            /// <returns>True se existe, False caso contrário</returns>
            Task<bool> ExistsAsync(string path);

            /// <summary>
            /// Lista todos os arquivos em um diretório
            /// </summary>
            /// <param name="directoryPath">Caminho do diretório (ex: "keys/user123")</param>
            /// <returns>Lista de caminhos de arquivos</returns>
            Task<IEnumerable<string>> ListFilesAsync(string directoryPath);

            /// <summary>
            /// Retorna o caminho completo para um caminho relativo
            /// </summary>
            string GetFullPath(string relativePath);
        }
    }
}
