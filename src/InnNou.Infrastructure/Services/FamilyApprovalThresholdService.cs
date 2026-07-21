using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Models;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class FamilyApprovalThresholdService(IDbConnectionFactory connectionFactory, IMapper mapper) : IFamilyApprovalThresholdService
{
    private const int StaffRoleLevel = 20;
    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    private static void EnsureCanManage(IRequestContext context, int organizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel) return;
        if (context.RoleLevel >= StaffRoleLevel && context.OrganizationId == organizationId) return;
        throw new ApiException(ErrorCodes.FamilyApprovalThresholdForbidden, "Insufficient permissions to manage this organization's approval thresholds.", 403);
    }

    private static async Task<Organization> ResolveAssociateOrganizationAsync(IDbConnection connection, Guid organizationToken)
    {
        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken", new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null }, commandType: CommandType.StoredProcedure);

        if (organization is null)
            throw new ApiException(ErrorCodes.OrganizationNotFound, "Organization not found.", 404);

        if (organization.OrganizationTypeCode != OrganizationTypeCodes.Associate)
            throw new ApiException(ErrorCodes.FamilyApprovalThresholdOrganizationNotAssociate, "Approval thresholds can only be configured for an Asociado organization.", 400);

        return organization;
    }

    private static async Task<Family> ResolveFamilyAsync(IDbConnection connection, Guid familyToken)
    {
        var family = await connection.QueryFirstOrDefaultAsync<Family>(
            "sp_Family_GetByToken", new { FamilyToken = familyToken }, commandType: CommandType.StoredProcedure);
        if (family is null)
            throw new ApiException(ErrorCodes.FamilyNotFound, "Family not found.", 404);
        return family;
    }

    // Approver must be within the target organization's own hierarchy (that Asociado or its
    // Super Asociado) — reuses sp_Organization_IsInHierarchy walked from the approver's own
    // organization as root, same idiom used everywhere else scoping is hierarchy-based.
    private static async Task<int> ResolveApproverUserIdAsync(IDbConnection connection, Guid approverUserToken, int organizationId)
    {
        var user = await connection.QueryFirstOrDefaultAsync<UserWithRoleResult>(
            "sp_User_GetByToken", new { UserToken = approverUserToken }, commandType: CommandType.StoredProcedure);

        if (user is null || !user.OrganizationId.HasValue)
            throw new ApiException(ErrorCodes.FamilyApprovalThresholdApproverNotFound, "Approver not found.", 404);

        var canApprove = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = user.OrganizationId.Value, TargetOrganizationId = organizationId },
            commandType: CommandType.StoredProcedure);

        if (canApprove != 1)
            throw new ApiException(ErrorCodes.FamilyApprovalThresholdApproverOutsideHierarchy, "The designated approver must belong to this organization or its Super Asociado.", 400);

        return user.UserId;
    }

    private sealed class FamilyApprovalThresholdPageRow : FamilyApprovalThreshold { public int TotalCount { get; set; } }

    public async Task<PagedResult<FamilyApprovalThresholdDto>> GetPagedAsync(Guid organizationToken, int pageNumber, int pageSize, Guid? familyToken, bool includeInactive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();
        var organization = await ResolveAssociateOrganizationAsync(connection, organizationToken);

        int? familyId = null;
        if (familyToken.HasValue)
            familyId = (await ResolveFamilyAsync(connection, familyToken.Value)).FamilyId;

        var p = new DynamicParameters();
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@FamilyId", familyId);
        p.Add("@IncludeInactive", includeInactive);
        var rows = (await connection.QueryAsync<FamilyApprovalThresholdPageRow>(
            "sp_FamilyApprovalThreshold_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<FamilyApprovalThresholdDto>
        {
            Items = mapper.MapList<FamilyApprovalThresholdDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<FamilyApprovalThresholdDto?> GetByTokenAsync(Guid token, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var row = await connection.QueryFirstOrDefaultAsync<FamilyApprovalThreshold>(
            "sp_FamilyApprovalThreshold_GetByToken", new { FamilyApprovalThresholdToken = token }, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyApprovalThresholdDto>(row);
    }

    public async Task<FamilyApprovalThresholdDto?> CreateAsync(Guid organizationToken, Guid familyToken, int level, decimal thresholdAmount, Guid approverUserToken, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        var organization = await ResolveAssociateOrganizationAsync(connection, organizationToken);
        EnsureCanManage(context, organization.OrganizationId);

        if (level < 1)
            throw new ApiException(ErrorCodes.FamilyApprovalThresholdInvalidLevel, "Level must be 1 or greater.", 400);
        if (thresholdAmount <= 0)
            throw new ApiException(ErrorCodes.FamilyApprovalThresholdInvalidAmount, "Threshold amount must be greater than zero.", 400);

        var family = await ResolveFamilyAsync(connection, familyToken);

        // Level must be contiguous from 1, and ThresholdAmount strictly increasing with Level —
        // same "no gaps, ordered" validation ArticlePackagingLevel.SequenceOrder already applies.
        var existingLevels = (await connection.QueryAsync<FamilyApprovalThreshold>(
            "sp_FamilyApprovalThreshold_GetPaged",
            new { OrganizationId = organization.OrganizationId, PageNumber = 1, PageSize = MaxPageSize, FamilyId = family.FamilyId, IncludeInactive = true },
            commandType: CommandType.StoredProcedure)).ToList();

        if (level != existingLevels.Count + 1)
            throw new ApiException(ErrorCodes.FamilyApprovalThresholdInvalidLevel, $"Levels must be added in order — the next level for this Family is {existingLevels.Count + 1}.", 400);

        if (existingLevels.Count > 0 && thresholdAmount <= existingLevels.Max(l => l.ThresholdAmount))
            throw new ApiException(ErrorCodes.FamilyApprovalThresholdInvalidAmount, "Each level's threshold amount must be greater than the previous level's.", 400);

        var approverUserId = await ResolveApproverUserIdAsync(connection, approverUserToken, organization.OrganizationId);

        var p = new DynamicParameters();
        p.Add("@FamilyApprovalThresholdToken", Guid.NewGuid());
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@FamilyId", family.FamilyId);
        p.Add("@Level", level);
        p.Add("@ThresholdAmount", thresholdAmount);
        p.Add("@ApproverUserId", approverUserId);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<FamilyApprovalThreshold>(
            "sp_FamilyApprovalThreshold_Create", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyApprovalThresholdDto>(row);
    }

    public async Task<FamilyApprovalThresholdDto?> EditAsync(Guid token, decimal thresholdAmount, Guid approverUserToken, IRequestContext context, CancellationToken cancellationToken = default)
    {
        if (thresholdAmount <= 0)
            throw new ApiException(ErrorCodes.FamilyApprovalThresholdInvalidAmount, "Threshold amount must be greater than zero.", 400);

        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<FamilyApprovalThreshold>(
            "sp_FamilyApprovalThreshold_GetByToken", new { FamilyApprovalThresholdToken = token }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            return null;

        EnsureCanManage(context, existing.OrganizationId);

        // Re-validate ordering against sibling levels — an edit that would break the
        // strictly-increasing chain is rejected the same way Create would reject it.
        var siblingLevels = (await connection.QueryAsync<FamilyApprovalThreshold>(
            "sp_FamilyApprovalThreshold_GetPaged",
            new { OrganizationId = existing.OrganizationId, PageNumber = 1, PageSize = MaxPageSize, FamilyId = existing.FamilyId, IncludeInactive = true },
            commandType: CommandType.StoredProcedure))
            .Where(l => l.FamilyApprovalThresholdId != existing.FamilyApprovalThresholdId)
            .ToList();

        var lowerMax = siblingLevels.Where(l => l.Level < existing.Level).Select(l => (decimal?)l.ThresholdAmount).Max();
        var higherMin = siblingLevels.Where(l => l.Level > existing.Level).Select(l => (decimal?)l.ThresholdAmount).Min();
        if (lowerMax.HasValue && thresholdAmount <= lowerMax.Value)
            throw new ApiException(ErrorCodes.FamilyApprovalThresholdInvalidAmount, "This level's threshold amount must be greater than the previous level's.", 400);
        if (higherMin.HasValue && thresholdAmount >= higherMin.Value)
            throw new ApiException(ErrorCodes.FamilyApprovalThresholdInvalidAmount, "This level's threshold amount must be less than the next level's.", 400);

        var approverUserId = await ResolveApproverUserIdAsync(connection, approverUserToken, existing.OrganizationId);

        var p = new DynamicParameters();
        p.Add("@FamilyApprovalThresholdToken", token);
        p.Add("@ThresholdAmount", thresholdAmount);
        p.Add("@ApproverUserId", approverUserId);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<FamilyApprovalThreshold>(
            "sp_FamilyApprovalThreshold_Update", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyApprovalThresholdDto>(row);
    }

    public async Task<FamilyApprovalThresholdDto?> SetActiveAsync(Guid token, bool isActive, IRequestContext context, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<FamilyApprovalThreshold>(
            "sp_FamilyApprovalThreshold_GetByToken", new { FamilyApprovalThresholdToken = token }, commandType: CommandType.StoredProcedure);
        if (existing is null)
            return null;

        EnsureCanManage(context, existing.OrganizationId);

        var p = new DynamicParameters();
        p.Add("@FamilyApprovalThresholdToken", token);
        p.Add("@IsActive", isActive);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());
        var row = await connection.QueryFirstOrDefaultAsync<FamilyApprovalThreshold>(
            "sp_FamilyApprovalThreshold_SetActive", p, commandType: CommandType.StoredProcedure);
        return row is null ? null : mapper.Map<FamilyApprovalThresholdDto>(row);
    }
}
