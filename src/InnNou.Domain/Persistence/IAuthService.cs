using InnNou.Domain.Models;

namespace InnNou.Domain.Persistence
{
    public interface IAuthService
    {
        Task<Login?> LoginAsync(string email, string password, CancellationToken cancellationToken);
        Task<Login?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
        Task<Login?> ImpersonateAsync(Guid actorUserToken, Guid targetUserToken, CancellationToken cancellationToken);
        Task<Login?> ImpersonateSupplierAsync(Guid actorUserToken, Guid supplierToken, CancellationToken cancellationToken);
        Task<Login?> ImpersonateWarehouseContactAsync(Guid actorUserToken, Guid warehouseContactToken, CancellationToken cancellationToken);
        Task<Login?> ImpersonateOrganizationAsync(Guid actorUserToken, Guid organizationToken, CancellationToken cancellationToken);
        Task<Login?> StopImpersonationAsync(Guid actorUserToken, CancellationToken cancellationToken);

        // Revokes the presented refresh token server-side — always succeeds (revoking an
        // already-revoked/unknown token is a no-op, not an error) so a client can call this
        // unconditionally on logout without special-casing an already-expired session.
        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);
    }
}
