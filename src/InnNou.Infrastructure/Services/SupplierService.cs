using AutoMapper;
using Dapper;
using InnNou.Application.Common;
using InnNou.Application.Common.Interfaces;
using InnNou.Domain.Dtos;
using InnNou.Domain.Dtos.Common;
using InnNou.Infrastructure.Abstractions;
using InnNou.Infrastructure.Repositories.DbEntities;
using System.Data;

namespace InnNou.Infrastructure.Services;

public class SupplierService(IDbConnectionFactory connectionFactory, IMapper mapper) : ISupplierService
{
    private sealed class SupplierPageRow : Supplier { public int TotalCount { get; set; } }

    public async Task<PagedResult<SupplierDto>> GetSuppliersAsync(
        int pageNumber,
        int pageSize,
        string? searchField,
        string? searchText,
        IRequestContext context,
        CancellationToken cancellationToken)
    {
        if (context.RoleLevel < 100 && !context.SupplierId.HasValue)
            return new PagedResult<SupplierDto>
            {
                Items = [],
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

        var safePageNumber = pageNumber < 1 ? 1 : pageNumber;
        var safePageSize = pageSize < 1 ? 10 : pageSize;

        await using var connection = connectionFactory.CreateConnection();

        var p = new DynamicParameters();
        p.Add("@ContextRoleLevel", context.RoleLevel);
        p.Add("@ContextSupplierId", context.RoleLevel >= 100 ? (int?)null : context.SupplierId);
        p.Add("@SearchField", string.IsNullOrWhiteSpace(searchField) ? null : searchField.Trim().ToLower());
        p.Add("@SearchText", string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim().ToLower());
        p.Add("@PageNumber", safePageNumber);
        p.Add("@PageSize", safePageSize);

        var rows = (await connection.QueryAsync<SupplierPageRow>(
            "sp_Supplier_GetPaged", p, commandType: CommandType.StoredProcedure)).ToList();

        return new PagedResult<SupplierDto>
        {
            Items = mapper.Map<List<SupplierDto>>(rows),
            TotalCount = rows.FirstOrDefault()?.TotalCount ?? 0,
            PageNumber = safePageNumber,
            PageSize = safePageSize
        };
    }

    public async Task<bool> SupplierExistsAsync(string name, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var result = await connection.ExecuteScalarAsync<int>(
            "sp_Supplier_ExistsByName",
            new { NormalizedName = name.ToUpperInvariant() },
            commandType: CommandType.StoredProcedure);

        return result == 1;
    }

    public async Task<SupplierDto?> CreateSupplierAsync(SupplierDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < 100)
            throw new UnauthorizedAccessException("Only super admins can create suppliers.");

        await using var connection = connectionFactory.CreateConnection();

        var created = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_Create",
            new
            {
                SupplierToken = Guid.NewGuid(),
                Name = dto.Name,
                NormalizedName = dto.Name.ToUpperInvariant(),
                LegalName = dto.LegalName,
                TaxId = dto.TaxId,
                Email = dto.Email,
                Phone = dto.Phone,
                AddressLine1 = dto.AddressLine1,
                AddressLine2 = dto.AddressLine2,
                City = dto.City,
                State = dto.State,
                PostalCode = dto.PostalCode,
                Country = dto.Country,
                IsGlobal = dto.IsGlobal ?? false,
                IsActive = true,
                IsDeleted = false,
                CreatedUtc = DateTime.UtcNow,
                CreatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return created is null ? null : mapper.Map<SupplierDto>(created);
    }

    public async Task<SupplierDto?> EditSupplierAsync(SupplierDto dto, IRequestContext context, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken",
            new { SupplierToken = dto.SupplierToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return null;

        if (context.RoleLevel < 100 && context.SupplierId != existing.SupplierId)
            throw new UnauthorizedAccessException("Cannot edit another supplier.");

        var newName = !string.IsNullOrWhiteSpace(dto.Name) ? dto.Name : existing.Name;

        var updated = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_Update",
            new
            {
                SupplierToken = dto.SupplierToken,
                Name = newName,
                NormalizedName = newName.ToUpperInvariant(),
                LegalName = dto.LegalName ?? existing.LegalName,
                TaxId = dto.TaxId ?? existing.TaxId,
                Email = dto.Email ?? existing.Email,
                Phone = dto.Phone ?? existing.Phone,
                AddressLine1 = dto.AddressLine1 ?? existing.AddressLine1,
                AddressLine2 = dto.AddressLine2 ?? existing.AddressLine2,
                City = dto.City ?? existing.City,
                State = dto.State ?? existing.State,
                PostalCode = dto.PostalCode ?? existing.PostalCode,
                Country = dto.Country ?? existing.Country,
                IsGlobal = dto.IsGlobal ?? existing.IsGlobal,
                LastUpdatedUtc = DateTime.UtcNow,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return updated is null ? null : mapper.Map<SupplierDto>(updated);
    }

    public async Task<bool> DeleteSupplierAsync(Guid supplierToken, IRequestContext context, CancellationToken cancellationToken)
    {
        if (context.RoleLevel < 100)
            throw new UnauthorizedAccessException("Only super admins can delete suppliers.");

        await using var connection = connectionFactory.CreateConnection();

        var existing = await connection.QueryFirstOrDefaultAsync<Supplier>(
            "sp_Supplier_GetByToken",
            new { SupplierToken = supplierToken },
            commandType: CommandType.StoredProcedure);

        if (existing is null)
            return false;

        await connection.ExecuteAsync(
            "sp_Supplier_SoftDelete",
            new
            {
                SupplierToken = supplierToken,
                IsDeleted = true,
                LastUpdatedUtc = DateTime.UtcNow,
                LastUpdatedBy = context.ActorUserToken.ToString()
            },
            commandType: CommandType.StoredProcedure);

        return true;
    }
}
