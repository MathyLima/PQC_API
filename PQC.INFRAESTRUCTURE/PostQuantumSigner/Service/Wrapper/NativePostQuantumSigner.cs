using PQC.MODULES.Documents.Application.Interfaces.PQCsigner;
using PQC.SHARED.Communication.Interfaces.PQC.SHARED.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PQC.INFRAESTRUCTURE.PostQuantumSigner.Service.Wrapper
{
    public class NativePostQuantumSigner : INativePostQuantumSigner
    {
        private readonly string _pqcCliPath;
        private readonly ISecureFileStorage _fileStorage;
        private const string BASE_TEMP_DIR = @"C:\Users\mathe\Documents\PQC_TEMP";
        private const string TEMP_SIGN_DIR = "sign";

        public NativePostQuantumSigner(
             string pqcCliPath,
             ISecureFileStorage fileStorage
         )
                {
                    _pqcCliPath = Path.Combine(AppContext.BaseDirectory, pqcCliPath);
                    _fileStorage = fileStorage;

                    EnsureTempDirectories();
                }


        public async Task<SignatureResult> SignAsync(byte[] data, byte[] privateKey)
        {
            var tempPrefix = Path.Combine(
                BASE_TEMP_DIR,
                TEMP_SIGN_DIR,
                $"key_{Guid.NewGuid()}"
            );
            var dataPath = $"{tempPrefix}_data.bin";
            var keyPath = $"{tempPrefix}.key";
            var sigPath = $"{tempPrefix}.sig";

            try
            {
                // Salva dados e chave
                var fullDataPath = await _fileStorage.SaveAsync(dataPath, data);
                var fullKeyPath = await _fileStorage.SaveAsync(keyPath, privateKey);
                var fullSigPath = _fileStorage.GetFullPath(sigPath);

                // Executar CLI
                var arguments = BuildSignArguments(fullDataPath, fullKeyPath, fullSigPath);
                var (exitCode, stdout, stderr) = await ExecutePqcCliAsync(arguments);

                // Verificar se assinatura foi gerada
                if (!await _fileStorage.ExistsAsync(sigPath))
                {
                    throw new Exception("Signature file was not generated");
                }

                // Ler assinatura
                var signature = await _fileStorage.GetAsync(sigPath);
                var detectedAlgorithm = DetectAlgorithmFromOutput(stdout) ?? "ML-DSA-65";

                return new SignatureResult
                {
                    Signature = signature,
                    Algorithm = detectedAlgorithm
                };
            }
            finally
            {
                await CleanupTempFilesAsync(dataPath, keyPath, sigPath);
            }
        }

        public async Task<bool> VerifyAsync(byte[] data, byte[] signature, byte[] publicKey)
        {
            var tempPrefix = Path.Combine(
                BASE_TEMP_DIR,
                TEMP_SIGN_DIR,
                $"key_{Guid.NewGuid()}"
            );
            var dataPath = $"{tempPrefix}_data.bin";
            var sigPath = $"{tempPrefix}.sig";
            var keyPath = $"{tempPrefix}.pub";

            try
            {
                // Salva dados, assinatura e chave pública
                var fullDataPath = await _fileStorage.SaveAsync(dataPath, data);
                var fullSigPath = await _fileStorage.SaveAsync(sigPath, signature);
                var fullKeyPath = await _fileStorage.SaveAsync(keyPath, publicKey);

                // Executar CLI
                var arguments = BuildVerifyArguments(fullDataPath, fullSigPath, fullKeyPath);
                var (exitCode, stdout, stderr) = await ExecutePqcCliAsync(arguments, throwOnError: false);

                // Verificação bem-sucedida se exitCode == 0
                return exitCode == 0;
            }
            finally
            {
                await CleanupTempFilesAsync(dataPath, sigPath, keyPath);
            }
        }

        public async Task<(byte[] publicKey, byte[] privateKey)> GenerateKeyPairAsync(string algorithm)
        {
            // Diretório temporário para gerar as chaves
            var tempPrefix = Path.Combine(
                BASE_TEMP_DIR,
                TEMP_SIGN_DIR,
                $"key_{Guid.NewGuid()}"
            );
            var publicKeyPath = $"{tempPrefix}.pub";
            var privateKeyPath = $"{tempPrefix}.key";

            try
            {
                // Obter caminhos completos no storage temporário
                var fullPubPath = _fileStorage.GetFullPath(publicKeyPath);
                var fullKeyPath = _fileStorage.GetFullPath(privateKeyPath);

                // Executar CLI para gerar o par de chaves
                var arguments = BuildKeyGenArguments(algorithm, tempPrefix);
                var (exitCode, stdout, stderr) = await ExecutePqcCliAsync(arguments);

                // Verificar se as chaves foram geradas
                if (!await _fileStorage.ExistsAsync(publicKeyPath) ||
                    !await _fileStorage.ExistsAsync(privateKeyPath))
                {
                    throw new Exception("Key files were not generated in temporary storage");
                }

                // Ler os bytes das chaves
                var publicKeyBytes = await _fileStorage.GetAsync(publicKeyPath);
                var privateKeyBytes = await _fileStorage.GetAsync(privateKeyPath);

                // Limpar arquivos temporários imediatamente após ler
                await CleanupTempFilesAsync(publicKeyPath, privateKeyPath);

                return (publicKeyBytes, privateKeyBytes);
            }
            catch
            {
                await CleanupTempFilesAsync(publicKeyPath, privateKeyPath);
                throw;
            }
        }



        private void EnsureTempDirectories()
        {
            var baseDir = BASE_TEMP_DIR;
            var signDir = Path.Combine(baseDir, TEMP_SIGN_DIR);

            Directory.CreateDirectory(baseDir);
            Directory.CreateDirectory(signDir);
        }


        private string BuildSignArguments(
            string inputFile,
            string privateKey,
            string? outputFile = null)
        {
            if (string.IsNullOrWhiteSpace(inputFile))
                throw new ArgumentException("Arquivo inválido");

            if (string.IsNullOrWhiteSpace(privateKey))
                throw new ArgumentException("Chave privada inválida");

            if (string.IsNullOrWhiteSpace(outputFile))
                outputFile = $"{inputFile}.sig";

            return $"sign \"{inputFile}\" \"{privateKey}\" \"{outputFile}\"";
        }

        private string BuildVerifyArguments(
            string inputFile,
            string signatureFile,
            string publicKey)
        {
            return $"verify \"{inputFile}\" \"{signatureFile}\" \"{publicKey}\"";
        }

        private string BuildKeyGenArguments(
            string algorithm,
            string? prefix = null)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return $"keygen {algorithm}";

            return $"keygen {algorithm} \"{prefix}\"";
        }

        /// <summary>
        /// Executa a CLI PQC e retorna o resultado
        /// </summary>
        private async Task<(int exitCode, string stdout, string stderr)> ExecutePqcCliAsync(
            string arguments,
            bool throwOnError = true)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _pqcCliPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) stdout.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) stderr.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            var stdoutStr = stdout.ToString();
            var stderrStr = stderr.ToString();

            if (throwOnError && process.ExitCode != 0)
            {
                throw new Exception($"PQC CLI failed with exit code {process.ExitCode}: {stderrStr}");
            }

            return (process.ExitCode, stdoutStr, stderrStr);
        }

        /// <summary>
        /// Detecta o algoritmo do output da CLI
        /// </summary>
        private string? DetectAlgorithmFromOutput(string cliOutput)
        {
            if (string.IsNullOrWhiteSpace(cliOutput))
                return null;

            if (cliOutput.Contains("ML-DSA-87")) return "ML-DSA-87";
            if (cliOutput.Contains("ML-DSA-65")) return "ML-DSA-65";
            if (cliOutput.Contains("ML-DSA-44")) return "ML-DSA-44";

            return null;
        }

        /// <summary>
        /// Limpa arquivos temporários
        /// </summary>
        private async Task CleanupTempFilesAsync(params string[] paths)
        {
            foreach (var path in paths)
            {
                try
                {
                    if (await _fileStorage.ExistsAsync(path))
                    {
                        await _fileStorage.DeleteAsync(path);
                    }
                }
                catch (Exception ex)
                {
                    // Log mas não falha - arquivos temporários
                    Console.WriteLine($"Warning: Failed to delete temp file {path}: {ex.Message}");
                }
            }
        }
    }
}