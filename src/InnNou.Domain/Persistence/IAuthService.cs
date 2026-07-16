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
    }
}
