using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using CommonUser = InnNou.Application.Responses.Common.User;
using CommonHotel = InnNou.Application.Responses.Common.Hotel;
using CommonRole = InnNou.Application.Responses.Common.Role;
using CommonSupplier = InnNou.Application.Responses.Common.Supplier;

namespace InnNou.Application.Mapping
{
    public static class ApplicationMappings
    {
        public static void Register(Mapper mapper)
        {
            // User
            mapper.Register<CreateUserCommandRequest, UserDto>(r => new UserDto
            {
                Email = r.Email,
                Password = r.Password,
                FirstName = r.FirstName,
                LastName = r.LastName,
                UserName = r.UserName,
                RoleId = r.RoleId,
                HotelId = r.HotelId,
                SupplierId = r.SupplierId
            });
            mapper.Register<UserDto, CreateUserCommandResponse>(d => new CreateUserCommandResponse
            {
                UserToken = d.UserToken,
                Email = d.Email
            });
            mapper.Register<EditUserCommandRequest, UserDto>(r => new UserDto
            {
                UserId = r.UserId,
                UserToken = r.UserToken,
                Email = r.Email ?? string.Empty,
                FirstName = r.FirstName ?? string.Empty,
                LastName = r.LastName ?? string.Empty,
                Password = r.Password ?? string.Empty,
                UserName = r.UserName ?? string.Empty
            });
            mapper.Register<UserDto, EditUserCommandResponse>(d => new EditUserCommandResponse
            {
                UserId = d.UserId,
                UserToken = d.UserToken,
                Email = d.Email,
                FirstName = d.FirstName,
                LastName = d.LastName,
                UserName = d.UserName,
                IsActive = d.IsActive
            });
            mapper.Register<UserDto, CommonUser>(d => new CommonUser
            {
                UserId = d.UserId,
                UserToken = d.UserToken,
                Email = d.Email,
                FirstName = d.FirstName,
                LastName = d.LastName,
                UserName = d.UserName,
                HotelId = d.HotelId,
                SupplierId = d.SupplierId,
                RoleId = d.RoleId,
                IsActive = d.IsActive
            });

            // Hotel
            mapper.Register<CreateHotelCommandRequest, HotelDto>(r => new HotelDto
            {
                Name = r.Name,
                LegalName = r.LegalName,
                Code = r.Code,
                ParentHotelId = r.ParentHotelId,
                TimeZone = r.TimeZone,
                CurrencyCode = r.CurrencyCode,
                LanguageCode = r.LanguageCode
            });
            mapper.Register<HotelDto, CreateHotelCommandResponse>(d => new CreateHotelCommandResponse
            {
                HotelId = d.HotelId,
                HotelToken = d.HotelToken,
                Name = d.Name,
                LegalName = d.LegalName,
                Code = d.Code,
                ParentHotelId = d.ParentHotelId,
                TimeZone = d.TimeZone,
                CurrencyCode = d.CurrencyCode,
                LanguageCode = d.LanguageCode,
                IsActive = d.IsActive
            });
            mapper.Register<EditHotelCommandRequest, HotelDto>(r => new HotelDto
            {
                HotelToken = r.HotelToken,
                Name = r.Name ?? string.Empty,
                LegalName = r.LegalName,
                Code = r.Code,
                ParentHotelId = r.ParentHotelId,
                TimeZone = r.TimeZone,
                CurrencyCode = r.CurrencyCode,
                LanguageCode = r.LanguageCode
            });
            mapper.Register<HotelDto, EditHotelCommandResponse>(d => new EditHotelCommandResponse
            {
                HotelId = d.HotelId,
                HotelToken = d.HotelToken,
                Name = d.Name,
                LegalName = d.LegalName,
                Code = d.Code,
                ParentHotelId = d.ParentHotelId,
                TimeZone = d.TimeZone,
                CurrencyCode = d.CurrencyCode,
                LanguageCode = d.LanguageCode,
                IsActive = d.IsActive
            });
            mapper.Register<HotelDto, CommonHotel>(d => new CommonHotel
            {
                HotelId = d.HotelId,
                HotelToken = d.HotelToken,
                Name = d.Name,
                ParentHotelId = d.ParentHotelId,
                TimeZone = d.TimeZone,
                CurrencyCode = d.CurrencyCode,
                LanguageCode = d.LanguageCode,
                IsActive = d.IsActive
            });

            // Role
            mapper.Register<RoleDto, CommonRole>(d => new CommonRole
            {
                RoleId = d.RoleId,
                RoleToken = d.RoleToken,
                Name = d.Name,
                Description = d.Description,
                RoleLevel = d.RoleLevel,
                CanImpersonate = d.CanImpersonate
            });

            // Supplier
            mapper.Register<CreateSupplierCommandRequest, SupplierDto>(r => new SupplierDto
            {
                Name = r.Name,
                LegalName = r.LegalName,
                TaxId = r.TaxId,
                Email = r.Email,
                Phone = r.Phone,
                AddressLine1 = r.AddressLine1,
                AddressLine2 = r.AddressLine2,
                City = r.City,
                State = r.State,
                PostalCode = r.PostalCode,
                Country = r.Country,
                IsGlobal = r.IsGlobal
            });
            mapper.Register<SupplierDto, CreateSupplierCommandResponse>(d => new CreateSupplierCommandResponse
            {
                SupplierId = d.SupplierId,
                SupplierToken = d.SupplierToken,
                Name = d.Name,
                LegalName = d.LegalName,
                TaxId = d.TaxId,
                Email = d.Email,
                Phone = d.Phone,
                AddressLine1 = d.AddressLine1,
                AddressLine2 = d.AddressLine2,
                City = d.City,
                State = d.State,
                PostalCode = d.PostalCode,
                Country = d.Country,
                IsGlobal = d.IsGlobal ?? false,
                IsActive = d.IsActive
            });
            mapper.Register<EditSupplierCommandRequest, SupplierDto>(r => new SupplierDto
            {
                SupplierToken = r.SupplierToken,
                Name = r.Name ?? string.Empty,
                LegalName = r.LegalName,
                TaxId = r.TaxId,
                Email = r.Email,
                Phone = r.Phone,
                AddressLine1 = r.AddressLine1,
                AddressLine2 = r.AddressLine2,
                City = r.City,
                State = r.State,
                PostalCode = r.PostalCode,
                Country = r.Country,
                IsGlobal = r.IsGlobal
            });
            mapper.Register<SupplierDto, EditSupplierCommandResponse>(d => new EditSupplierCommandResponse
            {
                SupplierId = d.SupplierId,
                SupplierToken = d.SupplierToken,
                Name = d.Name,
                LegalName = d.LegalName,
                TaxId = d.TaxId,
                Email = d.Email,
                Phone = d.Phone,
                AddressLine1 = d.AddressLine1,
                AddressLine2 = d.AddressLine2,
                City = d.City,
                State = d.State,
                PostalCode = d.PostalCode,
                Country = d.Country,
                IsGlobal = d.IsGlobal ?? false,
                IsActive = d.IsActive
            });
            mapper.Register<SupplierDto, CommonSupplier>(d => new CommonSupplier
            {
                SupplierId = d.SupplierId,
                SupplierToken = d.SupplierToken,
                Name = d.Name,
                LegalName = d.LegalName,
                TaxId = d.TaxId,
                Email = d.Email,
                Phone = d.Phone,
                AddressLine1 = d.AddressLine1,
                AddressLine2 = d.AddressLine2,
                City = d.City,
                State = d.State,
                PostalCode = d.PostalCode,
                Country = d.Country,
                IsGlobal = d.IsGlobal ?? false,
                IsActive = d.IsActive
            });
        }
    }
}
