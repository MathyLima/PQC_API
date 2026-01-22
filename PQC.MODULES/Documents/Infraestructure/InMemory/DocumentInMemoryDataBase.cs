using PQC.MODULES.Documents.Domain.Entities;

namespace PQC.MODULES.Documents.Infraestructure.InMemory
{
    public static class DocumentInMemoryDatabase
    {
        public static List<StoredDocument> Documents { get; } = new();
    }
}