using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface IMenuService
    {
        Task<List<MenuItemDto>> GetVisibleForContextAsync(int roleLevel, int? organizationId, int? supplierId, CancellationToken cancellationToken = default);
    }
}
