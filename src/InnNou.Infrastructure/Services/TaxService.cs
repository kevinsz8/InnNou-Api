using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using Microsoft.Data.SqlClient;
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

    public async Task<TaxJurisdictionDto> CreateTaxJurisdictionAsync(string countryCode, string code, string name, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.TaxJurisdictionForbidden, "Only a SuperAdmin can create a tax jurisdiction.", 403);

        if (string.IsNullOrWhiteSpace(code))
            throw new ApiException(ErrorCodes.TaxJurisdictionCodeRequired, "A jurisdiction code is required.", 400);

        await using var connection = connectionFactory.CreateConnection();

        var country = await connection.QueryFirstOrDefaultAsync<Country>(
            "sp_Country_GetByCode", new { Code = countryCode.Trim().ToUpperInvariant() }, commandType: CommandType.StoredProcedure);
        if (country is null)
            throw new ApiException(ErrorCodes.TaxJurisdictionCountryNotFound, "Country not found.", 404);

        var p = new DynamicParameters();
        p.Add("@CountryId", country.CountryId);
        p.Add("@Code", code.Trim());
        p.Add("@Name", name.Trim());

        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<TaxJurisdiction>(
                "sp_TaxJurisdiction_Create", p, commandType: CommandType.StoredProcedure);
            return mapper.Map<TaxJurisdictionDto>(row!);
        }
        catch (SqlException ex) when (ex.Message.Contains(ErrorCodes.TaxJurisdictionCountryNotFound, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.TaxJurisdictionCountryNotFound, "Country not found.", 404);
        }
        catch (SqlException ex) when (ex.Message.Contains(ErrorCodes.TaxJurisdictionCodeAlreadyExists, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.TaxJurisdictionCodeAlreadyExists, "A tax jurisdiction with this code already exists.", 409);
        }
    }

    public async Task<TaxCategoryDto> CreateTaxCategoryAsync(string code, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.TaxCategoryForbidden, "Only a SuperAdmin can create a tax category.", 403);

        if (string.IsNullOrWhiteSpace(code))
            throw new ApiException(ErrorCodes.TaxCategoryCodeRequired, "A category code is required.", 400);

        await using var connection = connectionFactory.CreateConnection();

        try
        {
            var row = await connection.QueryFirstOrDefaultAsync<TaxCategory>(
                "sp_TaxCategory_Create", new { Code = code.Trim() }, commandType: CommandType.StoredProcedure);
            return mapper.Map<TaxCategoryDto>(row!);
        }
        catch (SqlException ex) when (ex.Message.Contains(ErrorCodes.TaxCategoryCodeAlreadyExists, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.TaxCategoryCodeAlreadyExists, "A tax category with this code already exists.", 409);
        }
    }

    public async Task<List<FamilyTaxCategoryOverrideDto>> GetFamilyTaxCategoryOverridesAsync(Guid familyToken, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var family = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_GetByToken", new { FamilyToken = familyToken }, commandType: CommandType.StoredProcedure);
        if (family is null)
            throw new ApiException(ErrorCodes.FamilyNotFound, "Family not found.", 404);

        var rows = (await connection.QueryAsync<FamilyTaxCategoryOverride>(
            "sp_FamilyTaxCategoryOverride_GetByFamily", new { family.FamilyId }, commandType: CommandType.StoredProcedure)).ToList();
        return mapper.MapList<FamilyTaxCategoryOverrideDto>(rows);
    }

    public async Task<FamilyTaxCategoryOverrideDto> UpsertFamilyTaxCategoryOverrideAsync(Guid familyToken, Guid taxJurisdictionToken, Guid taxCategoryToken, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.FamilyTaxCategoryOverrideForbidden, "Only a SuperAdmin can configure a family's per-jurisdiction tax category.", 403);

        await using var connection = connectionFactory.CreateConnection();

        var family = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_GetByToken", new { FamilyToken = familyToken }, commandType: CommandType.StoredProcedure);
        if (family is null)
            throw new ApiException(ErrorCodes.FamilyNotFound, "Family not found.", 404);

        var jurisdiction = await connection.QueryFirstOrDefaultAsync<TaxJurisdiction>(
            "sp_TaxJurisdiction_GetByToken", new { TaxJurisdictionToken = taxJurisdictionToken }, commandType: CommandType.StoredProcedure);
        if (jurisdiction is null)
            throw new ApiException(ErrorCodes.TaxJurisdictionNotFound, "Tax jurisdiction not found.", 404);

        var category = await connection.QueryFirstOrDefaultAsync<TaxCategory>(
            "sp_TaxCategory_GetByToken", new { TaxCategoryToken = taxCategoryToken }, commandType: CommandType.StoredProcedure);
        if (category is null)
            throw new ApiException(ErrorCodes.TaxCategoryNotFound, "Tax category not found.", 404);

        var p = new DynamicParameters();
        p.Add("@FamilyId", family.FamilyId);
        p.Add("@TaxJurisdictionId", jurisdiction.TaxJurisdictionId);
        p.Add("@TaxCategoryId", category.TaxCategoryId);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<FamilyTaxCategoryOverride>(
            "sp_FamilyTaxCategoryOverride_Upsert", p, commandType: CommandType.StoredProcedure);
        return mapper.Map<FamilyTaxCategoryOverrideDto>(row!);
    }

    public async Task DeleteFamilyTaxCategoryOverrideAsync(Guid familyToken, Guid taxJurisdictionToken, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (context.RoleLevel < SuperAdminRoleLevel)
            throw new ApiException(ErrorCodes.FamilyTaxCategoryOverrideForbidden, "Only a SuperAdmin can configure a family's per-jurisdiction tax category.", 403);

        await using var connection = connectionFactory.CreateConnection();

        var family = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_GetByToken", new { FamilyToken = familyToken }, commandType: CommandType.StoredProcedure);
        if (family is null)
            throw new ApiException(ErrorCodes.FamilyNotFound, "Family not found.", 404);

        var jurisdiction = await connection.QueryFirstOrDefaultAsync<TaxJurisdiction>(
            "sp_TaxJurisdiction_GetByToken", new { TaxJurisdictionToken = taxJurisdictionToken }, commandType: CommandType.StoredProcedure);
        if (jurisdiction is null)
            throw new ApiException(ErrorCodes.TaxJurisdictionNotFound, "Tax jurisdiction not found.", 404);

        try
        {
            await connection.ExecuteAsync(
                "sp_FamilyTaxCategoryOverride_Delete",
                new { family.FamilyId, jurisdiction.TaxJurisdictionId },
                commandType: CommandType.StoredProcedure);
        }
        catch (SqlException ex) when (ex.Message.Contains(ErrorCodes.FamilyTaxCategoryOverrideNotFound, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(ErrorCodes.FamilyTaxCategoryOverrideNotFound, "No override exists for this family in this jurisdiction.", 404);
        }
    }
}
