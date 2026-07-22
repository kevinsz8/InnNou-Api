namespace InnNou.Application.Common.Interfaces
{
    // Local-disk logo storage (see CLAUDE.md's "Supplier logo" note) — expected to be swapped
    // for an S3/Azure Blob-backed implementation later without any caller needing to change,
    // since nothing outside the implementation knows the files live on local disk.
    public interface ISupplierLogoStorage
    {
        // Saves the file as this supplier's logo, replacing any previously saved logo
        // (including one with a different extension), and returns the public relative URL to
        // persist on the Supplier row.
        Task<string> SaveAsync(Guid supplierToken, Stream fileStream, string fileExtension, CancellationToken cancellationToken);

        // Removes any existing logo for this supplier. No-op if none exists.
        Task DeleteAsync(Guid supplierToken, CancellationToken cancellationToken);
    }
}
