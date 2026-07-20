using InnNou.Domain.Dtos;

namespace InnNou.Application.Common.Interfaces
{
    public interface ISupplierDeliveryZoneService
    {
        Task<List<SupplierDeliveryZoneDto>> GetBySupplierAsync(Guid? supplierToken, IRequestContext context, CancellationToken cancellationToken = default);
        Task<SupplierDeliveryZoneDto> CreateAsync(Guid? supplierToken, Guid zoneToken, int dayOfWeek, IRequestContext context, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(Guid? supplierToken, Guid zoneToken, int dayOfWeek, IRequestContext context, CancellationToken cancellationToken = default);
    }
}
