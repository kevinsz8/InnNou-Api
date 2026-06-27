using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class SubCategoryService(IDbConnectionFactory connectionFactory, IMapper mapper) : ISubCategoryService
{
    public async Task<List<SubCategoryDto>> GetAllAsync(int? categoryId = null, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@CategoryId", categoryId);
        var rows = (await connection.QueryAsync<SubCategory>(
            "sp_SubCategory_GetAll", p, commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<SubCategoryDto>(rows);
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
