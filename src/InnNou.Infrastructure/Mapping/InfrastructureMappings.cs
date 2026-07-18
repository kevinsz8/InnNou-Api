using InnNou.Domain.Dtos;
using InnNou.Infrastructure.Models;
using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Mapping;
using UserEntity = InnNou.Infrastructure.Repositories.DbEntities.User;
using OrganizationEntity = InnNou.Infrastructure.Repositories.DbEntities.Organization;
using RoleEntity = InnNou.Infrastructure.Repositories.DbEntities.Role;
using SupplierEntity = InnNou.Infrastructure.Repositories.DbEntities.Supplier;
using UnitTypeEntity = InnNou.Infrastructure.Repositories.DbEntities.UnitType;
using UnitOfMeasureEntity = InnNou.Infrastructure.Repositories.DbEntities.UnitOfMeasure;
using UnitConversionRateEntity = InnNou.Infrastructure.Repositories.DbEntities.UnitConversionRate;
using FamilyEntity = InnNou.Infrastructure.Repositories.DbEntities.Family;
using SubFamilyEntity = InnNou.Infrastructure.Repositories.DbEntities.SubFamily;
using CategoryEntity = InnNou.Infrastructure.Repositories.DbEntities.Category;
using SubCategoryEntity = InnNou.Infrastructure.Repositories.DbEntities.SubCategory;
using OrganizationContactEntity = InnNou.Infrastructure.Repositories.DbEntities.OrganizationContact;
using ArticleEntity = InnNou.Infrastructure.Repositories.DbEntities.Article;
using ArticlePriceEntity = InnNou.Infrastructure.Repositories.DbEntities.ArticlePrice;
using ArticleFavoriteEntity = InnNou.Infrastructure.Repositories.DbEntities.ArticleFavorite;
using CurrencyEntity = InnNou.Infrastructure.Repositories.DbEntities.Currency;
using WarehouseEntity = InnNou.Infrastructure.Repositories.DbEntities.Warehouse;
using WarehouseContactEntity = InnNou.Infrastructure.Repositories.DbEntities.WarehouseContact;
using OrderEntity = InnNou.Infrastructure.Repositories.DbEntities.Order;
using OrderLineEntity = InnNou.Infrastructure.Repositories.DbEntities.OrderLine;
using PurchaseOrderEntity = InnNou.Infrastructure.Repositories.DbEntities.PurchaseOrder;
using PurchaseOrderLineEntity = InnNou.Infrastructure.Repositories.DbEntities.PurchaseOrderLine;
using OrderTemplateEntity = InnNou.Infrastructure.Repositories.DbEntities.OrderTemplate;
using OrderTemplateLineEntity = InnNou.Infrastructure.Repositories.DbEntities.OrderTemplateLine;

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
                OrganizationId = u.OrganizationId,
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
                OrganizationId = u.OrganizationId,
                SupplierId = u.SupplierId,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                UserName = u.UserName,
                IsActive = u.IsActive,
                IsDeleted = u.IsDeleted
            });

            mapper.Register<OrganizationEntity, OrganizationDto>(o => new OrganizationDto
            {
                OrganizationId = o.OrganizationId,
                OrganizationToken = o.OrganizationToken,
                Name = o.Name,
                NormalizedName = o.NormalizedName,
                LegalName = o.LegalName,
                Code = o.Code,
                ParentOrganizationId = o.ParentOrganizationId,
                OrganizationTypeId = o.OrganizationTypeId,
                OrganizationTypeCode = o.OrganizationTypeCode,
                TimeZone = o.TimeZone,
                CurrencyCode = o.CurrencyCode,
                LanguageCode = o.LanguageCode,
                IsActive = o.IsActive,
                IsDeleted = o.IsDeleted
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
                SupplierType = s.SupplierType,
                HasAccessToSystem = s.HasAccessToSystem,
                IsActive = s.IsActive,
                IsDeleted = s.IsDeleted,
                OrganizationTokenResult = s.OrganizationTokenResult,
                OrganizationName = s.OrganizationName
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
                UnitTypeCode = e.UnitTypeCode,
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
                FromUOMCode = e.FromUOMCode,
                FromUOMSymbol = e.FromUOMSymbol,
                ToUnitOfMeasureId = e.ToUnitOfMeasureId,
                ToUOMCode = e.ToUOMCode,
                ToUOMSymbol = e.ToUOMSymbol,
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

            mapper.Register<ArticleEntity, ArticleDto>(e => new ArticleDto
            {
                ArticleId = e.ArticleId,
                ArticleToken = e.ArticleToken,
                SupplierId = e.SupplierId,
                SupplierName = e.SupplierName,
                SupplierType = e.SupplierType,
                Name = e.Name,
                NormalizedName = e.NormalizedName,
                Description = e.Description,
                SupplierSku = e.SupplierSku,
                Barcode = e.Barcode,
                Brand = e.Brand,
                FamilyId = e.FamilyId,
                FamilyCode = e.FamilyCode,
                SubFamilyId = e.SubFamilyId,
                SubFamilyCode = e.SubFamilyCode,
                PurchaseUnitId = e.PurchaseUnitId,
                PurchaseUnitCode = e.PurchaseUnitCode,
                PurchaseUnitSymbol = e.PurchaseUnitSymbol,
                PurchaseQuantity = e.PurchaseQuantity,
                ContentUnitId = e.ContentUnitId,
                ContentUnitCode = e.ContentUnitCode,
                ContentUnitSymbol = e.ContentUnitSymbol,
                ContentQuantity = e.ContentQuantity,
                BaseUnitId = e.BaseUnitId,
                BaseUnitCode = e.BaseUnitCode,
                BaseUnitSymbol = e.BaseUnitSymbol,
                MinimumOrderQty = e.MinimumOrderQty,
                LeadTimeDays = e.LeadTimeDays,
                IsActive = e.IsActive,
                IsDeleted = e.IsDeleted,
                ReplacedByArticleId = e.ReplacedByArticleId,
                ReplacedByArticleToken = e.ReplacedByArticleToken,
                IsFavorite = e.IsFavorite,
                IsInherited = e.IsInherited,
                FavoriteOrganizationName = e.FavoriteOrganizationName
            });

            mapper.Register<ArticlePriceEntity, ArticlePriceDto>(e => new ArticlePriceDto
            {
                ArticlePriceId = e.ArticlePriceId,
                ArticlePriceToken = e.ArticlePriceToken,
                ArticleId = e.ArticleId,
                ArticleToken = e.ArticleToken,
                OrganizationId = e.OrganizationId,
                OrganizationToken = e.OrganizationToken,
                Price = e.Price,
                CurrencyCode = e.CurrencyCode,
                EffectiveDate = e.EffectiveDate,
                Notes = e.Notes,
                CreatedUtc = e.CreatedUtc,
                CreatedBy = e.CreatedBy
            });

            mapper.Register<ArticleFavoriteEntity, ArticleFavoriteDto>(e => new ArticleFavoriteDto
            {
                ArticleFavoriteId = e.ArticleFavoriteId,
                ArticleFavoriteToken = e.ArticleFavoriteToken,
                ArticleId = e.ArticleId,
                ArticleToken = e.ArticleToken,
                ArticleName = e.ArticleName,
                SupplierSku = e.SupplierSku,
                SupplierName = e.SupplierName,
                OrganizationId = e.OrganizationId,
                OrganizationToken = e.OrganizationToken,
                OrganizationName = e.OrganizationName,
                IsInherited = e.IsInherited,
                CreatedUtc = e.CreatedUtc,
                CreatedBy = e.CreatedBy
            });

            mapper.Register<CurrencyEntity, CurrencyDto>(e => new CurrencyDto
            {
                CurrencyId = e.CurrencyId,
                Code = e.Code,
                IsActive = e.IsActive
            });

            mapper.Register<OrganizationContactEntity, OrganizationContactDto>(e => new OrganizationContactDto
            {
                OrganizationContactId = e.OrganizationContactId,
                OrganizationContactToken = e.OrganizationContactToken,
                OrganizationId = e.OrganizationId,
                ContactName = e.ContactName,
                ContactType = e.ContactType,
                Department = e.Department,
                Phone = e.Phone,
                Mobile = e.Mobile,
                Fax = e.Fax,
                Email = e.Email,
                Notes = e.Notes,
                IsPrimary = e.IsPrimary,
                IsActive = e.IsActive,
                IsDeleted = e.IsDeleted
            });

            mapper.Register<WarehouseEntity, WarehouseDto>(e => new WarehouseDto
            {
                WarehouseId = e.WarehouseId,
                WarehouseToken = e.WarehouseToken,
                OrganizationId = e.OrganizationId,
                Name = e.Name,
                Code = e.Code,
                Description = e.Description,
                PurposeCode = e.PurposeCode,
                IsInventoriable = e.IsInventoriable,
                CanReceivePurchases = e.CanReceivePurchases,
                CanReceiveTransfers = e.CanReceiveTransfers,
                CanTransferOut = e.CanTransferOut,
                CanConsumeInventory = e.CanConsumeInventory,
                CanProduceItems = e.CanProduceItems,
                CanSellItems = e.CanSellItems,
                CanAdjustInventory = e.CanAdjustInventory,
                CanReceiveReturns = e.CanReceiveReturns,
                TrackLotNumbers = e.TrackLotNumbers,
                TrackExpirationDates = e.TrackExpirationDates,
                TrackSerialNumbers = e.TrackSerialNumbers,
                RequireApproval = e.RequireApproval,
                IsDefaultReceivingWarehouse = e.IsDefaultReceivingWarehouse,
                IsDefaultConsumptionWarehouse = e.IsDefaultConsumptionWarehouse,
                IsActive = e.IsActive,
                IsDeleted = e.IsDeleted
            });

            mapper.Register<WarehouseContactEntity, WarehouseContactDto>(e => new WarehouseContactDto
            {
                WarehouseContactId = e.WarehouseContactId,
                WarehouseContactToken = e.WarehouseContactToken,
                WarehouseId = e.WarehouseId,
                ContactName = e.ContactName,
                ContactType = e.ContactType,
                Department = e.Department,
                Phone = e.Phone,
                Mobile = e.Mobile,
                Fax = e.Fax,
                Email = e.Email,
                Notes = e.Notes,
                IsPrimary = e.IsPrimary,
                HasAccessToSystem = e.HasAccessToSystem,
                IsActive = e.IsActive,
                IsDeleted = e.IsDeleted
            });

            mapper.Register<OrderLineEntity, OrderLineDto>(e => new OrderLineDto
            {
                OrderLineId = e.OrderLineId,
                OrderLineToken = e.OrderLineToken,
                OrderId = e.OrderId,
                OrderToken = e.OrderToken,
                ArticleId = e.ArticleId,
                ArticleToken = e.ArticleToken,
                ArticleName = e.ArticleName,
                SupplierId = e.SupplierId,
                SupplierName = e.SupplierName,
                Quantity = e.Quantity,
                PurchaseUnitId = e.PurchaseUnitId,
                PurchaseUnitCode = e.PurchaseUnitCode,
                PurchaseQuantity = e.PurchaseQuantity,
                ContentUnitId = e.ContentUnitId,
                ContentUnitCode = e.ContentUnitCode,
                ContentQuantity = e.ContentQuantity,
                UnitPrice = e.UnitPrice,
                CurrencyCode = e.CurrencyCode,
                Notes = e.Notes,
                CreatedUtc = e.CreatedUtc,
                CreatedBy = e.CreatedBy,
                LastUpdatedUtc = e.LastUpdatedUtc,
                LastUpdatedBy = e.LastUpdatedBy
            });

            mapper.Register<PurchaseOrderLineEntity, PurchaseOrderLineDto>(e => new PurchaseOrderLineDto
            {
                PurchaseOrderLineId = e.PurchaseOrderLineId,
                PurchaseOrderLineToken = e.PurchaseOrderLineToken,
                PurchaseOrderId = e.PurchaseOrderId,
                PurchaseOrderToken = e.PurchaseOrderToken,
                OrderLineId = e.OrderLineId,
                OrderLineToken = e.OrderLineToken,
                ArticleId = e.ArticleId,
                ArticleToken = e.ArticleToken,
                ArticleName = e.ArticleName,
                SupplierId = e.SupplierId,
                SupplierName = e.SupplierName,
                Quantity = e.Quantity,
                PurchaseUnitId = e.PurchaseUnitId,
                PurchaseUnitCode = e.PurchaseUnitCode,
                PurchaseQuantity = e.PurchaseQuantity,
                ContentUnitId = e.ContentUnitId,
                ContentUnitCode = e.ContentUnitCode,
                ContentQuantity = e.ContentQuantity,
                UnitPrice = e.UnitPrice,
                CurrencyCode = e.CurrencyCode,
                Notes = e.Notes,
                CreatedUtc = e.CreatedUtc,
                CreatedBy = e.CreatedBy,
                LastUpdatedUtc = e.LastUpdatedUtc,
                LastUpdatedBy = e.LastUpdatedBy
            });

            mapper.Register<OrderEntity, OrderDto>(e => new OrderDto
            {
                OrderId = e.OrderId,
                OrderToken = e.OrderToken,
                OrganizationId = e.OrganizationId,
                OrganizationToken = e.OrganizationToken,
                WarehouseId = e.WarehouseId,
                WarehouseToken = e.WarehouseToken,
                WarehouseName = e.WarehouseName,
                Status = e.Status,
                Notes = e.Notes,
                SubmittedUtc = e.SubmittedUtc,
                CreatedUtc = e.CreatedUtc,
                CreatedBy = e.CreatedBy,
                LastUpdatedUtc = e.LastUpdatedUtc,
                LastUpdatedBy = e.LastUpdatedBy,
                LineCount = e.LineCount
            });

            mapper.Register<PurchaseOrderEntity, PurchaseOrderDto>(e => new PurchaseOrderDto
            {
                PurchaseOrderId = e.PurchaseOrderId,
                PurchaseOrderToken = e.PurchaseOrderToken,
                OrderId = e.OrderId,
                OrderToken = e.OrderToken,
                SupplierId = e.SupplierId,
                SupplierName = e.SupplierName,
                OrganizationId = e.OrganizationId,
                OrganizationToken = e.OrganizationToken,
                WarehouseId = e.WarehouseId,
                WarehouseToken = e.WarehouseToken,
                WarehouseName = e.WarehouseName,
                Status = e.Status,
                SentUtc = e.SentUtc,
                CancelledUtc = e.CancelledUtc,
                CancelledBy = e.CancelledBy,
                CreatedUtc = e.CreatedUtc,
                CreatedBy = e.CreatedBy,
                LineCount = e.LineCount
            });

            mapper.Register<OrderTemplateLineEntity, OrderTemplateLineDto>(e => new OrderTemplateLineDto
            {
                OrderTemplateLineId = e.OrderTemplateLineId,
                OrderTemplateLineToken = e.OrderTemplateLineToken,
                OrderTemplateId = e.OrderTemplateId,
                OrderTemplateToken = e.OrderTemplateToken,
                ArticleId = e.ArticleId,
                ArticleToken = e.ArticleToken,
                ArticleName = e.ArticleName,
                SupplierId = e.SupplierId,
                SupplierName = e.SupplierName,
                SupplierSku = e.SupplierSku,
                SupplierType = e.SupplierType,
                PurchaseUnitId = e.PurchaseUnitId,
                PurchaseUnitCode = e.PurchaseUnitCode,
                PurchaseUnitSymbol = e.PurchaseUnitSymbol,
                IsArticleActive = e.IsArticleActive,
                IsArticleDeleted = e.IsArticleDeleted,
                ReplacedByArticleToken = e.ReplacedByArticleToken,
                Quantity = e.Quantity,
                CreatedUtc = e.CreatedUtc,
                CreatedBy = e.CreatedBy,
                LastUpdatedUtc = e.LastUpdatedUtc,
                LastUpdatedBy = e.LastUpdatedBy
            });

            mapper.Register<OrderTemplateEntity, OrderTemplateDto>(e => new OrderTemplateDto
            {
                OrderTemplateId = e.OrderTemplateId,
                OrderTemplateToken = e.OrderTemplateToken,
                Name = e.Name,
                OrganizationId = e.OrganizationId,
                OrganizationToken = e.OrganizationToken,
                WarehouseId = e.WarehouseId,
                WarehouseToken = e.WarehouseToken,
                WarehouseName = e.WarehouseName,
                IsWarehouseActive = e.IsWarehouseActive,
                OwnerUserId = e.OwnerUserId,
                OwnerUserToken = e.OwnerUserToken,
                OwnerFirstName = e.OwnerFirstName,
                OwnerLastName = e.OwnerLastName,
                OwnerEmail = e.OwnerEmail,
                CreatedUtc = e.CreatedUtc,
                CreatedBy = e.CreatedBy,
                LastUpdatedUtc = e.LastUpdatedUtc,
                LastUpdatedBy = e.LastUpdatedBy,
                LineCount = e.LineCount
            });
        }
    }
}
