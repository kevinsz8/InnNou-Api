using InnNou.Domain.Dtos;

namespace InnNou.Domain.Persistence
{
    public interface ITenantService
    {
        Task<TenantDto?> CreateTenantAsync(string name, string slug, CancellationToken cancellationToken);
        Task<List<TenantDto>> GetTenantsAsync(CancellationToken cancellationToken);
        Task<TenantDto?> GetTenantByIdAsync(Guid tenantId, CancellationToken cancellationToken);
    }
}
