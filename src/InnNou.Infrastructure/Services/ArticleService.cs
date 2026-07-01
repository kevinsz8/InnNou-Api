using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class ArticleService(IDbConnectionFactory connectionFactory, IMapper mapper) : IArticleService
{
    private sealed class ArticlePageRow : Article { public int TotalCount { get; set; } }

    public async Task<PagedResult<ArticleDto>> GetPagedAsync(int pageNumber, int pageSize, int? supplierId, int? familyId, int? subFamilyId, string? searchText, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SupplierId", supplierId);
        p.Add("@FamilyId", familyId);
        p.Add("@SubFamilyId", subFamilyId);
        p.Add("@SearchText", searchText);
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<ArticlePageRow>(
            "sp_Article_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<ArticleDto>
        {
            Items = mapper.MapList<ArticleDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<ArticleDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticleToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ArticleDto>(row);
    }

    public async Task<bool> ExistsBySupplierSkuAsync(int supplierId, string supplierSku, Guid? excludeToken, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SupplierId", supplierId);
        p.Add("@SupplierSku", supplierSku);
        p.Add("@ExcludeToken", excludeToken);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_Article_ExistsBySupplierSku", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<ArticleDto?> CreateAsync(ArticleDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticleToken", Guid.NewGuid());
        p.Add("@SupplierId", dto.SupplierId);
        p.Add("@Name", dto.Name);
        p.Add("@Description", dto.Description);
        p.Add("@SupplierSku", dto.SupplierSku);
        p.Add("@Barcode", dto.Barcode);
        p.Add("@Brand", dto.Brand);
        p.Add("@FamilyId", dto.FamilyId);
        p.Add("@SubFamilyId", dto.SubFamilyId);
        p.Add("@PurchaseUnitId", dto.PurchaseUnitId);
        p.Add("@PurchaseQuantity", dto.PurchaseQuantity);
        p.Add("@ContentUnitId", dto.ContentUnitId);
        p.Add("@ContentQuantity", dto.ContentQuantity);
        p.Add("@BaseUnitId", dto.BaseUnitId);
        p.Add("@MinimumOrderQty", dto.MinimumOrderQty);
        p.Add("@LeadTimeDays", dto.LeadTimeDays);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ArticleDto>(row);
    }

    public async Task<ArticleDto?> EditAsync(ArticleDto dto, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticleToken", dto.ArticleToken);
        p.Add("@Name", dto.Name);
        p.Add("@Description", dto.Description);
        p.Add("@SupplierSku", dto.SupplierSku);
        p.Add("@Barcode", dto.Barcode);
        p.Add("@Brand", dto.Brand);
        p.Add("@FamilyId", dto.FamilyId);
        p.Add("@SubFamilyId", dto.SubFamilyId);
        p.Add("@PurchaseUnitId", dto.PurchaseUnitId);
        p.Add("@PurchaseQuantity", dto.PurchaseQuantity);
        p.Add("@ContentUnitId", dto.ContentUnitId);
        p.Add("@ContentQuantity", dto.ContentQuantity);
        p.Add("@BaseUnitId", dto.BaseUnitId);
        p.Add("@MinimumOrderQty", dto.MinimumOrderQty);
        p.Add("@LeadTimeDays", dto.LeadTimeDays);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<Article>(
            "sp_Article_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<ArticleDto>(row);
    }

    public async Task<bool> DeleteAsync(Guid token, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@ArticleToken", token);
        p.Add("@DeletedBy", context.ActorUserToken.ToString());
        try
        {
            await connection.ExecuteAsync("sp_Article_SoftDelete", p, commandType: CommandType.StoredProcedure);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
