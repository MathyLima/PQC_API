using Microsoft.AspNetCore.Mvc;
using PQC.API.Models;
using PQC.MODULES.Documents.Application.DTOs;
using PQC.MODULES.Documents.Application.UseCases;
using PQC.MODULES.Documents.Application.UseCases.Sign;
using PQC.MODULES.Documents.Application.UseCases.Validation;
using System.Text.Json;

namespace PQC.API.Controllers.Documents
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
    }

    public class PreparePdfResponseDto
    {
        public string ToBeSignedBase64 { get; set; }
        public string PreparedFilePath { get; set; }
        public string FileName { get; set; }
    }


    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly SignDocumentUseCase _signDocumentUseCase;
        private readonly ValidateDocumentUseCase _validateDocumentUseCase;
        private readonly SaveDocumentUseCase _saveDocumentUseCase;
        private readonly IHttpClientFactory _httpClientFactory;

        public DocumentsController(
            SignDocumentUseCase signDocumentUseCase,
            ValidateDocumentUseCase validateUseCase,
            SaveDocumentUseCase saveDocumentUseCase,
            IHttpClientFactory httpClientFactory)
        {
            _signDocumentUseCase = signDocumentUseCase;
            _validateDocumentUseCase = validateUseCase;
            _saveDocumentUseCase = saveDocumentUseCase;
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("sign")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> SignDocument([FromForm] CreateDocumentRequestJson request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                Console.WriteLine("❌ Nenhum arquivo foi enviado");
                return BadRequest("Nenhum arquivo foi enviado");
            }

            if (string.IsNullOrWhiteSpace(request.FileName))
            {
                Console.WriteLine("❌ Nome do arquivo é obrigatório");
                return BadRequest("Nome do arquivo é obrigatório");
            }

            Console.WriteLine("\n══════════════════════════════════════════════");
            Console.WriteLine("   INICIANDO PROCESSO DE ASSINATURA");
            Console.WriteLine("══════════════════════════════════════════════");
            Console.WriteLine($"📄 Arquivo: {request.FileName}");
            Console.WriteLine($"📊 Tamanho: {request.File.Length} bytes");
            Console.WriteLine($"👤 UserId: {request.UserId}");
            Console.WriteLine();

            byte[] content;
            using (var ms = new MemoryStream())
            {
                await request.File!.CopyToAsync(ms);
                content = ms.ToArray();
            }

            var useCaseInput = new DocumentUploadRequest
            {
                UserId = request.UserId!,
                Content = content,
                FileName = request.FileName,
                ContentType = request.File.ContentType,
            };

            Console.WriteLine("💾 Salvando arquivo original...");
            var documentPath = await _saveDocumentUseCase.Execute(useCaseInput);
            Console.WriteLine($"✅ Arquivo salvo: {documentPath}");

            var documentId = Guid.NewGuid().ToString();
            Console.WriteLine($"🆔 DocumentId gerado: {documentId}\n");

            var client = _httpClientFactory.CreateClient("PdfService");

            Console.WriteLine("🔑 Preparando metadados com chave pública...");
            var prepareMetadataObj = await _signDocumentUseCase.PrepareMetadata(
                request.UserId,
                request.FileName,
                documentId
            );

            var metadataDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                JsonSerializer.Serialize(prepareMetadataObj)
            );

            var metadata = new
            {
                documentId = metadataDict["documentId"].GetString(),
                signerName = metadataDict["signerName"].GetString(),
                reason = "Documento assinado digitalmente com PQC",
                location = "Digital",
                publicKey = metadataDict["publicKey"].GetString()
            };

            Console.WriteLine("✅ Metadados preparados");
            Console.WriteLine($"   DocumentId: {metadata.documentId}");
            Console.WriteLine($"   SignerName: {metadata.signerName}");
            Console.WriteLine($"   PublicKey (primeiros 50 chars): {metadata.publicKey?.Substring(0, Math.Min(50, metadata.publicKey.Length))}...\n");

            var payload = new
            {
                caminhoArquivo = documentPath,
                metadata = metadata
            };

            Console.WriteLine("📤 Enviando para Java preparar PDF...");
            var response = await client.PostAsJsonAsync("/api/pdfManager/preparar", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Erro ao preparar PDF: {errorContent}");
                return StatusCode((int)response.StatusCode, $"Erro ao preparar PDF: {errorContent}");
            }

            Console.WriteLine("📥 Lendo resposta do serviço Java...");
            var rawJson = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"RAW RESPONSE: {rawJson}\n");

            var apiResponse = JsonSerializer.Deserialize<ApiResponse<PreparePdfResponseDto>>(
                rawJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse == null || !apiResponse.Success || apiResponse.Data == null)
            {
                Console.WriteLine("❌ Resposta inválida do serviço Java");
                return StatusCode(500, "Resposta inválida do serviço de preparação do PDF.");
            }

            var prepareResult = apiResponse.Data;

            if (string.IsNullOrWhiteSpace(prepareResult.FileName))
            {
                prepareResult.FileName = request.FileName;
                Console.WriteLine($"⚠️ FileName não veio do Java, usando original: {request.FileName}");
            }

            Console.WriteLine($"✅ PDF preparado pelo Java");
            Console.WriteLine($"   Arquivo preparado: {prepareResult.PreparedFilePath}");
            Console.WriteLine($"   FileName: {prepareResult.FileName}");

            // ── Determinar bytes a assinar ───────────────────────────────────────
            // O Java retorna toBeSignedBase64 = bytes brutos do ByteRange (50KB+).
            // O Dilithium assina esses bytes diretamente — faz o hash internamente.
            // NÃO usar Hash (SHA-256, 32 bytes) — causaria hash-do-hash.
            if (string.IsNullOrEmpty(prepareResult.ToBeSignedBase64))
                throw new Exception("ToBeSignedBase64 não foi retornado pelo serviço Java.");

            byte[] bytesToSign =
                Convert.FromBase64String(prepareResult.ToBeSignedBase64);

            Console.WriteLine($"✅ Bytes to sign: {bytesToSign.Length}");


            var signRequest = new SignDocumentRequest
            {
                DataToSign = bytesToSign,
                PreparedFilePath = prepareResult.PreparedFilePath,
                FileName = prepareResult.FileName
            };

            Console.WriteLine("✍️ Assinando bytes com PQC...");
            var assinar = await _signDocumentUseCase.Execute(
                signRequest,
                request.UserId,
                documentId,
                documentPath
            );

            var signatureBase64 = Convert.ToBase64String(assinar.SignedContent);
            Console.WriteLine($"✅ Assinatura PQC gerada: {signatureBase64.Substring(0, 40)}...\n");

            Console.WriteLine("📝 Finalizando PDF com assinatura...");

            var finalizePayload = new
            {
                caminhoArquivo = prepareResult.PreparedFilePath,
                assinaturaBase64 = signatureBase64,
                metadata = metadata
            };

            var finalizeResponse = await client.PostAsJsonAsync("/api/pdfManager/finalizar", finalizePayload);

            if (!finalizeResponse.IsSuccessStatusCode)
            {
                var errorContent = await finalizeResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"❌ Erro ao finalizar PDF: {errorContent}");
                return StatusCode((int)finalizeResponse.StatusCode, $"Erro ao finalizar PDF: {errorContent}");
            }

            var rawFinalize = await finalizeResponse.Content.ReadAsStringAsync();
            var finalizeApiResponse = JsonSerializer.Deserialize<ApiResponse<Dictionary<string, object>>>(
                rawFinalize,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (finalizeApiResponse == null || !finalizeApiResponse.Success)
                return StatusCode(500, "Erro ao finalizar PDF.");

            var signedPath = finalizeApiResponse.Data["signedPath"].ToString();
            Console.WriteLine($"✅ PDF finalizado: {signedPath}\n");

            if (!System.IO.File.Exists(signedPath))
            {
                Console.WriteLine($"❌ Arquivo assinado não encontrado: {signedPath}");
                return StatusCode(500, $"Arquivo assinado não encontrado: {signedPath}");
            }

            var signedPdfBytes = await System.IO.File.ReadAllBytesAsync(signedPath);
            Console.WriteLine($"📄 Arquivo assinado lido: {signedPdfBytes.Length} bytes");

            Console.WriteLine("\n🗑️ Limpando arquivos temporários...");
            try
            {
                if (System.IO.File.Exists(prepareResult.PreparedFilePath))
                {
                    System.IO.File.Delete(prepareResult.PreparedFilePath);
                    Console.WriteLine($"   ✓ Deletado: {prepareResult.PreparedFilePath}");
                }
                if (System.IO.File.Exists(documentPath))
                {
                    System.IO.File.Delete(documentPath);
                    Console.WriteLine($"   ✓ Deletado: {documentPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro ao limpar arquivos temporários: {ex.Message}");
            }

            Console.WriteLine("\n══════════════════════════════════════════════");
            Console.WriteLine("   ✅ DOCUMENTO ASSINADO COM SUCESSO!");
            Console.WriteLine("══════════════════════════════════════════════");
            Console.WriteLine($"   DocumentId: {documentId}");
            Console.WriteLine($"   FileName: {request.FileName}");
            Console.WriteLine($"   Size: {signedPdfBytes.Length} bytes");
            Console.WriteLine("══════════════════════════════════════════════\n");

            return File(signedPdfBytes, "application/pdf", request.FileName.Replace(".pdf", "_signed.pdf"));
        }

        [HttpPost("verify")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> VerifyDocument([FromForm] VerifyDocumentRequestJson request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                Console.WriteLine("❌ Nenhum arquivo foi enviado para verificação");
                return BadRequest("Nenhum arquivo foi enviado");
            }

            Console.WriteLine("\n══════════════════════════════════════════════");
            Console.WriteLine("   INICIANDO VERIFICAÇÃO DE DOCUMENTO");
            Console.WriteLine("══════════════════════════════════════════════");
            Console.WriteLine($"📄 Arquivo: {request.File.FileName}");
            Console.WriteLine($"📊 Tamanho: {request.File.Length} bytes");
            Console.WriteLine();

            string tempPath = null;

            try
            {
                var tempDir = Path.Combine(Path.GetTempPath(), "pdf-signatures");
                Directory.CreateDirectory(tempDir);

                var fileName = $"{Guid.NewGuid()}.pdf";
                tempPath = Path.Combine(tempDir, fileName);

                Console.WriteLine($"💾 Salvando temporariamente: {tempPath}");

                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                Console.WriteLine("✅ Arquivo salvo temporariamente\n");
                Console.WriteLine("📤 Enviando para Java extrair assinaturas...");

                var client = _httpClientFactory.CreateClient("PdfService");
                var encodedPath = Uri.EscapeDataString(tempPath);
                var response = await client.GetAsync(
                    $"/api/pdfManager/verificar?caminhoArquivo={encodedPath}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Erro ao verificar no Java: {errorContent}");
                    return StatusCode((int)response.StatusCode, errorContent);
                }

                var signaturesJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"✅ Resposta recebida do Java");
                Console.WriteLine($"   JSON (primeiros 500 chars): {signaturesJson.Substring(0, Math.Min(500, signaturesJson.Length))}...\n");

                var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<JavaSignatureInfo>>>(
                    signaturesJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (apiResponse == null || !apiResponse.Success || apiResponse.Data == null)
                {
                    Console.WriteLine("❌ Resposta inválida do serviço Java");
                    return StatusCode(500, "Resposta inválida do serviço de verificação do PDF.");
                }

                var javaSignatures = apiResponse.Data;
                Console.WriteLine($"✅ {javaSignatures.Count} assinatura(s) encontrada(s)");

                foreach (var sig in javaSignatures)
                {
                    var pkStatus = !string.IsNullOrEmpty(sig.PublicKeyBase64)
                        ? $"Presente ({sig.PublicKeyBase64.Substring(0, Math.Min(30, sig.PublicKeyBase64.Length))}...)"
                        : "AUSENTE ❌";
                    Console.WriteLine($"   Assinatura #{sig.Number}: PublicKey = {pkStatus}");
                }
                Console.WriteLine();

                var javaSignaturesJson = JsonSerializer.Serialize(javaSignatures);

                Console.WriteLine("🔐 Validando assinaturas com C#...\n");
                var validationResult = await _validateDocumentUseCase.Execute(javaSignaturesJson);

                Console.WriteLine("\n══════════════════════════════════════════════");
                Console.WriteLine(validationResult.IsValid ? "   ✅ DOCUMENTO VÁLIDO" : "   ❌ DOCUMENTO INVÁLIDO");
                Console.WriteLine("══════════════════════════════════════════════\n");

                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ERRO durante verificação: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}\n");
                return StatusCode(500, $"Erro ao verificar documento: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPath) && System.IO.File.Exists(tempPath))
                {
                    try
                    {
                        System.IO.File.Delete(tempPath);
                        Console.WriteLine($"🗑️ Arquivo temporário deletado: {tempPath}\n");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Erro ao deletar arquivo temporário: {ex.Message}\n");
                    }
                }
            }
        }
    }
}