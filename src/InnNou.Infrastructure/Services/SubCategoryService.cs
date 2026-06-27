using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class SubCategoryService(IDbConnectionFactory connectionFactory, IMapper mapper) : ISubCategoryService
{
    private sealed class SubCategoryPageRow : SubCategory { public int TotalCount { get; set; } }

    public async Task<PagedResult<SubCategoryDto>> GetPagedAsync(int pageNumber, int pageSize, int? categoryId = null, string? searchText = null, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@CategoryId", categoryId);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        var rows = (await connection.QueryAsync<SubCategoryPageRow>(
            "sp_SubCategory_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();
        return new PagedResult<SubCategoryDto>
        {
            Items = mapper.MapList<SubCategoryDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<SubCategoryDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubCategoryToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<SubCategory>(
            "sp_SubCategory_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubCategoryDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, int categoryId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        p.Add("@CategoryId", categoryId);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_SubCategory_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<SubCategoryDto?> CreateAsync(SubCategoryDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubCategoryToken", Guid.NewGuid());
        p.Add("@CategoryId", dto.CategoryId);
        p.Add("@Code", dto.Code);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubCategory>(
            "sp_SubCategory_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubCategoryDto>(row);
    }

    public async Task<SubCategoryDto?> EditAsync(SubCategoryDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubCategoryToken", dto.SubCategoryToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubCategory>(
            "sp_SubCategory_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubCategoryDto>(row);
    }

    public async Task<SubCategoryDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@SubCategoryToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<SubCategory>(
            "sp_SubCategory_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<SubCategoryDto>(row);
    }
}
