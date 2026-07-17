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

public class OrganizationContactService(IDbConnectionFactory connectionFactory, IMapper mapper) : IOrganizationContactService
{
    private sealed class OrganizationContactPageRow : OrganizationContact { public int TotalCount { get; set; } }

    private const int SuperAdminRoleLevel = 100;
    private const int MaxPageSize = 100;

    // RoleLevel >= 100 manages/reads any organization's contacts. Below that, the caller must be
    // organization-scoped and the target must be within their own organization's hierarchy —
    // a caller with no organization (e.g. a Supplier-scoped login) is denied, never unrestricted.
    private static async Task<bool> CanManageOrganizationAsync(IDbConnection connection, IRequestContext context, int targetOrganizationId)
    {
        if (context.RoleLevel >= SuperAdminRoleLevel)
            return true;

        if (!context.OrganizationId.HasValue)
            return false;

        var canAccess = await connection.ExecuteScalarAsync<int>(
            "sp_Organization_IsInHierarchy",
            new { RootOrganizationId = context.OrganizationId.Value, TargetOrganizationId = targetOrganizationId },
            commandType: CommandType.StoredProcedure);

        return canAccess == 1;
    }

    public async Task<PagedResult<OrganizationContactDto>> GetPagedByOrganizationTokenAsync(
        Guid organizationToken,
        int pageNumber,
        int pageSize,
        string? searchText,
        bool includeInactive,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : Math.Min(pageSize, MaxPageSize);

        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new { OrganizationToken = organizationToken, RootOrganizationId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (organization is null || !await CanManageOrganizationAsync(connection, context, organization.OrganizationId))
            return new PagedResult<OrganizationContactDto> { Items = [], TotalCount = 0, PageNumber = safePageNumber, PageSize = safePageSize };

        var p = new DynamicParameters();
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@IncludeInactive", includeInactive);

        var rows = (await connection.QueryAsync<OrganizationContactPageRow>(
            "sp_OrganizationContact_GetPagedByOrganizationId", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<OrganizationContactDto>
        {
            Items = mapper.MapList<OrganizationContactDto>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<OrganizationContactDto?> GetByTokenAsync(Guid organizationContactToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var contact = await connection.QueryFirstOrDefaultAsync<OrganizationContact>(
            "sp_OrganizationContact_GetByToken",
            new { OrganizationContactToken = organizationContactToken },
            commandType: CommandType.StoredProcedure);

        if (contact is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, contact.OrganizationId))
            return null;

        return mapper.Map<OrganizationContactDto>(contact);
    }

    public async Task<OrganizationContactDto?> CreateAsync(OrganizationContactDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var organization = await connection.QueryFirstOrDefaultAsync<Organization>(
            "sp_Organization_GetByToken",
            new { OrganizationToken = dto.OrganizationToken, RootOrganizationId = (int?)null },
            commandType: CommandType.StoredProcedure);

        if (organization is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, organization.OrganizationId))
            throw new ApiException(ErrorCodes.OrganizationContactOutsideScope, "Cannot create contact for another organization.", 403);

        var p = new DynamicParameters();
        p.Add("@OrganizationContactToken", Guid.NewGuid());
        p.Add("@OrganizationId", organization.OrganizationId);
        p.Add("@ContactName", dto.ContactName);
        p.Add("@ContactType", dto.ContactType);
        p.Add("@Department", dto.Department);
        p.Add("@Phone", dto.Phone);
        p.Add("@Mobile", dto.Mobile);
        p.Add("@Fax", dto.Fax);
        p.Add("@Email", dto.Email);
        p.Add("@Notes", dto.Notes);
        p.Add("@IsPrimary", dto.IsPrimary);
        p.Add("@CreatedUtc", DateTime.UtcNow);
        p.Add("@CreatedBy", context.ActorUserToken.ToString());

        var created = await connection.QueryFirstOrDefaultAsync<OrganizationContact>(
            "sp_OrganizationContact_Create", p, commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<OrganizationContactDto>(created);
    }

    public async Task<OrganizationContactDto?> EditAsync(OrganizationContactDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<OrganizationContact>(
            "sp_OrganizationContact_GetByToken",
            new { OrganizationContactToken = dto.OrganizationContactToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId))
            throw new ApiException(ErrorCodes.OrganizationContactOutsideScope, "Cannot edit contact from another organization.", 403);

        var p = new DynamicParameters();
        p.Add("@OrganizationContactToken", dto.OrganizationContactToken);
        p.Add("@ContactName", dto.ContactName);
        p.Add("@ContactType", dto.ContactType);
        p.Add("@Department", dto.Department);
        p.Add("@Phone", dto.Phone);
        p.Add("@Mobile", dto.Mobile);
        p.Add("@Fax", dto.Fax);
        p.Add("@Email", dto.Email);
        p.Add("@Notes", dto.Notes);
        p.Add("@IsPrimary", dto.IsPrimary);
        p.Add("@LastUpdatedUtc", DateTime.UtcNow);
        p.Add("@LastUpdatedBy", context.ActorUserToken.ToString());

        var updated = await connection.QueryFirstOrDefaultAsync<OrganizationContact>(
            "sp_OrganizationContact_Update", p, commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<OrganizationContactDto>(updated);
    }

    public async Task<bool> DeleteAsync(Guid organizationContactToken, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<OrganizationContact>(
            "sp_OrganizationContact_GetByToken",
            new { OrganizationContactToken = organizationContactToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        if (!await CanManageOrganizationAsync(connection, context, existing.OrganizationId))
            throw new ApiException(ErrorCodes.OrganizationContactOutsideScope, "Cannot delete contact from another organization.", 403);

        var now = DateTime.UtcNow;
        var actor = context.ActorUserToken.ToString();

        await connection.ExecuteAsync(
            "sp_OrganizationContact_SoftDelete",
            new
            {
                OrganizationContactToken = organizationContactToken,
                DeletedUtc = now,
                DeletedBy = actor,
                LastUpdatedUtc = now,
                LastUpdatedBy = actor
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }
}
