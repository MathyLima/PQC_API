using PQC.MODULES.Documents.Application.Interfaces.PQCsigner;
using PQC.SHARED.Communication.Interfaces.PQC.SHARED.Interfaces;
using System.Diagnostics;
using System.Text;

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

                // Ler arquivo .sig da CLI
                var signatureFileContent = await _fileStorage.GetAsync(sigPath);

                // 🔥 PARSEAR O ARQUIVO .sig E EXTRAIR APENAS OS BYTES DA ASSINATURA
                var (algorithm, signatureBytes) = ParseSignatureFile(signatureFileContent);

                Console.WriteLine($"✍️ Signature parsed: {signatureBytes.Length} bytes, Algorithm: {algorithm}");

                return new SignatureResult
                {
                    Signature = signatureBytes, // ← Bytes puros da assinatura
                    Algorithm = algorithm
                };
            }
            finally
            {
                await CleanupTempFilesAsync(dataPath, keyPath, sigPath);
            }
        }

        /// <summary>
        /// Verifica uma assinatura PQC.
        /// publicKey aqui recebe os bytes do PEM ORIGINAL gerado pela CLI
        /// (não bytes puros da chave — já é o conteúdo literal do arquivo .pub)
        /// </summary>
        public async Task<bool> VerifyAsync(byte[] data, byte[] signature, byte[] publicKey)
        {

            // ✅ LOGS DE DIAGNÓSTICO
            Console.WriteLine($"   data (hash) length: {data.Length} bytes");
            Console.WriteLine($"   data (hash) Base64: {Convert.ToBase64String(data)}");
            Console.WriteLine($"   signature length: {signature.Length} bytes");
            Console.WriteLine($"   signature Base64 (primeiros 40): {Convert.ToBase64String(signature).Substring(0, 40)}...");
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
                var pemPublicKey = Encoding.UTF8.GetString(publicKey);
                Console.WriteLine($"🔍 Using PEM public key directly from storage");
                Console.WriteLine($"   PEM preview: {pemPublicKey.Substring(0, Math.Min(60, pemPublicKey.Length))}...");

                // Detecta algoritmo a partir do PEM para montar o .sig
                var algorithm = DetectAlgorithmFromPem(pemPublicKey);
                Console.WriteLine($"   Algorithm detected from PEM: {algorithm}");

                var signatureFileContent = CreateSignatureFile(signature, algorithm);

                // Salva dados, assinatura e chave pública (PEM original diretamente)
                var fullDataPath = await _fileStorage.SaveAsync(dataPath, data);
                var fullSigPath = await _fileStorage.SaveAsync(sigPath, signatureFileContent);

                var fullKeyPath = await _fileStorage.SaveAsync(keyPath, publicKey);


                Console.WriteLine($"🔍 Verifying with:");
                Console.WriteLine($"   Data: {data.Length} bytes");
                Console.WriteLine($"   Signature: {signature.Length} bytes");
                Console.WriteLine($"   Public key (PEM): {publicKey.Length} bytes");
                Console.WriteLine($"   Algorithm: {algorithm}");

                // Executar CLI
                var arguments = BuildVerifyArguments(fullDataPath, fullSigPath, fullKeyPath);
                var (exitCode, stdout, stderr) = await ExecutePqcCliAsync(arguments, throwOnError: false);

                if (exitCode == 0)
                {
                    Console.WriteLine("CLI verification PASSED");
                }
                else
                {
                    Console.WriteLine($"CLI verification FAILED");
                    Console.WriteLine($"   stdout: {stdout}");
                    Console.WriteLine($"   stderr: {stderr}");
                }

                return exitCode == 0;
            }
            finally
            {
                await CleanupTempFilesAsync(dataPath, sigPath, keyPath);
            }
        }

        public async Task<(byte[] publicKey, byte[] privateKey)> GenerateKeyPairAsync(string algorithm)
        {
            var tempPrefix = Path.Combine(
                BASE_TEMP_DIR,
                TEMP_SIGN_DIR,
                $"key_{Guid.NewGuid()}"
            );
            var publicKeyPath = $"{tempPrefix}.pub";
            var privateKeyPath = $"{tempPrefix}.priv";

            try
            {
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

                // Ler os bytes das chaves (retorna bytes literais dos arquivos .pub e .priv da CLI)
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

        /// <summary>
        /// Detecta o algoritmo ML-DSA a partir do header do PEM
        /// </summary>
        private string DetectAlgorithmFromPem(string pem)
        {
            if (pem.Contains("ML-DSA-87"))
                return "ML-DSA-87";
            if (pem.Contains("ML-DSA-65"))
                return "ML-DSA-65";
            if (pem.Contains("ML-DSA-44"))
                return "ML-DSA-44";

            // Fallback: nunca deveria cair aqui se o PEM for válido
            throw new Exception($"Algoritmo não detectado no PEM. Conteúdo: {pem.Substring(0, Math.Min(100, pem.Length))}");
        }

        /// <summary>
        /// Parse o arquivo .sig da CLI e extrai o algoritmo e os bytes da assinatura
        /// Formato esperado:
        /// [ALGORITHM]
        /// ML-DSA-44
        /// 
        /// [SIGNATURE]
        /// <base64_signature>
        /// </summary>
        private (string algorithm, byte[] signature) ParseSignatureFile(byte[] signatureFile)
        {
            var content = Encoding.UTF8.GetString(signatureFile);
            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string? algorithm = null;
            bool inAlgorithmSection = false;
            bool inSignatureSection = false;
            var base64Lines = new List<string>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine == "[ALGORITHM]")
                {
                    inAlgorithmSection = true;
                    inSignatureSection = false;
                    continue;
                }
                else if (trimmedLine == "[SIGNATURE]")
                {
                    inAlgorithmSection = false;
                    inSignatureSection = true;
                    continue;
                }
                else if (trimmedLine.StartsWith("["))
                {
                    inAlgorithmSection = false;
                    inSignatureSection = false;
                    continue;
                }

                if (inAlgorithmSection && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    algorithm = trimmedLine;
                }
                else if (inSignatureSection && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    base64Lines.Add(trimmedLine);
                }
            }

            if (algorithm == null)
            {
                throw new Exception("Algorithm not found in .sig file");
            }

            if (base64Lines.Count == 0)
            {
                throw new Exception("Signature not found in .sig file");
            }

            var base64Signature = string.Join("", base64Lines);

            Console.WriteLine($"📝 Parsed from .sig file:");
            Console.WriteLine($"   Algorithm: {algorithm}");
            Console.WriteLine($"   Base64 length: {base64Signature.Length} chars");

            var signatureBytes = Convert.FromBase64String(base64Signature);

            Console.WriteLine($"   Signature bytes: {signatureBytes.Length}");

            return (algorithm, signatureBytes);
        }

        /// <summary>
        /// Cria o conteúdo do arquivo .sig no formato esperado pela CLI
        /// </summary>
        private byte[] CreateSignatureFile(byte[] signatureBytes, string algorithm)
        {
            var base64Signature = Convert.ToBase64String(signatureBytes);

            var content = $"[ALGORITHM]\n{algorithm}\n\n[SIGNATURE]\n{base64Signature}\n";

            Console.WriteLine($"📝 Creating .sig file:");
            Console.WriteLine($"   Algorithm: {algorithm}");
            Console.WriteLine($"   Signature bytes: {signatureBytes.Length}");
            Console.WriteLine($"   Base64 length: {base64Signature.Length} chars");

            return Encoding.UTF8.GetBytes(content);
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
                    Console.WriteLine($"Warning: Failed to delete temp file {path}: {ex.Message}");
                }
            }
        }
    }
}