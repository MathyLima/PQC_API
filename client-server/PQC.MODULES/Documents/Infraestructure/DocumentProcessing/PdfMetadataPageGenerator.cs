using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;

namespace PQC.MODULES.Documents.Infraestructure.DocumentProcessing;

public class PdfMetadataPageGenerator : ISignatureMetadataPageGenerator
{
    private const double PageWidth = 595;  // A4 width in points
    private const double PageHeight = 842; // A4 height in points
    private const double Margin = 50;
    private const double ContentWidth = PageWidth - (2 * Margin);

    public async Task<byte[]> GenerateMetaDataPageAsync(SignatureMetadata metadata)
    {
        using var doc = new PdfDocument();
        var page = doc.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        var gfx = XGraphics.FromPdfPage(page);

        double yPosition = Margin;

        // Título principal
        yPosition = DrawMainTitle(gfx, yPosition);

        // Informações do documento
        yPosition = DrawDocumentInfo(gfx, metadata, yPosition);

        // Seção: Dados do Signatário
        yPosition = DrawSection(gfx, "DADOS DO SIGNATÁRIO", yPosition);
        yPosition = DrawSignerInfo(gfx, metadata, yPosition);

        // Seção: Dados Técnicos da Assinatura
        yPosition = DrawSection(gfx, "DADOS TÉCNICOS DA ASSINATURA", yPosition);
        yPosition = DrawTechnicalInfo(gfx, metadata, yPosition);

        // Rodapé informativo
        DrawFooter(gfx, metadata);

        using var ms = new MemoryStream();
        doc.Save(ms);
        return await Task.FromResult(ms.ToArray());
    }

    private double DrawMainTitle(XGraphics gfx, double yPosition)
    {
        var titleFont = new XFont("Arial", 18, XFontStyleEx.Bold);
        var title = "Informações de Assinatura Digital Pós Quântica";

        gfx.DrawString(
            title,
            titleFont,
            XBrushes.Black,
            new XRect(Margin, yPosition, ContentWidth, 30),
            XStringFormats.TopLeft
        );

        return yPosition + 40;
    }

    private double DrawDocumentInfo(XGraphics gfx, SignatureMetadata metadata, double yPosition)
    {
        var normalFont = new XFont("Arial", 11);

        gfx.DrawString(
            $"Documento: {metadata.DocumentName}",
            normalFont,
            XBrushes.Black,
            new XRect(Margin, yPosition, ContentWidth, 20),
            XStringFormats.TopLeft
        );
        yPosition += 20;

        gfx.DrawString(
            $"Assinado em: {metadata.SignedAt:yyyy-MM-dd HH:mm:ss} UTC",
            normalFont,
            XBrushes.Black,
            new XRect(Margin, yPosition, ContentWidth, 20),
            XStringFormats.TopLeft
        );

        return yPosition + 35;
    }

    private double DrawSection(XGraphics gfx, string sectionTitle, double yPosition)
    {
        var sectionFont = new XFont("Arial", 12, XFontStyleEx.Bold);

        // Desenha linha horizontal acima do título
        gfx.DrawLine(
            new XPen(XColors.Black, 1),
            Margin,
            yPosition,
            PageWidth - Margin,
            yPosition
        );

        yPosition += 5;

        gfx.DrawString(
            sectionTitle,
            sectionFont,
            XBrushes.Black,
            new XRect(Margin, yPosition, ContentWidth, 20),
            XStringFormats.TopLeft
        );

        return yPosition + 25;
    }

    private double DrawSignerInfo(XGraphics gfx, SignatureMetadata metadata, double yPosition)
    {
        var normalFont = new XFont("Arial", 11);
        var labelFont = new XFont("Arial", 11, XFontStyleEx.Bold);
        double lineHeight = 22;

        // Nome
        yPosition = DrawLabelValue(gfx, "Nome:", metadata.SignerName, labelFont, normalFont, yPosition, lineHeight);

        // Email
        yPosition = DrawLabelValue(gfx, "Email:", metadata.SignerEmail, labelFont, normalFont, yPosition, lineHeight);

        return yPosition + 20;
    }

