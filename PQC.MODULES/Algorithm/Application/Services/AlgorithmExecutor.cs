using PQC.MODULES.Algorithm.Domain.Entities;
using System.Diagnostics;

namespace PQC.MODULES.Algorithm.Application.Services
{
    public class AlgorithmExecutor
    {
        private readonly string _execPath;
        private readonly string _tempDir;
        private readonly string _privateKeyPath;

        public AlgorithmExecutor(
            string execPath,
            string tempDir,
            string privateKeyPath)
        {
            _execPath = Path.Combine(AppContext.BaseDirectory, execPath);
            _tempDir = Path.Combine(AppContext.BaseDirectory, tempDir);
            _privateKeyPath = Path.Combine(AppContext.BaseDirectory, privateKeyPath);

            Directory.CreateDirectory(_tempDir);
        }

        public async Task<SignatureResult> SignDocumentAsync(string documentPath)
        {
            var documentContent = await File.ReadAllBytesAsync(documentPath);
            var tempInputFile = Path.Combine(_tempDir, $"input_{Guid.NewGuid()}.bin");
            var tempSignatureFile = Path.Combine(_tempDir, $"signature_{Guid.NewGuid()}.sign");

            try
            {
                await File.WriteAllBytesAsync(tempInputFile, documentContent);

                var arguments = BuildSignArguments(tempInputFile, tempSignatureFile);
                var processResult = await ExecuteProcessAsync(_execPath, arguments);

                string? algorithm = null;
                string? signatureText = null;

                if (processResult.ExitCode == 0 && File.Exists(tempSignatureFile))
                {
                    var lines = await File.ReadAllLinesAsync(tempSignatureFile);
                    bool inSignature = false;
                    var sb = new System.Text.StringBuilder();

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("[ALGORITHM]"))
                        {
                            var index = Array.IndexOf(lines, line);
                            if (index + 1 < lines.Length)
                                algorithm = lines[index + 1].Trim();
                        }

                        if (line.StartsWith("[SIGNATURE]"))
                        {
                            inSignature = true;
                            continue;
                        }

                        if (line.StartsWith("[") && inSignature)
                            break; // terminou a seção

                        if (inSignature && !string.IsNullOrWhiteSpace(line))
                            sb.AppendLine(line.Trim());
                    }

                    signatureText = sb.ToString();
                }

                return new SignatureResult
                {
                    Success = processResult.ExitCode == 0 && signatureText != null,
                    Algorithm = algorithm,
                    Signature = signatureText != null ? System.Text.Encoding.UTF8.GetBytes(signatureText) : null,
                    ExitCode = processResult.ExitCode,
                    StdOutput = processResult.StdOutput,
                    StdError = processResult.StdError,
                    ErrorMessage = processResult.ExitCode != 0 || signatureText == null
                        ? processResult.StdError ?? "Assinatura não foi gerada"
                        : null
                };
            }
            finally
            {
                //CleanupTempFiles(tempInputFile, tempSignatureFile);
            }
        }



        private string BuildSignArguments(string inputFile, string outputFile)
        {
            return $"sign \"{inputFile}\" \"{_privateKeyPath}\" \"{outputFile}\"";
        }
        /*

        private void CleanupTempFiles(params string[] files)
        {
            foreach (var f in files)
            {
                if (File.Exists(f))
                    File.Delete(f);
            }
        }
        */

        private async Task<(int ExitCode, string StdOutput, string StdError)>
             ExecuteProcessAsync(string executable, string arguments)
                {
                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = executable,
                            Arguments = arguments,
                            WorkingDirectory = _tempDir, // <-- mudar para _tempDir
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

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


            }
}
