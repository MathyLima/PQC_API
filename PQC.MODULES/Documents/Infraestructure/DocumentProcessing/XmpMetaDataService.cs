using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;
using System.Xml.Linq;

namespace PQC.MODULES.Documents.Infraestructure.DocumentProcessing
{
    public class XmpMetadataService : IXmpMetaDataService
    {
        public string GenerateXmpMetadata(SignatureMetadata metadata, string existingXmp)
        {
            if (string.IsNullOrEmpty(existingXmp))
            {
                // PRIMEIRA ASSINATURA - criar estrutura nova
                return CreateInitialXmp(metadata);
            }
            else
            {
                // JÁ EXISTE XMP - fazer append
                return AppendSignatureToXmp(existingXmp, metadata);
            }
        }

        private string CreateInitialXmp(SignatureMetadata metadata)
        {
            return $@"<?xpacket begin='' id='W5M0MpCehiHzreSzNTczkc9d'?>
                        <x:xmpmeta xmlns:x='adobe:ns:meta/'>
                          <rdf:RDF xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'>
                            <rdf:Description rdf:about='' 
                                             xmlns:dc='http://purl.org/dc/elements/1.1/'
                                             xmlns:pqc='http://pqc.com/signature/1.0/'>
                              <pqc:signatures>
                                <rdf:Seq>
                                  <rdf:li>
                                    <pqc:order>1</pqc:order>
                                    <pqc:documentId>{metadata.DocumentId}</pqc:documentId>
                                    <dc:creator>{metadata.SignerName}</dc:creator>
                                    <dc:date>{metadata.SignedAt:yyyy-MM-ddTHH:mm:ssZ}</dc:date>
                                    <pqc:algorithm>{metadata.Algorithm}</pqc:algorithm>
                                    <pqc:signatureHash>{metadata.SignatureHash}</pqc:signatureHash>
                                  </rdf:li>
                                </rdf:Seq>
                              </pqc:signatures>
                            </rdf:Description>
                          </rdf:RDF>
                        </x:xmpmeta>
                      <?xpacket end='w'?>";
        }

        private string AppendSignatureToXmp(string existingXmp, SignatureMetadata metadata)
        {
            try
            {
                var xmpDoc = XDocument.Parse(existingXmp);

                var rdfNs = XNamespace.Get("http://www.w3.org/1999/02/22-rdf-syntax-ns#");
                var dcNs = XNamespace.Get("http://purl.org/dc/elements/1.1/");
                var pqcNs = XNamespace.Get("http://pqc.com/signature/1.0/");

                // Encontrar o array de assinaturas
                var seq = xmpDoc.Descendants(rdfNs + "Seq").FirstOrDefault();

                if (seq == null)
                {
                    // Não deveria acontecer, mas por segurança
                    return CreateInitialXmp(metadata);
                }

                // Contar assinaturas existentes
                int order = seq.Elements(rdfNs + "li").Count() + 1;

                // Criar nova assinatura
                var newSignature = new XElement(rdfNs + "li",
                    new XElement(pqcNs + "order", order),
                    new XElement(pqcNs + "documentId", metadata.DocumentId),
                    new XElement(dcNs + "creator", metadata.SignerName),
                    new XElement(dcNs + "date", metadata.SignedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")),
                    new XElement(pqcNs + "algorithm", metadata.Algorithm),
                    new XElement(pqcNs + "signatureHash", metadata.SignatureHash)
                );

                // Adicionar ao array
                seq.Add(newSignature);

                return xmpDoc.ToString();
            }
            catch
            {
                // Se der erro no parse, criar novo
                return CreateInitialXmp(metadata);
            }
        }
    }
}