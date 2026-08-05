using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;

namespace InnNou.Application.Common.Interfaces
{
    // Value-add on top of Requisiciones Internas (see .claude/RequisitionsModule.md): a per-
    // (Department, Article) minimum + reorder quantity, mirroring Warehouse Par Levels'
    // Minimum/ReorderQuantity shape but with no Seasonal/Event override layer in V1 (see the
    // migration's own header comment for why that's a deliberate, revisitable scoping choice).
    //
    // Critical difference from IParLevelService: a Department has no StockLevels of its own, so
    // "suggested" is never a live on-hand-vs-minimum comparison — it's resolved from real
    // CONSUMPTION history (a consumption-pace + elapsed-time signal), never a fabricated balance.
    // See sp_DepartmentParLevel_GetSuggested's own header comment for the exact formula.
    public interface IDepartmentParLevelService
    {
        Task<DepartmentParLevelDto?> CreateAsync(Guid departmentToken, Guid articleToken, decimal minimumQuantity, decimal reorderQuantity, IRequestContext context, CancellationToken cancellationToken);
        Task<DepartmentParLevelDto?> EditAsync(Guid departmentParLevelToken, decimal minimumQuantity, decimal reorderQuantity, IRequestContext context, CancellationToken cancellationToken);
        Task<DepartmentParLevelDto?> SetActiveAsync(Guid departmentParLevelToken, bool isActive, IRequestContext context, CancellationToken cancellationToken);
        Task<DepartmentParLevelDto?> GetByDepartmentAndArticleAsync(Guid departmentToken, Guid articleToken, IRequestContext context, CancellationToken cancellationToken);
        Task<PagedResult<SuggestedRequisitionDto>> GetSuggestedAsync(Guid? organizationToken, Guid? departmentToken, Guid? articleToken, int pageNumber, int pageSize, IRequestContext context, CancellationToken cancellationToken);
    }
}
