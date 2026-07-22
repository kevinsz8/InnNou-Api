namespace InnNou.Application.Common.Interfaces
{
    public class EmailMessage
    {
        public required string ToAddress { get; set; }
        public required string Subject { get; set; }
        public required string HtmlBody { get; set; }
        public List<EmailAttachment> Attachments { get; set; } = [];
    }

    public class EmailAttachment
    {
        public required string FileName { get; set; }
        public required byte[] Content { get; set; }
        public string ContentType { get; set; } = "application/pdf";
    }
}
