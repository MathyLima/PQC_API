public class CreateDocumentContentRequest
{
    public byte[] Content { get; set; }       // conteúdo do PDF em bytes
    public string FileName { get; set; }      // nome do arquivo
    public string ContentType { get; set; }   // tipo MIME, ex: "application/pdf"
    public string UserId { get; set; }        // id do usuário
}
