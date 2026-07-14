using InnNou.Application.Requests;
using InnNou.Application.Responses;
using InnNou.Domain.Dtos;
using InnNou.Shared.Mapping;
using CommonUser = InnNou.Application.Responses.Common.User;
using CommonOrganization = InnNou.Application.Responses.Common.Organization;
using CommonRole = InnNou.Application.Responses.Common.Role;
using CommonSupplier = InnNou.Application.Responses.Common.Supplier;
using CommonUnitType = InnNou.Application.Responses.Common.UnitType;
using CommonUnitOfMeasure = InnNou.Application.Responses.Common.UnitOfMeasure;
using CommonUnitConversionRate = InnNou.Application.Responses.Common.UnitConversionRate;
using CommonFamily = InnNou.Application.Responses.Common.Family;
using CommonSubFamily = InnNou.Application.Responses.Common.SubFamily;
using CommonCategory = InnNou.Application.Responses.Common.Category;
using CommonSubCategory = InnNou.Application.Responses.Common.SubCategory;
using CommonOrganizationContact = InnNou.Application.Responses.Common.OrganizationContact;
using CommonArticle = InnNou.Application.Responses.Common.Article;
using CommonArticlePrice = InnNou.Application.Responses.Common.ArticlePrice;
using CommonCurrency = InnNou.Application.Responses.Common.Currency;
using CommonMenuItem = InnNou.Application.Responses.Common.MenuItem;

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
                OrganizationId = r.OrganizationId,
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
                OrganizationId = d.OrganizationId,
                SupplierId = d.SupplierId,
                RoleId = d.RoleId,
                IsActive = d.IsActive
            });
            mapper.Register<BulkImportRowErrorDto, BulkImportUserRowError>(d => new BulkImportUserRowError
            {
                RowNumber = d.RowNumber,
                Email = d.Email,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<BulkImportResultDto, BulkImportUsersCommandResponse>(d => new BulkImportUsersCommandResponse
            {
                TotalRows = d.TotalRows,
                SuccessCount = d.SuccessCount,
                FailureCount = d.FailureCount,
                Errors = mapper.MapList<BulkImportUserRowError>(d.Errors)
            });

            // Organization
            mapper.Register<CreateOrganizationCommandRequest, OrganizationDto>(r => new OrganizationDto
            {
                Name = r.Name,
                LegalName = r.LegalName,
                Code = r.Code,
                ParentOrganizationId = r.ParentOrganizationId,
                OrganizationTypeId = r.OrganizationTypeId ?? 0,
                TimeZone = r.TimeZone,
                CurrencyCode = r.CurrencyCode,
                LanguageCode = r.LanguageCode
            });
            mapper.Register<OrganizationDto, CreateOrganizationCommandResponse>(d => new CreateOrganizationCommandResponse
            {
                OrganizationId = d.OrganizationId,
                OrganizationToken = d.OrganizationToken,
                Name = d.Name,
                LegalName = d.LegalName,
                Code = d.Code,
                ParentOrganizationId = d.ParentOrganizationId,
                OrganizationTypeId = d.OrganizationTypeId,
                OrganizationTypeCode = d.OrganizationTypeCode,
                TimeZone = d.TimeZone,
                CurrencyCode = d.CurrencyCode,
                LanguageCode = d.LanguageCode,
                IsActive = d.IsActive
            });
            mapper.Register<EditOrganizationCommandRequest, OrganizationDto>(r => new OrganizationDto
            {
                OrganizationToken = r.OrganizationToken,
                Name = r.Name ?? string.Empty,
                LegalName = r.LegalName,
                Code = r.Code,
                ParentOrganizationId = r.ParentOrganizationId,
                OrganizationTypeId = r.OrganizationTypeId ?? 0,
                TimeZone = r.TimeZone,
                CurrencyCode = r.CurrencyCode,
                LanguageCode = r.LanguageCode
            });
            mapper.Register<OrganizationDto, EditOrganizationCommandResponse>(d => new EditOrganizationCommandResponse
            {
                OrganizationId = d.OrganizationId,
                OrganizationToken = d.OrganizationToken,
                Name = d.Name,
                LegalName = d.LegalName,
                Code = d.Code,
                ParentOrganizationId = d.ParentOrganizationId,
                OrganizationTypeId = d.OrganizationTypeId,
                OrganizationTypeCode = d.OrganizationTypeCode,
                TimeZone = d.TimeZone,
                CurrencyCode = d.CurrencyCode,
                LanguageCode = d.LanguageCode,
                IsActive = d.IsActive
            });
            mapper.Register<OrganizationDto, CommonOrganization>(d => new CommonOrganization
            {
                OrganizationId = d.OrganizationId,
                OrganizationToken = d.OrganizationToken,
                Name = d.Name,
                ParentOrganizationId = d.ParentOrganizationId,
                OrganizationTypeId = d.OrganizationTypeId,
                OrganizationTypeCode = d.OrganizationTypeCode,
                TimeZone = d.TimeZone,
                CurrencyCode = d.CurrencyCode,
                LanguageCode = d.LanguageCode,
                IsActive = d.IsActive
            });
            mapper.Register<BulkImportOrganizationRowErrorDto, BulkImportOrganizationRowError>(d => new BulkImportOrganizationRowError
            {
                RowNumber = d.RowNumber,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<BulkImportOrganizationResultDto, BulkImportOrganizationsCommandResponse>(d => new BulkImportOrganizationsCommandResponse
            {
                TotalRows = d.TotalRows,
                SuccessCount = d.SuccessCount,
                FailureCount = d.FailureCount,
                Errors = mapper.MapList<BulkImportOrganizationRowError>(d.Errors)
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
                IsGlobal = r.IsGlobal,
                HasAccessToSystem = r.HasAccessToSystem,
                LoginEmail = r.LoginEmail,
                Password = r.Password
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
                HasAccessToSystem = d.HasAccessToSystem ?? false,
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
                IsGlobal = r.IsGlobal,
                HasAccessToSystem = r.HasAccessToSystem,
                LoginEmail = r.LoginEmail,
                Password = r.Password
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
                HasAccessToSystem = d.HasAccessToSystem ?? false,
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
                HasAccessToSystem = d.HasAccessToSystem ?? false,
                IsActive = d.IsActive
            });
            mapper.Register<BulkImportSupplierRowErrorDto, BulkImportSupplierRowError>(d => new BulkImportSupplierRowError
            {
                RowNumber = d.RowNumber,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<BulkImportSupplierResultDto, BulkImportSuppliersCommandResponse>(d => new BulkImportSuppliersCommandResponse
            {
                TotalRows = d.TotalRows,
                SuccessCount = d.SuccessCount,
                FailureCount = d.FailureCount,
                Errors = mapper.MapList<BulkImportSupplierRowError>(d.Errors)
            });

            // Catalog — DTO → Common response shape
            mapper.Register<UnitTypeDto, CommonUnitType>(d => new CommonUnitType
            {
                UnitTypeToken = d.UnitTypeToken,
                Code = d.Code,
                IsSystem = d.IsSystem,
                IsActive = d.IsActive
            });
            mapper.Register<UnitOfMeasureDto, CommonUnitOfMeasure>(d => new CommonUnitOfMeasure
            {
                UnitOfMeasureToken = d.UnitOfMeasureToken,
                UnitTypeId = d.UnitTypeId,
                Code = d.Code,
                Symbol = d.Symbol,
                Decimals = d.Decimals,
                IsSystem = d.IsSystem,
                IsActive = d.IsActive
            });
            mapper.Register<UnitConversionRateDto, CommonUnitConversionRate>(d => new CommonUnitConversionRate
            {
                UnitConversionRateToken = d.UnitConversionRateToken,
                FromUnitOfMeasureId = d.FromUnitOfMeasureId,
                FromUOMCode = d.FromUOMCode,
                FromUOMSymbol = d.FromUOMSymbol,
                ToUnitOfMeasureId = d.ToUnitOfMeasureId,
                ToUOMCode = d.ToUOMCode,
                ToUOMSymbol = d.ToUOMSymbol,
                Factor = d.Factor,
                IsActive = d.IsActive
            });
            mapper.Register<FamilyDto, CommonFamily>(d => new CommonFamily
            {
                FamilyToken = d.FamilyToken,
                Code = d.Code,
                IsSystem = d.IsSystem,
                IsActive = d.IsActive
            });
            mapper.Register<SubFamilyDto, CommonSubFamily>(d => new CommonSubFamily
            {
                SubFamilyToken = d.SubFamilyToken,
                FamilyId = d.FamilyId,
                Code = d.Code,
                IsSystem = d.IsSystem,
                IsActive = d.IsActive
            });
            mapper.Register<BulkImportFamilyRowErrorDto, BulkImportFamilyRowError>(d => new BulkImportFamilyRowError
            {
                RowNumber = d.RowNumber,
                FamilyCode = d.FamilyCode,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<BulkImportFamilyResultDto, BulkImportFamiliesCommandResponse>(d => new BulkImportFamiliesCommandResponse
            {
                TotalRows = d.TotalRows,
                SuccessCount = d.SuccessCount,
                FailureCount = d.FailureCount,
                Errors = mapper.MapList<BulkImportFamilyRowError>(d.Errors)
            });
            mapper.Register<BulkImportSubFamilyRowErrorDto, BulkImportSubFamilyRowError>(d => new BulkImportSubFamilyRowError
            {
                RowNumber = d.RowNumber,
                SubFamilyCode = d.SubFamilyCode,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<BulkImportSubFamilyResultDto, BulkImportSubFamiliesCommandResponse>(d => new BulkImportSubFamiliesCommandResponse
            {
                TotalRows = d.TotalRows,
                SuccessCount = d.SuccessCount,
                FailureCount = d.FailureCount,
                Errors = mapper.MapList<BulkImportSubFamilyRowError>(d.Errors)
            });
            mapper.Register<CategoryDto, CommonCategory>(d => new CommonCategory
            {
                CategoryToken = d.CategoryToken,
                Code = d.Code,
                IsSystem = d.IsSystem,
                IsActive = d.IsActive
            });
            mapper.Register<SubCategoryDto, CommonSubCategory>(d => new CommonSubCategory
            {
                SubCategoryToken = d.SubCategoryToken,
                CategoryId = d.CategoryId,
                Code = d.Code,
                IsSystem = d.IsSystem,
                IsActive = d.IsActive
            });
            mapper.Register<BulkImportCategoryRowErrorDto, BulkImportCategoryRowError>(d => new BulkImportCategoryRowError
            {
                RowNumber = d.RowNumber,
                CategoryCode = d.CategoryCode,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<BulkImportCategoryResultDto, BulkImportCategoriesCommandResponse>(d => new BulkImportCategoriesCommandResponse
            {
                TotalRows = d.TotalRows,
                SuccessCount = d.SuccessCount,
                FailureCount = d.FailureCount,
                Errors = mapper.MapList<BulkImportCategoryRowError>(d.Errors)
            });
            mapper.Register<BulkImportSubCategoryRowErrorDto, BulkImportSubCategoryRowError>(d => new BulkImportSubCategoryRowError
            {
                RowNumber = d.RowNumber,
                SubCategoryCode = d.SubCategoryCode,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<BulkImportSubCategoryResultDto, BulkImportSubCategoriesCommandResponse>(d => new BulkImportSubCategoriesCommandResponse
            {
                TotalRows = d.TotalRows,
                SuccessCount = d.SuccessCount,
                FailureCount = d.FailureCount,
                Errors = mapper.MapList<BulkImportSubCategoryRowError>(d.Errors)
            });

            // Article
            mapper.Register<ArticleDto, CommonArticle>(d => new CommonArticle
            {
                ArticleToken = d.ArticleToken,
                SupplierId = d.SupplierId,
                SupplierName = d.SupplierName,
                Name = d.Name,
                Description = d.Description,
                SupplierSku = d.SupplierSku,
                Barcode = d.Barcode,
                Brand = d.Brand,
                FamilyId = d.FamilyId,
                FamilyCode = d.FamilyCode,
                SubFamilyId = d.SubFamilyId,
                SubFamilyCode = d.SubFamilyCode,
                PurchaseUnitCode = d.PurchaseUnitCode,
                PurchaseUnitSymbol = d.PurchaseUnitSymbol,
                PurchaseQuantity = d.PurchaseQuantity,
                ContentUnitCode = d.ContentUnitCode,
                ContentUnitSymbol = d.ContentUnitSymbol,
                ContentQuantity = d.ContentQuantity,
                BaseUnitCode = d.BaseUnitCode,
                BaseUnitSymbol = d.BaseUnitSymbol,
                MinimumOrderQty = d.MinimumOrderQty,
                LeadTimeDays = d.LeadTimeDays,
                IsActive = d.IsActive,
                ReplacedByArticleToken = d.ReplacedByArticleToken
            });
            mapper.Register<BulkImportArticleRowErrorDto, BulkImportArticleRowError>(d => new BulkImportArticleRowError
            {
                RowNumber = d.RowNumber,
                Identifier = d.Identifier,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<BulkImportArticleResultDto, BulkImportArticlesCommandResponse>(d => new BulkImportArticlesCommandResponse
            {
                TotalRows = d.TotalRows,
                InsertedCount = d.InsertedCount,
                UpdatedCount = d.UpdatedCount,
                DeletedCount = d.DeletedCount,
                FailureCount = d.FailureCount,
                Errors = mapper.MapList<BulkImportArticleRowError>(d.Errors)
            });

            // ArticlePrice
            mapper.Register<ArticlePriceDto, CommonArticlePrice>(d => new CommonArticlePrice
            {
                ArticlePriceToken = d.ArticlePriceToken,
                ArticleToken = d.ArticleToken,
                OrganizationToken = d.OrganizationToken,
                Price = d.Price,
                CurrencyCode = d.CurrencyCode,
                EffectiveDate = d.EffectiveDate,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });
            mapper.Register<BulkImportArticlePriceRowErrorDto, BulkImportArticlePriceRowError>(d => new BulkImportArticlePriceRowError
            {
                RowNumber = d.RowNumber,
                SupplierSku = d.SupplierSku,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<BulkImportArticlePriceResultDto, BulkImportArticlePricesCommandResponse>(d => new BulkImportArticlePricesCommandResponse
            {
                TotalRows = d.TotalRows,
                SuccessCount = d.SuccessCount,
                FailureCount = d.FailureCount,
                Errors = mapper.MapList<BulkImportArticlePriceRowError>(d.Errors)
            });

            // Currency
            mapper.Register<CurrencyDto, CommonCurrency>(d => new CommonCurrency
            {
                Code = d.Code,
                IsActive = d.IsActive
            });

            // MenuItem
            mapper.Register<MenuItemDto, CommonMenuItem>(d => new CommonMenuItem
            {
                MenuItemToken = d.MenuItemToken,
                ParentMenuItemToken = d.ParentMenuItemToken,
                Name = d.Name,
                Route = d.Route,
                Icon = d.Icon,
                SortOrder = d.SortOrder
            });

            // OrganizationContact
            mapper.Register<CreateOrganizationContactCommandRequest, OrganizationContactDto>(r => new OrganizationContactDto
            {
                OrganizationToken = r.OrganizationToken,
                ContactName = r.ContactName,
                ContactType = r.ContactType,
                Department = r.Department,
                Phone = r.Phone,
                Mobile = r.Mobile,
                Fax = r.Fax,
                Email = r.Email,
                Notes = r.Notes,
                IsPrimary = r.IsPrimary
            });
            mapper.Register<EditOrganizationContactCommandRequest, OrganizationContactDto>(r => new OrganizationContactDto
            {
                OrganizationContactToken = r.OrganizationContactToken,
                ContactName = r.ContactName,
                ContactType = r.ContactType,
                Department = r.Department,
                Phone = r.Phone,
                Mobile = r.Mobile,
                Fax = r.Fax,
                Email = r.Email,
                Notes = r.Notes,
                IsPrimary = r.IsPrimary
            });
            mapper.Register<OrganizationContactDto, CreateOrganizationContactCommandResponse>(d => new CreateOrganizationContactCommandResponse
            {
                OrganizationContactId = d.OrganizationContactId,
                OrganizationContactToken = d.OrganizationContactToken,
                OrganizationId = d.OrganizationId,
                ContactName = d.ContactName,
                ContactType = d.ContactType,
                Department = d.Department,
                Phone = d.Phone,
                Mobile = d.Mobile,
                Fax = d.Fax,
                Email = d.Email,
                Notes = d.Notes,
                IsPrimary = d.IsPrimary,
                IsActive = d.IsActive
            });
            mapper.Register<OrganizationContactDto, EditOrganizationContactCommandResponse>(d => new EditOrganizationContactCommandResponse
            {
                OrganizationContactId = d.OrganizationContactId,
                OrganizationContactToken = d.OrganizationContactToken,
                OrganizationId = d.OrganizationId,
                ContactName = d.ContactName,
                ContactType = d.ContactType,
                Department = d.Department,
                Phone = d.Phone,
                Mobile = d.Mobile,
                Fax = d.Fax,
                Email = d.Email,
                Notes = d.Notes,
                IsPrimary = d.IsPrimary,
                IsActive = d.IsActive
            });
            mapper.Register<OrganizationContactDto, CommonOrganizationContact>(d => new CommonOrganizationContact
            {
                OrganizationContactId = d.OrganizationContactId,
                OrganizationContactToken = d.OrganizationContactToken,
                OrganizationId = d.OrganizationId,
                ContactName = d.ContactName,
                ContactType = d.ContactType,
                Department = d.Department,
                Phone = d.Phone,
                Mobile = d.Mobile,
                Fax = d.Fax,
                Email = d.Email,
                Notes = d.Notes,
                IsPrimary = d.IsPrimary,
                IsActive = d.IsActive
            });
        }
    }
}
