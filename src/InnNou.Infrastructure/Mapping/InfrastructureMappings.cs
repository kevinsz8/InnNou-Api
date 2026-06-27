using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Models;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using UserEntity = InnNou.Infrastructure.Repositories.DbEntities.User;
using HotelEntity = InnNou.Infrastructure.Repositories.DbEntities.Hotel;
using RoleEntity = InnNou.Infrastructure.Repositories.DbEntities.Role;
using SupplierEntity = InnNou.Infrastructure.Repositories.DbEntities.Supplier;
using UnitTypeEntity = InnNou.Infrastructure.Repositories.DbEntities.UnitType;
using UnitOfMeasureEntity = InnNou.Infrastructure.Repositories.DbEntities.UnitOfMeasure;
using UnitConversionRateEntity = InnNou.Infrastructure.Repositories.DbEntities.UnitConversionRate;
using FamilyEntity = InnNou.Infrastructure.Repositories.DbEntities.Family;
using SubFamilyEntity = InnNou.Infrastructure.Repositories.DbEntities.SubFamily;
using CategoryEntity = InnNou.Infrastructure.Repositories.DbEntities.Category;
using SubCategoryEntity = InnNou.Infrastructure.Repositories.DbEntities.SubCategory;

namespace InnNou.Infrastructure.Mapping
{
    public static class InfrastructureMappings
    {
        public static void Register(Mapper mapper)
        {
            mapper.Register<UserEntity, UserDto>(u => new UserDto
            {
                UserId = u.UserId,
                UserToken = u.UserToken,
                RoleId = u.RoleId,
                HotelId = u.HotelId,
                SupplierId = u.SupplierId,
                Email = u.Email,
                NormalizedEmail = u.NormalizedEmail,
                FirstName = u.FirstName,
                LastName = u.LastName,
                UserName = u.UserName,
                NormalizedUserName = u.NormalizedUserName,
                IsActive = u.IsActive,
                IsDeleted = u.IsDeleted
            });

            mapper.Register<UserWithRoleResult, UserDto>(u => new UserDto
            {
                UserId = u.UserId,
                UserToken = u.UserToken,
                RoleId = u.RoleId,
                HotelId = u.HotelId,
                SupplierId = u.SupplierId,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                UserName = u.UserName,
                IsActive = u.IsActive,
                IsDeleted = u.IsDeleted
            });

            mapper.Register<HotelEntity, HotelDto>(h => new HotelDto
            {
                HotelId = h.HotelId,
                HotelToken = h.HotelToken,
                Name = h.Name,
                NormalizedName = h.NormalizedName,
                LegalName = h.LegalName,
                Code = h.Code,
                ParentHotelId = h.ParentHotelId,
                TimeZone = h.TimeZone,
                CurrencyCode = h.CurrencyCode,
                LanguageCode = h.LanguageCode,
                IsActive = h.IsActive,
                IsDeleted = h.IsDeleted
            });

            mapper.Register<RoleEntity, RoleDto>(r => new RoleDto
            {
                RoleId = r.RoleId,
                RoleToken = r.RoleToken,
                Name = r.Name,
                NormalizedName = r.NormalizedName,
                Description = r.Description,
                RoleLevel = r.RoleLevel,
                CanImpersonate = r.CanImpersonate,
                IsActive = r.IsActive
            });

            mapper.Register<SupplierEntity, SupplierDto>(s => new SupplierDto
            {
                SupplierId = s.SupplierId,
                SupplierToken = s.SupplierToken,
                Name = s.Name,
                LegalName = s.LegalName,
                TaxId = s.TaxId,
                Email = s.Email,
                Phone = s.Phone,
                AddressLine1 = s.AddressLine1,
                AddressLine2 = s.AddressLine2,
                City = s.City,
                State = s.State,
                PostalCode = s.PostalCode,
                Country = s.Country,
                IsGlobal = s.IsGlobal,
                IsActive = s.IsActive,
                IsDeleted = s.IsDeleted
            });

            // Catalog entities → DTOs
            mapper.Register<UnitTypeEntity, UnitTypeDto>(e => new UnitTypeDto
            {
                UnitTypeId = e.UnitTypeId,
                UnitTypeToken = e.UnitTypeToken,
                Code = e.Code,
                IsSystem = e.IsSystem,
                IsActive = e.IsActive
            });
            mapper.Register<UnitOfMeasureEntity, UnitOfMeasureDto>(e => new UnitOfMeasureDto
            {
                UnitOfMeasureId = e.UnitOfMeasureId,
                UnitOfMeasureToken = e.UnitOfMeasureToken,
                UnitTypeId = e.UnitTypeId,
                Code = e.Code,
                Symbol = e.Symbol,
                Decimals = e.Decimals,
                IsSystem = e.IsSystem,
                IsActive = e.IsActive
            });
            mapper.Register<UnitConversionRateEntity, UnitConversionRateDto>(e => new UnitConversionRateDto
            {
                UnitConversionRateId = e.UnitConversionRateId,
                UnitConversionRateToken = e.UnitConversionRateToken,
                FromUnitOfMeasureId = e.FromUnitOfMeasureId,
                ToUnitOfMeasureId = e.ToUnitOfMeasureId,
                Factor = e.Factor,
                IsActive = e.IsActive
            });
            mapper.Register<FamilyEntity, FamilyDto>(e => new FamilyDto
            {
                FamilyId = e.FamilyId,
                FamilyToken = e.FamilyToken,
                Code = e.Code,
                IsSystem = e.IsSystem,
                IsActive = e.IsActive
            });
            mapper.Register<SubFamilyEntity, SubFamilyDto>(e => new SubFamilyDto
            {
                SubFamilyId = e.SubFamilyId,
                SubFamilyToken = e.SubFamilyToken,
                FamilyId = e.FamilyId,
                Code = e.Code,
                IsSystem = e.IsSystem,
                IsActive = e.IsActive
            });
            mapper.Register<CategoryEntity, CategoryDto>(e => new CategoryDto
            {
                CategoryId = e.CategoryId,
                CategoryToken = e.CategoryToken,
                Code = e.Code,
                IsSystem = e.IsSystem,
                IsActive = e.IsActive
            });
            mapper.Register<SubCategoryEntity, SubCategoryDto>(e => new SubCategoryDto
            {
                SubCategoryId = e.SubCategoryId,
                SubCategoryToken = e.SubCategoryToken,
                CategoryId = e.CategoryId,
                Code = e.Code,
                IsSystem = e.IsSystem,
                IsActive = e.IsActive
            });
        }
    }
}
