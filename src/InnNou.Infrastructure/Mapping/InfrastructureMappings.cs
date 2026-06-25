using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Models;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using UserEntity = InnNou.Infrastructure.Repositories.DbEntities.User;
using HotelEntity = InnNou.Infrastructure.Repositories.DbEntities.Hotel;
using RoleEntity = InnNou.Infrastructure.Repositories.DbEntities.Role;
using SupplierEntity = InnNou.Infrastructure.Repositories.DbEntities.Supplier;

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
        }
    }
}
