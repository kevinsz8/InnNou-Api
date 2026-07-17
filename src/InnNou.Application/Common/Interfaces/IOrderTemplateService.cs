using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    public interface IOrderTemplateService
    {
        Task<PagedResult<OrderTemplateDto>> GetPagedAsync(Guid? organizationToken, Guid? warehouseToken, string? searchText, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderTemplateDto?> GetByTokenAsync(Guid orderTemplateToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderTemplateDto?> CreateAsync(Guid warehouseToken, string name, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderTemplateDto?> RenameAsync(Guid orderTemplateToken, string name, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteAsync(Guid orderTemplateToken, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderTemplateLineDto?> AddLineAsync(Guid orderTemplateToken, Guid articleToken, decimal quantity, IRequestContext context, CancellationToken cancellationToken);
        Task<OrderTemplateLineDto?> EditLineAsync(Guid orderTemplateLineToken, decimal quantity, IRequestContext context, CancellationToken cancellationToken);
        Task<bool> DeleteLineAsync(Guid orderTemplateLineToken, IRequestContext context, CancellationToken cancellationToken);
        Task<(byte[] FileBytes, string FileName)> ExportAsync(Guid orderTemplateToken, string? language, IRequestContext context, CancellationToken cancellationToken);
        Task<ApplyOrderTemplateResultDto?> ApplyToOrderAsync(Guid orderTemplateToken, Guid orderToken, IRequestContext context, CancellationToken cancellationToken);
    }
}
