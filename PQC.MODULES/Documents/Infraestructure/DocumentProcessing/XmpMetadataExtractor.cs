using iText.Kernel.Pdf;
using iText.Kernel.XMP;
using iText.Kernel.XMP.Impl;
using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;
using PQC.SHARED.Communication.DTOs.Documents.Responses;
using System.Text;
using System.Xml.Linq;

namespace PQC.MODULES.Documents.Infraestructure.DocumentProcessing
{
    /// <summary>
    /// Extrator de XMP que suporta TANTO atributos QUANTO elementos
    /// </summary>
    public class XmpMetadataExtractor : IXmpMetadataExtractor
    {
        public async Task<SignatureValidationResult> ExtractSignaturesAsync(byte[] pdfContent)
        {
            var signatures = new List<ExtractedSignature>();

            try
            {
                using var ms = new MemoryStream(pdfContent);
                using var pdfReader = new PdfReader(ms);
                using var pdfDoc = new PdfDocument(pdfReader);

                // ✅ Extrair XMP do objeto customizado
                string xmpXml = CustomXmpHandler.ExtractCustomXmp(pdfDoc);

                if (string.IsNullOrEmpty(xmpXml))
                {
                    Console.WriteLine("⚠️ Nenhum XMP customizado encontrado");
                    return new SignatureValidationResult
                    {
                        HasSignatures = false,
                        Signatures = new List<ExtractedSignature>()
                    };
                }

                signatures = ParseSignaturesFromXmp(xmpXml);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro: {ex.Message}");
            }

            return new SignatureValidationResult
            {
                HasSignatures = signatures.Count > 0,
                Signatures = signatures
            };
        }


