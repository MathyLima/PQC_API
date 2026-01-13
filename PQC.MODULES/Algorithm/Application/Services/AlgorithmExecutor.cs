using PQC.MODULES.Algorithm.Domain.Entities;
using System.Diagnostics;
namespace PQC.MODULES.Algorithm.Application.Services
{
    public class AlgorithmExecutor
    {
        private readonly string _execPath;
        private readonly string _tempDir;

        public AlgorithmExecutor(string execPath, string tempDir)
        {
            _execPath = execPath;
            _tempDir = tempDir;
            if(!Directory.Exists(_tempDir))
            {
                Directory.CreateDirectory(_tempDir);
            }
        }
        public async Task<SignatureResult> SignDocumentAsync(byte[] documentContent, string? privateKeyPath = null)
        {
            var tempInputFile = Path.Combine(_tempDir, $"input_{Guid.NewGuid()}.pdf");
            var tempOutputFile = Path.Combine(_tempDir, $"output_{Guid.NewGuid()}.sig");

            try
            {
                await File.WriteAllBytesAsync(tempInputFile, documentContent);
                var arguments = BuildSignArguments(tempInputFile, tempOutputFile, privateKeyPath);
                var processResult = await ExecuteProcessAsync(_execPath, arguments);

                byte[]? signature = null;
                if (File.Exists(tempOutputFile))
                {
                    signature = await File.ReadAllBytesAsync(tempOutputFile);
                }

                return new SignatureResult
                {
                    Success = processResult.ExitCode == 0,
                    Signature = signature,
                    ExitCode = processResult.ExitCode,
                    StdOutput = processResult.StdOutput,
                    StdError = processResult.StdError,
                    ErrorMessage = processResult.ExitCode != 0 ? processResult.StdError : null
                };
            }
            finally
            {
                CleanupTempFiles(tempInputFile, tempOutputFile);
            }
        }

        private string BuildSignArguments(string inputFile, string outputFile, string? privateKeyPath)
        {
            var args = $"sign -in \"{inputFile}\" -out \"{outputFile}\"";
            if (!string.IsNullOrEmpty(privateKeyPath))
            {
                args += $" -key \"{privateKeyPath}\"";
            }
            return args;
        }
        
        private async Task<(int ExitCode, string StdOutput, string StdError)> ExecuteProcessAsync(
            string executable,
            string arguments)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            return (
                process.ExitCode,
                outputBuilder.ToString(),
                errorBuilder.ToString()
                );
        }


        private void CleanupTempFiles(params string[] files)
        {
            foreach (var file in files)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                    // Log mas não falha
                }
            }
        }

    }
}
