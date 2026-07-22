namespace InnNou.Application.Common.Interfaces
{
    // Provider-agnostic email seam — current implementation talks to a Brevo SMTP relay, but
    // nothing outside the implementation knows that (same "swap later without callers changing"
    // reasoning as ISupplierLogoStorage/IOrderPdfStorage below).
    public interface IEmailSender
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
    }
}
