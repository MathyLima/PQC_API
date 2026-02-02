// PQC.MODULES.Documents/DependencyInjection.cs
using Microsoft.Extensions.DependencyInjection;
using PdfSharp.Fonts;
using PQC.MODULES.Documents.Application.Interfaces.PDFcomposer;
using PQC.MODULES.Documents.Application.UseCases.List;
using PQC.MODULES.Documents.Application.UseCases.Sign;
using PQC.MODULES.Documents.Infraestructure.DocumentProcessing;

namespace PQC.MODULES.Documents.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDocumentsModule(
            this IServiceCollection services)
        {
            // ========== USE CASES ==========
            services.AddScoped<SignDocumentUseCase>();
            services.AddScoped<ListDocumentsByUserIdUseCase>();
            if (GlobalFontSettings.FontResolver == null)
            {
                GlobalFontSettings.FontResolver = new SystemFontResolver();
            }
            // ========== DOCUMENT PROCESSING ==========
            services.AddScoped<IDocumentComposer, PdfDocumentComposer>();
            services.AddScoped<ISignatureMetadataPageGenerator, PdfMetadataPageGenerator>();  
            services.AddScoped<IDocumentMerger, PdfDocumentMerger>();  
            services.AddScoped<IXmpMetaDataService, XmpMetadataService>(); 

            return services;
        }
    }
}