    private double DrawTechnicalInfo(XGraphics gfx, SignatureMetadata metadata, double yPosition)
    {
        var normalFont = new XFont("Arial", 11);
        var labelFont = new XFont("Arial", 11, XFontStyleEx.Bold);
        var monoFont = new XFont("Courier New", 5);
        double lineHeight = 22;

        // Algoritmo
        yPosition = DrawLabelValue(gfx, "Algoritmo:", metadata.Algorithm, labelFont, normalFont, yPosition, lineHeight);

        // Hash da Assinatura (com quebra de linha)
        gfx.DrawString(
            "Hash da Assinatura:",
            labelFont,
            XBrushes.Black,
            new XRect(Margin, yPosition, ContentWidth, 20),
            XStringFormats.TopLeft
        );
        yPosition += lineHeight;

        // Quebra o hash em múltiplas linhas
        yPosition = DrawWrappedText(gfx, metadata.SignatureValue, monoFont, yPosition);

        return yPosition + 15;
    }

    private double DrawLabelValue(XGraphics gfx, string label, string value, XFont labelFont, XFont valueFont, double yPosition, double lineHeight)
    {
        var labelSize = gfx.MeasureString(label, labelFont);

        // Desenha o label
        gfx.DrawString(
            label,
            labelFont,
            XBrushes.Black,
            new XRect(Margin, yPosition, labelSize.Width, 20),
            XStringFormats.TopLeft
        );

        // Desenha o valor ao lado
        gfx.DrawString(
            value,
            valueFont,
            XBrushes.Black,
            new XRect(Margin + labelSize.Width + 5, yPosition, ContentWidth - labelSize.Width - 5, 20),
            XStringFormats.TopLeft
        );

        return yPosition + lineHeight;
    }

    private double DrawWrappedText(XGraphics gfx, string text, XFont font, double yPosition)
    {
        double lineHeight = 7;

        double maxWidth = ContentWidth;

        string currentLine = "";

        foreach (char c in text)
        {
            string testLine = currentLine + c;

            double width = gfx.MeasureString(testLine, font).Width;

            // Se estourar a largura, quebra a linha
            if (width > maxWidth)
            {
                gfx.DrawString(
                    currentLine,
                    font,
                    XBrushes.Black,
                    new XRect(Margin, yPosition, ContentWidth, lineHeight),
                    XStringFormats.TopLeft
                );

                yPosition += lineHeight;

                currentLine = c.ToString();
            }
            else
            {
                currentLine = testLine;
            }
        }

        // Última linha
        if (!string.IsNullOrEmpty(currentLine))
        {
            gfx.DrawString(
                currentLine,
                font,
                XBrushes.Black,
                new XRect(Margin, yPosition, ContentWidth, lineHeight),
                XStringFormats.TopLeft
            );

            yPosition += lineHeight;
        }

        return yPosition;
    }

    private void DrawFooter(XGraphics gfx, SignatureMetadata metadata)
    {
        var footerFont = new XFont("Arial", 9, XFontStyleEx.Italic);
        var boldFont = new XFont("Arial", 9, XFontStyleEx.Bold);

        var footerText =
            $"Esta assinatura foi gerada com {metadata.Algorithm}, um algoritmo de assinatura pós-quântico.\n" +
            "O hash interno é parte do processo algorítmico e não é exposto ao usuário.";

        double footerY = PageHeight - Margin - 40;

        // Formato centralizado
        var centerFormat = new XStringFormat
        {
            Alignment = XStringAlignment.Center,
            LineAlignment = XLineAlignment.Center
        };

        // Linha superior do rodapé
        gfx.DrawLine(
            new XPen(XColors.Black, 0.5),
            Margin,
            footerY - 10,
            PageWidth - Margin,
            footerY - 10
        );

        // Texto do rodapé (quebrado em linhas)
        var lines = footerText.Split('\n');

        foreach (var line in lines)
        {
            gfx.DrawString(
                line,
                footerFont,
                XBrushes.DarkGray,
                new XRect(Margin, footerY, ContentWidth, 15),
                centerFormat
            );

            footerY += 15;
        }

        // Espaço antes da linha final
        footerY += 5;

        // Texto final
        var finalText = $"FIM DO REGISTRO DE ASSINATURA - {metadata.SignerName}";

        gfx.DrawString(
            finalText,
            boldFont,
            XBrushes.Black,
            new XRect(Margin, footerY, ContentWidth, 15),
            centerFormat
        );
    }

    private string MaskCpf(string cpf)
    {
        if (string.IsNullOrEmpty(cpf) || cpf.Length < 11)
            return "***.***.***-**";

        return $"{cpf.Substring(0, 3)}.***.***-**";
    }

}