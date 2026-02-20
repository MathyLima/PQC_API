using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace PQC.MODULES.Documents.Infraestructure.DocumentProcessing
{
    public class XmpMetadataService : IXmpMetaDataService
    {
        // ================= VALIDATION =================

        public void ValidateExistingXmp(string existingXmp)
        {
            try
            {
                string clean = RemoveXPackets(existingXmp);

                var doc = XDocument.Parse(clean);

                var rdfNs = XNamespace.Get("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                var pqcNs = XNamespace.Get("http://pqc.com/signature/1.0/");

                var bag = doc
                    .Descendants(pqcNs + "signatures")
                    .Descendants(rdfNs + "Bag")
                    .FirstOrDefault();

                if (bag == null)
                    throw new Exception("Bag de assinaturas inexistente");

                int expected = 1;

                foreach (var li in bag.Elements(rdfNs + "li"))
                {
                    var order = li.Element(pqcNs + "order")?.Value;

                    if (order == null || int.Parse(order) != expected)
                        throw new Exception("Ordem de assinaturas inválida");

                    expected++;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"XMP inválido ou adulterado: {ex.Message}"
                );
            }
        }

        // ================= GENERATION =================

        /// <summary>
        /// Única versão de GenerateXmpMetadata.
        /// Se existingXmp for vazio → cria XMP inicial.
        /// Se existingXmp existir → appenda a nova assinatura ao Bag.
        /// </summary>
        public string GenerateXmpMetadata(
            SignatureMetadata metadata,
            string signatureBase64,
            string existingXmp)
        {
            if (string.IsNullOrWhiteSpace(existingXmp))
            {
                Console.WriteLine("📝 XMP: Criando XMP inicial (sem existente)");
                return CreateInitialXmp(metadata, signatureBase64);
            }

            Console.WriteLine("📝 XMP: Existente encontrado, appending nova assinatura...");
            return AppendSignature(existingXmp, metadata, signatureBase64);
        }

        // ================= INTERNAL =================

        private string CreateInitialXmp(
            SignatureMetadata metadata,
            string signature)
        {
            Console.WriteLine($"📝 XMP: Criando inicial com order=1, documentId={metadata.DocumentId}");

            return $@"<?xpacket begin='ï»¿' id='W5M0MpCehiHzreSzNTczkc9d'?>
                        <x:xmpmeta xmlns:x='adobe:ns:meta/'>
                          <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
                            <rdf:Description rdf:about=''
                                xmlns:dc='http://purl.org/dc/elements/1.1/'
                                xmlns:pqc='http://pqc.com/signature/1.0/'>
                                <pqc:signatures>
                                    <rdf:Bag>
                                        <rdf:li rdf:parseType='Resource'>
                                        <pqc:order>1</pqc:order>
                                        <pqc:documentId>{metadata.DocumentId}</pqc:documentId>
                                        <dc:creator>{metadata.SignerName}</dc:creator>
                                        <dc:date>{metadata.SignedAt:O}</dc:date>
                                        <pqc:algorithm>{metadata.Algorithm}</pqc:algorithm>
                                        <pqc:documentHash>{metadata.DocumentHash}</pqc:documentHash>
                                        <pqc:signatureValue>{signature}</pqc:signatureValue>
                                        <pqc:publicKey>{metadata.PublicKey}</pqc:publicKey>
                                        </rdf:li>
                                    </rdf:Bag>
                                </pqc:signatures>
                            </rdf:Description>
                      </rdf:RDF>
                    </x:xmpmeta>
                <?xpacket end='w'?>";
        }

        private string AppendSignature(
            string existingXmp,
            SignatureMetadata metadata,
            string signature)
        {
            string clean = RemoveXPackets(existingXmp);

            var doc = XDocument.Parse(clean);

            var rdfNs = XNamespace.Get("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
            var dcNs = XNamespace.Get("http://purl.org/dc/elements/1.1/");
            var pqcNs = XNamespace.Get("http://pqc.com/signature/1.0/");

            var bag = doc
                .Descendants(pqcNs + "signatures")
                .Descendants(rdfNs + "Bag")
                .FirstOrDefault();

            // ✅ Se não encontrar o Bag, não swallows — explode com erro claro
            if (bag == null)
            {
                throw new InvalidOperationException(
                    "AppendSignature: Bag não encontrado no XMP existente. " +
                    $"XMP recebido: {existingXmp.Substring(0, Math.Min(200, existingXmp.Length))}"
                );
            }

            int order = bag.Elements(rdfNs + "li").Count() + 1;

            Console.WriteLine($"📝 XMP: Appending com order={order}, documentId={metadata.DocumentId}");

            var li = new XElement(rdfNs + "li",
                new XAttribute(rdfNs + "parseType", "Resource"),

                new XElement(pqcNs + "order", order),
                new XElement(pqcNs + "documentId", metadata.DocumentId),
                new XElement(dcNs + "creator", metadata.SignerName),
                new XElement(dcNs + "date", metadata.SignedAt.ToString("O")),
                new XElement(pqcNs + "algorithm", metadata.Algorithm),
                new XElement(pqcNs + "documentHash", metadata.DocumentHash),
                new XElement(pqcNs + "signatureValue", signature),
                new XElement(pqcNs + "publicKey", metadata.PublicKey)
            );

            bag.Add(li);

            Console.WriteLine($"📝 XMP: Bag agora tem {bag.Elements(rdfNs + "li").Count()} assinatura(s)");

            return WrapWithXPackets(
                doc.ToString(SaveOptions.DisableFormatting)
            );
        }

        // ================= UTILS =================

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

        private string WrapWithXPackets(string xmp)
        {
            return $@"<?xpacket begin='ï»¿' id='W5M0MpCehiHzreSzNTczkc9d'?>
                             {xmp}
                    <?xpacket end='w'?>";
        }
    }
}