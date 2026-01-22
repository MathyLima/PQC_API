using PQC.MODULES.Documents.Infraestructure.SignAlgorithm.Domain.Entities;
using System.Diagnostics;

namespace PQC.MODULES.Documents.Infraestructure.SignAlgorithm.Application.Services
{
    public class SignDocumentAlgorithmExecutor
    {
        private readonly string _execPath;
        private readonly string _tempDir;
        private readonly string _privateKeyPath;

        public SignDocumentAlgorithmExecutor(
            string execPath,
            string tempDir,
            string privateKeyPath)
        {
            _execPath = Path.Combine(AppContext.BaseDirectory, execPath);
            _tempDir = Path.Combine(AppContext.BaseDirectory, tempDir);
            _privateKeyPath = Path.Combine(AppContext.BaseDirectory, privateKeyPath);

            Directory.CreateDirectory(_tempDir);
        }

        /// <summary>
        /// Assina um documento usando o algoritmo PQC externo
        /// </summary>
        public async Task<SignatureResult> SignDocumentAsync(byte[] documentContent)
        {
            ValidateDocumentContent(documentContent);

            var (tempInputFile, tempSignatureFile) = CreateTempFilePaths();

            try
            {
                // 1. Salvar conteúdo em arquivo temporário
                await SaveDocumentToTempFile(documentContent, tempInputFile);

                // 2. Executar processo de assinatura externo
                var processResult = await ExecuteSigningProcess(tempInputFile, tempSignatureFile);

                // 3. Ler resultado da assinatura
                var (algorithm, signature) = await ReadSignatureFromFile(tempSignatureFile, processResult.ExitCode);

                // 4. Montar resultado final
                return BuildSignatureResult(processResult, algorithm, signature);
            }
            finally
            {
                CleanupTempFiles(tempInputFile, tempSignatureFile);
            }
        }

        #region Validation

        private void ValidateDocumentContent(byte[] documentContent)
        {
            if (documentContent == null || documentContent.Length == 0)
            {
                throw new ArgumentException("Document content is empty");
            }
        }

        #endregion

        #region File Operations

        private (string inputFile, string signatureFile) CreateTempFilePaths()
        {
            var tempInputFile = Path.Combine(_tempDir, $"input_{Guid.NewGuid()}.bin");
            var tempSignatureFile = Path.Combine(_tempDir, $"signature_{Guid.NewGuid()}.sign");
            return (tempInputFile, tempSignatureFile);
        }

        private async Task SaveDocumentToTempFile(byte[] documentContent, string tempInputFile)
        {
            await File.WriteAllBytesAsync(tempInputFile, documentContent);
        }

        private void CleanupTempFiles(params string[] files)
        {
            foreach (var f in files)
            {
                if (File.Exists(f))
                {
                    try
                    {
                        File.Delete(f);
                    }
                    catch
                    {
                        // Ignora erros de limpeza
                    }
                }
            }
        }

        #endregion

        #region Process Execution

        private async Task<(int ExitCode, string StdOutput, string StdError)> ExecuteSigningProcess(
            string tempInputFile,
            string tempSignatureFile)
        {
            var arguments = BuildSignArguments(tempInputFile, tempSignatureFile);
            return await ExecuteProcessAsync(_execPath, arguments);
        }

        private string BuildSignArguments(string inputFile, string outputFile)
        {
            return $"sign \"{inputFile}\" \"{_privateKeyPath}\" \"{outputFile}\"";
        }

        private async Task<(int ExitCode, string StdOutput, string StdError)> ExecuteProcessAsync(
            string executable,
            string arguments)
        {
            using var process = CreateProcess(executable, arguments);

            var (stdout, stderr) = SetupProcessOutputCapture(process);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            return (
                process.ExitCode,
                stdout.ToString(),
                stderr.ToString()
            );
        }

        private Process CreateProcess(string executable, string arguments)
        {
            return new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = arguments,
                    WorkingDirectory = _tempDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
        }

        private (System.Text.StringBuilder stdout, System.Text.StringBuilder stderr) SetupProcessOutputCapture(Process process)
        {
            var stdout = new System.Text.StringBuilder();
            var stderr = new System.Text.StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stdout.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    stderr.AppendLine(e.Data);
            };

            return (stdout, stderr);
        }

        #endregion

        #region Signature Parsing

        /// <summary>
        /// Lê o arquivo de assinatura e extrai algoritmo + assinatura
        /// </summary>
        private async Task<(string? algorithm, string? signature)> ReadSignatureFromFile(
            string tempSignatureFile,
            int exitCode)
        {
            if (exitCode != 0 || !File.Exists(tempSignatureFile))
            {
                return (null, null);
            }

            var lines = await File.ReadAllLinesAsync(tempSignatureFile);
            var algorithm = ExtractAlgorithmFromLines(lines);
            var signature = ExtractSignatureFromLines(lines);

            return (algorithm, signature);
        }

        private string? ExtractAlgorithmFromLines(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("[ALGORITHM]") && i + 1 < lines.Length)
                {
                    return lines[i + 1].Trim();
                }
            }
            return null;
        }

        private string? ExtractSignatureFromLines(string[] lines)
        {
            var sb = new System.Text.StringBuilder();
            bool inSignature = false;

            foreach (var line in lines)
            {
                // Encontrou início da seção de assinatura
                if (line.StartsWith("[SIGNATURE]"))
                {
                    inSignature = true;
                    continue;
                }

                // Encontrou outra seção - termina leitura
                if (line.StartsWith("[") && inSignature)
                {
                    break;
                }

                // Captura linhas da assinatura
                if (inSignature && !string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine(line.Trim());
                }
            }

            var result = sb.ToString();
            return string.IsNullOrEmpty(result) ? null : result;
        }

        #endregion

        #region Result Building

        private SignatureResult BuildSignatureResult(
            (int ExitCode, string StdOutput, string StdError) processResult,
            string? algorithm,
            string? signature)
        {
            var success = processResult.ExitCode == 0 && signature != null;
            var signatureBytes = signature != null
                ? System.Text.Encoding.UTF8.GetBytes(signature)
                : null;

            return new SignatureResult
            {
                Success = success,
                Algorithm = algorithm,
                Signature = signatureBytes,
                ExitCode = processResult.ExitCode,
                StdOutput = processResult.StdOutput,
                StdError = processResult.StdError,
                ErrorMessage = !success
                    ? processResult.StdError ?? "Assinatura não foi gerada"
                    : null
            };
        }

        #endregion
    }
}