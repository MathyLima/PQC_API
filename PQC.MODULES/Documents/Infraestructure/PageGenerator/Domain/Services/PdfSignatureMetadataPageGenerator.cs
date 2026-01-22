using iText.Bouncycastle;
using iText.Bouncycastleconnector;
using iText.IO.Font.Constants;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Font;
using iText.Layout.Properties;
using Org.BouncyCastle.Asn1.Cms;

namespace PQC.MODULES.Documents.Infraestructure.PageGenerator.Domain.Services
{
    public class PdfSignatureMetadataPageGenerator : ISignatureMetadata
    {
        public async Task<byte[]> GenerateMetadataPageAsync(SignatureMetadata metadata)
        {
            BouncyCastleFactoryCreator.SetFactory(new BouncyCastleFactory());

            using var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf);

            var fontProvider = new FontProvider();
            fontProvider.AddStandardPdfFonts();
            document.SetFontProvider(fontProvider);

            // Reduzir margens para aproveitar mais espaço
            document.SetMargins(30, 30, 30, 30);

            // Título
            document.Add(new Paragraph("Informações de Assinatura Digital Pós Quântica")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(16)
                .SimulateBold()
                .SetMarginBottom(8));

            // Informações do documento
            document.Add(new Paragraph($"Documento: {metadata.DocumentName}")
                .SetFontSize(11)
                .SetMarginBottom(2));

            document.Add(new Paragraph($"Assinado em: {metadata.SignedAt:dd/MM/yyyy HH:mm:ss} UTC")
                .SetFontSize(11)
                .SetMarginBottom(8));

            // Dados do signatário
            document.Add(new Paragraph("DADOS DO SIGNATÁRIO")
                .SetFontSize(13)
                .SimulateBold()
                .SetMarginBottom(4));

            document.Add(new Paragraph($"Nome: {metadata.SignerName}")
                .SetFontSize(10)
                .SetMarginBottom(2));

            document.Add(new Paragraph($"CPF: {metadata.SignerCpf}")
                .SetFontSize(10)
                .SetMarginBottom(2));

            document.Add(new Paragraph($"Email: {metadata.SignerEmail}")
                .SetFontSize(10)
                .SetMarginBottom(8));

            // Dados técnicos
            document.Add(new Paragraph("DADOS TÉCNICOS DA ASSINATURA")
                .SetFontSize(13)
                .SimulateBold()
                .SetMarginBottom(4));

            document.Add(new Paragraph("Hash da Assinatura:")
                .SetFontSize(10)
                .SimulateBold()
                .SetMarginBottom(2));

            // Adiciona o hash com tamanho ajustável
            AddAutoSizedHash(
                document,
                metadata.SignatureHash,
                maxFontSize: 8,
                minFontSize: 5
            );

            document.Add(new Paragraph($"Esta assinatura foi gerada com {metadata.Algorithm}, um algoritmo de assinatura pós-quântico. O hash interno é parte do\r\nprocesso algorítmico e não é exposto ao usuário.")
                .SetFontSize(10)
                .SetMarginBottom(4));

            document.Add(new Paragraph($"FIM DO REGISTRO DE ASSINATURA - {metadata.SignerName}")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(10)
                .SimulateBold());
            document.Close();
            return ms.ToArray();
        }

        private void AddAutoSizedHash(
            Document document,
            string hash,
            float maxFontSize,
            float minFontSize)
        {
            var page = document.GetPdfDocument().GetPage(1);
            var pageSize = page.GetPageSize();

            // Calcula espaço disponível de forma simplificada
            var leftMargin = document.GetLeftMargin();
            var rightMargin = document.GetRightMargin();
            var bottomMargin = document.GetBottomMargin();
            var topMargin = document.GetTopMargin();

            var availableWidth = pageSize.GetWidth() - leftMargin - rightMargin;
            var availableHeight = pageSize.GetHeight() - topMargin - bottomMargin;

            // Estima altura já utilizada (aproximação)
            var usedHeight = 0f;
            usedHeight += 16 + 8; // Título
            usedHeight += 11 + 2 + 11 + 8; // Documento e data
            usedHeight += 13 + 4; // Seção signatário
            usedHeight += (10 + 2) * 3 + 8; // Nome, CPF, Email
            usedHeight += 13 + 4; // Seção técnica
            usedHeight += 10 + 4 + 10 + 2; // Algoritmo e label hash
            usedHeight += 30; // Margem de segurança para fim

            var remainingHeight = availableHeight - usedHeight;

            float fontSize = maxFontSize;
            Paragraph bestParagraph = null;

            // Testa tamanhos decrescentes
            while (fontSize >= minFontSize)
            {
                var testParagraph = new Paragraph(hash)
                    .SetFontSize(fontSize)
                    .SetFontFamily(StandardFonts.COURIER)
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetMarginBottom(0);

                // Estima altura necessária baseado no texto
                var charsPerLine = (int)(availableWidth / (fontSize * 0.6)); // Courier é monospace
                var lines = Math.Ceiling((double)hash.Length / charsPerLine);
                var estimatedHeight = lines * (fontSize + 2);

                if (estimatedHeight <= remainingHeight)
                {
                    bestParagraph = testParagraph;
                    break;
                }

                fontSize -= 0.5f;
            }

            // Se não encontrou tamanho adequado, usa o mínimo
            if (bestParagraph == null)
            {
                bestParagraph = new Paragraph(hash)
                    .SetFontSize(minFontSize)
                    .SetFontFamily(StandardFonts.COURIER)
                    .SetTextAlignment(TextAlignment.LEFT)
                    .SetMarginBottom(0);
            }

            document.Add(bestParagraph);

            // Linha de finalização
            document.Add(new Paragraph("\n"));
            document.Add(new Paragraph("─────────────────────────────────────────────────────────")
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(10)
                .SetMarginTop(10)
                .SetMarginBottom(4));

            
        }
    }
}