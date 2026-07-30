namespace InnNou.Application.Responses
{
    public class UploadSupplierInvoiceAttachmentCommandResponse
    {
        public Guid SupplierInvoiceToken { get; set; }
        public bool Uploaded { get; set; }
    }
}
