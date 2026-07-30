using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class TaxService(IDbConnectionFactory connectionFactory, IMapper mapper) : ITaxService
{
    // Rate configuration is SuperAdmin-only — a jurisdiction's tax rate is a legal fact, not an
    // org-level business setting, and there is no organization-hierarchy dimension to it at all.
    private const int SuperAdminRoleLevel = 100;

    public async Task<List<TaxCategoryDto>> GetTaxCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<TaxCategory>(
            "sp_TaxCategory_GetAll", commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<TaxCategoryDto>(rows);
    }

    public async Task<List<TaxJurisdictionDto>> GetTaxJurisdictionsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<TaxJurisdiction>(
            "sp_TaxJurisdiction_GetAll", commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<TaxJurisdictionDto>(rows);
    }

    public async Task<List<TaxRateGridRowDto>> GetTaxRateGridAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var rows = (await connection.QueryAsync<TaxRateGridRow>(
            "sp_TaxRate_GetAllWithJurisdictionAndCategory", commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<TaxRateGridRowDto>(rows);
    }

    public async Task<TaxRateGridRowDto?> UpsertTaxRateAsync(Guid taxJurisdictionToken, Guid taxCategoryToken, decimal ratePercent, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.TaxRateForbidden, "Only a SuperAdmin can configure tax rates.", 403);

        if (ratePercent < 0 || ratePercent > 100)
            throw new ApiException(ErrorCodes.TaxRateInvalidPercent, "A tax rate must be between 0 and 100.", 400);

        await using var connection = connectionFactory.CreateConnection();

        var jurisdiction = await connection.QueryFirstOrDefaultAsync<TaxJurisdiction>(
            "sp_TaxJurisdiction_GetByToken", new { TaxJurisdictionToken = taxJurisdictionToken }, commandType: CommandType.StoredProcedure);
        if (jurisdiction is null)
            throw new ApiException(ErrorCodes.TaxJurisdictionNotFound, "Tax jurisdiction not found.", 404);

        var category = await connection.QueryFirstOrDefaultAsync<TaxCategory>(
            "sp_TaxCategory_GetByToken", new { TaxCategoryToken = taxCategoryToken }, commandType: CommandType.StoredProcedure);
        if (category is null)
            throw new ApiException(ErrorCodes.TaxCategoryNotFound, "Tax category not found.", 404);

        var p = new DynamicParameters();
        p.Add("@TaxJurisdictionId", jurisdiction.TaxJurisdictionId);
        p.Add("@TaxCategoryId", category.TaxCategoryId);
        p.Add("@RatePercent", ratePercent);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        await connection.ExecuteAsync("sp_TaxRate_Upsert", p, commandType: CommandType.StoredProcedure);

        var grid = await GetTaxRateGridAsync(cancellationToken);
        return grid.FirstOrDefault(r => r.TaxJurisdictionToken == taxJurisdictionToken && r.TaxCategoryToken == taxCategoryToken);
    }
}
