using Dapper;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class CategoryService(IDbConnectionFactory connectionFactory, IMapper mapper) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<Category>(
            "sp_Category_GetAll", commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<CategoryDto>(rows);
    }

    public async Task<CategoryDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@CategoryToken", token);
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_GetByToken", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", code);
        return await connection.ExecuteScalarAsync<bool>(
            "sp_Category_ExistsByCode", p, commandType: CommandType.StoredProcedure);
    }

    public async Task<CategoryDto?> CreateAsync(CategoryDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@Code", dto.Code);
        p.Add("@CreatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }

    public async Task<CategoryDto?> EditAsync(CategoryDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@CategoryToken", dto.CategoryToken);
        p.Add("@Code", dto.Code);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }

    public async Task<CategoryDto?> SetActiveAsync(Guid token, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var p = new DynamicParameters();
        p.Add("@CategoryToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", "API");
        var row = await connection.QueryFirstOrDefaultAsync<Category>(
            "sp_Category_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<CategoryDto>(row);
    }
}
