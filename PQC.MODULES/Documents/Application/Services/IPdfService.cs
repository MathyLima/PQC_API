namespace PQC.MODULES.Documents.Application.Services
{
    public interface IPdfService
    {
        Task<string> ProcessPdfAsync(IFormFile file);
    }
}
