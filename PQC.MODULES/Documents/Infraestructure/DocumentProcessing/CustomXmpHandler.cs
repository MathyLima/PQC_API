using iText.Kernel.Pdf;
using System.Text;

namespace PQC.MODULES.Documents.Infraestructure.DocumentProcessing
{
    public class CustomXmpHandler
    {
        private const string CUSTOM_XMP_KEY = "PQCSignatureMetadata";

        /// <summary>
        /// Injeta XMP em um objeto customizado (não no metadata stream padrão)
        /// </summary>
        public static void InjectCustomXmp(PdfDocument pdfDoc, string xmpContent)
        {
            var catalog = pdfDoc.GetCatalog();

            // Criar stream customizado
            byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpContent);
            var streamDict = new PdfStream(xmpBytes);
            streamDict.Put(PdfName.Type, new PdfName(CUSTOM_XMP_KEY));

            // Adicionar ao catalog
            catalog.GetPdfObject().Put(new PdfName(CUSTOM_XMP_KEY), streamDict);

            Console.WriteLine($"✅ XMP customizado injetado em /{CUSTOM_XMP_KEY}");
        }

        /// <summary>
        /// Extrai XMP do objeto customizado
        /// </summary>
        public static string ExtractCustomXmp(PdfDocument pdfDoc)
        {
            var catalog = pdfDoc.GetCatalog();
            var xmpObject = catalog.GetPdfObject().Get(new PdfName(CUSTOM_XMP_KEY));

            if (xmpObject == null)
            {
                return string.Empty;
            }

            if (xmpObject is PdfStream stream)
            {
                byte[] bytes = stream.GetBytes();
                return Encoding.UTF8.GetString(bytes);
            }

            return string.Empty;
        }

        /// <summary>
        /// Remove XMP do objeto customizado
        /// </summary>
        public static void RemoveCustomXmp(PdfDocument pdfDoc)
        {
            var catalog = pdfDoc.GetCatalog();
            catalog.GetPdfObject().Remove(new PdfName(CUSTOM_XMP_KEY));
            Console.WriteLine($"✅ XMP customizado removido de /{CUSTOM_XMP_KEY}");
        }
    }
}