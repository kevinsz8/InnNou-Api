using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IOrderService
    {
        Task<PagedResult<OrderDto>> GetPagedAsync(Guid? warehouseToken, string? status, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> GetByTokenAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> CreateAsync(Guid warehouseToken, string? notes, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderLineDto?> AddLineAsync(Guid orderToken, Guid articleToken, decimal quantity, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderLineDto?> EditLineAsync(Guid orderLineToken, decimal quantity, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteLineAsync(Guid orderLineToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> SubmitAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderDto?> CancelAsync(Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
