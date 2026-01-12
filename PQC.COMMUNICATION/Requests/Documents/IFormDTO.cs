using Microsoft.AspNetCore.Http;


namespace PQC.COMMUNICATION.Requests.Documents
{
    public class IFormDTO
    {
        // O swagger nao consegue fazer a documentação se não houver essa camada de abstração
        public IFormFile? File { get; set; }
    }

}