        public async Task<byte[]> RemoveSignatureMetadataAsync(byte[] pdfContent, int signatureOrder)
        {
            using var inputMs = new MemoryStream(pdfContent);
            using var outputMs = new MemoryStream();
            using var pdfReader = new PdfReader(inputMs);
            using var pdfWriter = new PdfWriter(outputMs);
            using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

            // ✅ Extrair, modificar e reinjetar XMP customizado
            string currentXmp = CustomXmpHandler.ExtractCustomXmp(pdfDoc);

            if (string.IsNullOrEmpty(currentXmp))
            {
                pdfDoc.Close();
                return outputMs.ToArray();
            }

            // Parse e remover assinatura
            string cleanXmp = RemoveXPackets(currentXmp);
            var xmpDoc = XDocument.Parse(cleanXmp);
            var rdfNs = XNamespace.Get("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            var pqcNs = XNamespace.Get("http://pqc.com/signature/1.0/");

            var bag = xmpDoc.Descendants(pqcNs + "signatures")
                .Descendants(rdfNs + "Bag")
                .FirstOrDefault();

            if (bag != null)
            {
                var signatureToRemove = bag.Elements(rdfNs + "li")
                    .FirstOrDefault(li =>
                    {
                        var orderElement = li.Descendants(pqcNs + "order").FirstOrDefault();
                        return orderElement != null && int.Parse(orderElement.Value) == signatureOrder;
                    });

                signatureToRemove?.Remove();

                if (!bag.Elements(rdfNs + "li").Any())
                {
                    // Remover XMP completamente se não há mais assinaturas
                    CustomXmpHandler.RemoveCustomXmp(pdfDoc);
                }
                else
                {
                    // Reinjetar XMP atualizado
                    string updatedXmp = WrapWithXPackets(xmpDoc.ToString(SaveOptions.DisableFormatting));
                    CustomXmpHandler.InjectCustomXmp(pdfDoc, updatedXmp);
                }
            }

            pdfDoc.Close();
            return PdfCleanupHelper.StabilizePdf(outputMs.ToArray());
        }
        private string GetXmpMetadata(PdfDocument doc)
        {
            try
            {
                byte[] xmpBytes = doc.GetXmpMetadata();
                if (xmpBytes != null && xmpBytes.Length > 0)
                {
                    return Encoding.UTF8.GetString(xmpBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Erro ao extrair XMP: {ex.Message}");
            }

            return string.Empty;
        }

        private string RemoveXPackets(string xmp)
        {
            xmp = System.Text.RegularExpressions.Regex.Replace(
                xmp, @"<\?xpacket\s+begin[^>]*\?>", "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            xmp = System.Text.RegularExpressions.Regex.Replace(
                xmp, @"<\?xpacket\s+end[^>]*\?>", "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return xmp.Trim();
        }

        private string WrapWithXPackets(string xmpContent)
        {
            return $@"<?xpacket begin='ï»¿' id='W5M0MpCehiHzreSzNTczkc9d'?>
{xmpContent}
<?xpacket end='w'?>";
        }

        private List<ExtractedSignature> ParseSignaturesFromXmp(string xmpXml)
        {
            var signatures = new List<ExtractedSignature>();

            try
            {
                Console.WriteLine("\n🔍 Procurando assinaturas no XMP...");

                // Remove xpackets apenas para parsing
                string cleanXmp = RemoveXPackets(xmpXml);

                var xmpDoc = XDocument.Parse(cleanXmp);
                var rdfNs = XNamespace.Get("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                var dcNs = XNamespace.Get("http://purl.org/dc/elements/1.1/");
                var pqcNs = XNamespace.Get("http://pqc.com/signature/1.0/");

                Console.WriteLine($"   Namespace PQC: {pqcNs}");

                // Procurar especificamente pelo elemento pqc:signatures
                var signaturesElement = xmpDoc.Descendants(pqcNs + "signatures").FirstOrDefault();

                if (signaturesElement == null)
                {
                    Console.WriteLine("   ❌ Elemento <pqc:signatures> não encontrado!");
                    return signatures;
                }

                Console.WriteLine("   ✅ Elemento <pqc:signatures> encontrado!");

                // Procurar pelo Bag dentro de signatures
                var bag = signaturesElement.Descendants(rdfNs + "Bag").FirstOrDefault();

                if (bag == null)
                {
                    Console.WriteLine("   ❌ <rdf:Bag> não encontrado dentro de <pqc:signatures>");
                    return signatures;
                }

                Console.WriteLine("   ✅ <rdf:Bag> encontrado!");

                // Buscar elementos rdf:li dentro do Bag
                var signatureElements = bag.Elements(rdfNs + "li").ToList();

                Console.WriteLine($"   ✅ Encontrados {signatureElements.Count} elementos <rdf:li>");

                foreach (var li in signatureElements)
                {
                    try
                    {
                        // ✅ SUPORTA AMBOS: atributos E elementos aninhados
                        string? order = GetValueFromAttributeOrElement(li, pqcNs, "order");
                        string? documentId = GetValueFromAttributeOrElement(li, pqcNs, "documentId");
                        string? creator = GetValueFromAttributeOrElement(li, dcNs, "creator");
                        string? date = GetValueFromAttributeOrElement(li, dcNs, "date");
                        string? algorithm = GetValueFromAttributeOrElement(li, pqcNs, "algorithm");
                        string? documentHash = GetValueFromAttributeOrElement(li, pqcNs, "documentHash");
                        string? signatureValue = GetValueFromAttributeOrElement(li, pqcNs, "signatureValue");
                        string? publicKey = GetValueFromAttributeOrElement(li, pqcNs, "publicKey");

                        if (order == null)
                        {
                            Console.WriteLine("      ⚠️ order não encontrado - pulando");
                            continue;
                        }

                        int orderNum = int.Parse(order);
                        Console.WriteLine($"\n   📋 Processando assinatura:");
                        Console.WriteLine($"      order: {orderNum}");
                        Console.WriteLine($"      documentId: {documentId}");
                        Console.WriteLine($"      creator: {creator}");
                        Console.WriteLine($"      date: {date}");

                        int pageNumber = orderNum + 1;
                        Console.WriteLine($"      ✓ Ordem: {orderNum}, Página: {pageNumber}");

                        signatures.Add(new ExtractedSignature
                        {
                            Order = orderNum,

                            DocumentId = documentId ?? string.Empty,

                            SignerName = creator ?? string.Empty,

                            SignedAt = DateTime.Parse(date ?? DateTime.UtcNow.ToString()),

                            Algorithm = algorithm ?? string.Empty,

                            // ✅ HASH DO PDF
                            DocumentHash = documentHash ?? string.Empty,

                            // ✅ ASSINATURA DO HASH
                            SignatureValue = signatureValue ?? string.Empty,

                            PublicKey = publicKey ?? string.Empty,

                            PageNumber = pageNumber
                        });


                        Console.WriteLine($"      ✅ Assinatura #{orderNum} extraída!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"      ⚠️ Erro ao processar assinatura: {ex.Message}");
                    }
                }

                Console.WriteLine($"\n✅ {signatures.Count} assinatura(s) extraída(s)");
                foreach (var sig in signatures)
                {
                    Console.WriteLine($"   - Ordem: {sig.Order}, Signer: {sig.SignerName}, DocId: {sig.DocumentId.Substring(0, Math.Min(12, sig.DocumentId.Length))}...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao parsear XMP: {ex.Message}");
            }

            Console.WriteLine($"📋 Total de assinaturas encontradas: {signatures.Count}\n");
            return signatures;
        }

        /// <summary>
        /// Obtém valor tanto de atributo quanto de elemento aninhado
        /// </summary>
        private string? GetValueFromAttributeOrElement(XElement element, XNamespace ns, string name)
        {
            // Tentar ler do atributo primeiro (formato iText7)
            var attribute = element.Attribute(ns + name);
            if (attribute != null)
                return attribute.Value;

            // Se não for atributo, tentar elemento aninhado (formato parseType='Resource')
            var childElement = element.Descendants(ns + name).FirstOrDefault();
            if (childElement != null)
                return childElement.Value;

            return null;
        }

        /// <summary>
        /// Remove APENAS o XMP customizado, mantém todas as páginas
        /// </summary>
        public async Task<byte[]> RemoveAllXmpAsync(byte[] pdfContent)
        {
            using var inputMs = new MemoryStream(pdfContent);
            using var outputMs = new MemoryStream();
            using var pdfReader = new PdfReader(inputMs);
            using var pdfWriter = new PdfWriter(outputMs);
            using var pdfDoc = new PdfDocument(pdfReader, pdfWriter);

            // Remove apenas o objeto XMP customizado
            CustomXmpHandler.RemoveCustomXmp(pdfDoc);

            pdfDoc.Close();
            return PdfCleanupHelper.StabilizePdf(outputMs.ToArray());
        }
    }
}