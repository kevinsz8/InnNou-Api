namespace InnNou.Application.Common.Interfaces
{
    // Local-disk PDF storage (see CLAUDE.md's "Order confirmation" note) — expected to be
    // swapped for an S3/Azure Blob-backed implementation later without any caller needing to
    // change, same seam shape as ISupplierLogoStorage.
    public interface IOrderPdfStorage
    {
        Task SaveAsync(Guid orderToken, byte[] pdfBytes, CancellationToken cancellationToken);
        Task<byte[]?> GetAsync(Guid orderToken, CancellationToken cancellationToken);
    }
}
