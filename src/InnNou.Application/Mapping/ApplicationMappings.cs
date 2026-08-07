using InnNou.Application.Common;
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
using CommonArticlePackagingLevel = InnNou.Application.Responses.Common.ArticlePackagingLevel;
using CommonArticlePackagingConversion = InnNou.Application.Responses.Common.ArticlePackagingConversion;
using CommonArticlePackagingConversionLevel = InnNou.Application.Responses.Common.ArticlePackagingConversionLevel;
using CommonArticlePriceComparison = InnNou.Application.Responses.Common.ArticlePriceComparison;
using CommonArticlePrice = InnNou.Application.Responses.Common.ArticlePrice;
using CommonArticleFavorite = InnNou.Application.Responses.Common.ArticleFavorite;
using CommonArticleClassification = InnNou.Application.Responses.Common.ArticleClassification;
using CommonCurrency = InnNou.Application.Responses.Common.Currency;
using CommonMenuItem = InnNou.Application.Responses.Common.MenuItem;
using CommonWarehouse = InnNou.Application.Responses.Common.Warehouse;
using CommonWarehouseContact = InnNou.Application.Responses.Common.WarehouseContact;
using CommonOrder = InnNou.Application.Responses.Common.Order;
using CommonOrderLine = InnNou.Application.Responses.Common.OrderLine;
using CommonPurchaseOrder = InnNou.Application.Responses.Common.PurchaseOrder;
using CommonPurchaseOrderLine = InnNou.Application.Responses.Common.PurchaseOrderLine;
using CommonPurchaseOrderRectification = InnNou.Application.Responses.Common.PurchaseOrderRectification;
using CommonPurchaseOrderLineRectification = InnNou.Application.Responses.Common.PurchaseOrderLineRectification;
using CommonGoodsReceipt = InnNou.Application.Responses.Common.GoodsReceipt;
using CommonGoodsReceiptLine = InnNou.Application.Responses.Common.GoodsReceiptLine;
using CommonStockLevel = InnNou.Application.Responses.Common.StockLevel;
using CommonInventoryMovement = InnNou.Application.Responses.Common.InventoryMovement;
using CommonInventoryTransfer = InnNou.Application.Responses.Common.InventoryTransfer;
using CommonInventoryTransferLine = InnNou.Application.Responses.Common.InventoryTransferLine;
using CommonInternalOrder = InnNou.Application.Responses.Common.InternalOrder;
using CommonInternalOrderLine = InnNou.Application.Responses.Common.InternalOrderLine;
using CommonInternalOrderShipment = InnNou.Application.Responses.Common.InternalOrderShipment;
using CommonInternalOrderShipmentLine = InnNou.Application.Responses.Common.InternalOrderShipmentLine;
using CommonInternalOrderReceipt = InnNou.Application.Responses.Common.InternalOrderReceipt;
using CommonInternalOrderReceiptLine = InnNou.Application.Responses.Common.InternalOrderReceiptLine;
using CommonNotification = InnNou.Application.Responses.Common.Notification;
using CommonSupplierPriceChangeSubscription = InnNou.Application.Responses.Common.SupplierPriceChangeSubscription;
using CommonInventoryPeriod = InnNou.Application.Responses.Common.InventoryPeriod;
using CommonInventoryPeriodCount = InnNou.Application.Responses.Common.InventoryPeriodCount;
using CommonDashboardSummary = InnNou.Application.Responses.Common.DashboardSummary;
using CommonMonthlySpend = InnNou.Application.Responses.Common.MonthlySpend;
using CommonSupplierSpend = InnNou.Application.Responses.Common.SupplierSpend;
using CommonSupplierScorecard = InnNou.Application.Responses.Common.SupplierScorecard;
using CommonSupplierReturn = InnNou.Application.Responses.Common.SupplierReturn;
using CommonSupplierReturnLine = InnNou.Application.Responses.Common.SupplierReturnLine;
using CommonEligibleReturnLine = InnNou.Application.Responses.Common.EligibleReturnLine;
using CommonRecentActivityItem = InnNou.Application.Responses.Common.RecentActivityItem;
using CommonDepartmentParLevel = InnNou.Application.Responses.Common.DepartmentParLevel;
using CommonSuggestedRequisition = InnNou.Application.Responses.Common.SuggestedRequisition;
using CommonParLevel = InnNou.Application.Responses.Common.ParLevel;
using CommonParLevelOverride = InnNou.Application.Responses.Common.ParLevelOverride;
using CommonParLevelEffective = InnNou.Application.Responses.Common.ParLevelEffective;
using CommonBelowParRow = InnNou.Application.Responses.Common.BelowParRow;
using CommonConsolidatedPurchaseOrder = InnNou.Application.Responses.Common.ConsolidatedPurchaseOrder;
using CommonConsolidatedPurchaseOrderMember = InnNou.Application.Responses.Common.ConsolidatedPurchaseOrderMember;
using CommonOrderApprovalStep = InnNou.Application.Responses.Common.OrderApprovalStep;
using CommonFamilyApprovalThreshold = InnNou.Application.Responses.Common.FamilyApprovalThreshold;
using CommonArticleDiscount = InnNou.Application.Responses.Common.ArticleDiscount;
using CommonOrderTemplate = InnNou.Application.Responses.Common.OrderTemplate;
using CommonOrderTemplateLine = InnNou.Application.Responses.Common.OrderTemplateLine;
using CommonApplyOrderTemplateLineResult = InnNou.Application.Responses.Common.ApplyOrderTemplateLineResult;
using CommonCountry = InnNou.Application.Responses.Common.Country;
using CommonZone = InnNou.Application.Responses.Common.Zone;
using CommonTaxCategory = InnNou.Application.Responses.Common.TaxCategory;
using CommonTaxJurisdiction = InnNou.Application.Responses.Common.TaxJurisdiction;
using CommonTaxRateGridRow = InnNou.Application.Responses.Common.TaxRateGridRow;
using CommonFamilyTaxCategoryOverride = InnNou.Application.Responses.Common.FamilyTaxCategoryOverride;
using CommonDepartment = InnNou.Application.Responses.Common.Department;
using CommonRequisition = InnNou.Application.Responses.Common.Requisition;
using CommonRequisitionLine = InnNou.Application.Responses.Common.RequisitionLine;
using CommonRequisitionIssue = InnNou.Application.Responses.Common.RequisitionIssue;
using CommonRequisitionIssueLine = InnNou.Application.Responses.Common.RequisitionIssueLine;
using CommonSupplierInvoice = InnNou.Application.Responses.Common.SupplierInvoice;
using CommonSupplierInvoiceLine = InnNou.Application.Responses.Common.SupplierInvoiceLine;
using CommonSupplierInvoicePurchaseOrder = InnNou.Application.Responses.Common.SupplierInvoicePurchaseOrder;
using CommonSupplierInvoiceTaxBreakdown = InnNou.Application.Responses.Common.SupplierInvoiceTaxBreakdown;
using CommonSupplierInvoiceMatchTolerance = InnNou.Application.Responses.Common.SupplierInvoiceMatchTolerance;
using CommonSupplierInvoicePurchaseOrderPolicy = InnNou.Application.Responses.Common.SupplierInvoicePurchaseOrderPolicy;
using CommonSupplierCreditNote = InnNou.Application.Responses.Common.SupplierCreditNote;
using CommonSupplierCreditNoteLine = InnNou.Application.Responses.Common.SupplierCreditNoteLine;
using CommonSupplierCreditNoteTaxBreakdown = InnNou.Application.Responses.Common.SupplierCreditNoteTaxBreakdown;
using CommonSupplierCreditNoteInvoiceRef = InnNou.Application.Responses.Common.SupplierCreditNoteInvoiceRef;
using CommonGoodsReceiptForInvoicing = InnNou.Application.Responses.Common.GoodsReceiptForInvoicing;
using CommonGoodsReceiptSummary = InnNou.Application.Responses.Common.GoodsReceiptSummary;
using CommonGoodsReceiptTaxPreviewLine = InnNou.Application.Responses.Common.GoodsReceiptTaxPreviewLine;
using CommonSupplierDeliveryZone = InnNou.Application.Responses.Common.SupplierDeliveryZone;

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
                UserName = r.UserName ?? string.Empty,
                RoleId = r.RoleId
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
                LanguageCode = r.LanguageCode,
                AddressLine1 = r.AddressLine1,
                AddressLine2 = r.AddressLine2,
                City = r.City,
                State = r.State,
                PostalCode = r.PostalCode,
                Country = r.Country,
                Description = r.Description
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
                ZoneToken = d.ZoneToken,
                ZoneCode = d.ZoneCode,
                ZoneName = d.ZoneName,
                CountryCode = d.CountryCode,
                CountryName = d.CountryName,
                AddressLine1 = d.AddressLine1,
                AddressLine2 = d.AddressLine2,
                City = d.City,
                State = d.State,
                PostalCode = d.PostalCode,
                Country = d.Country,
                Description = d.Description,
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
                LanguageCode = r.LanguageCode,
                AddressLine1 = r.AddressLine1,
                AddressLine2 = r.AddressLine2,
                City = r.City,
                State = r.State,
                PostalCode = r.PostalCode,
                Country = r.Country,
                Description = r.Description
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
                ZoneToken = d.ZoneToken,
                ZoneCode = d.ZoneCode,
                ZoneName = d.ZoneName,
                CountryCode = d.CountryCode,
                CountryName = d.CountryName,
                AddressLine1 = d.AddressLine1,
                AddressLine2 = d.AddressLine2,
                City = d.City,
                State = d.State,
                PostalCode = d.PostalCode,
                Country = d.Country,
                Description = d.Description,
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
                ZoneToken = d.ZoneToken,
                ZoneCode = d.ZoneCode,
                ZoneName = d.ZoneName,
                CountryCode = d.CountryCode,
                CountryName = d.CountryName,
                AddressLine1 = d.AddressLine1,
                AddressLine2 = d.AddressLine2,
                City = d.City,
                State = d.State,
                PostalCode = d.PostalCode,
                Country = d.Country,
                Description = d.Description,
                IsActive = d.IsActive
            });

            // Country / Zone / SupplierDeliveryZone
            mapper.Register<CountryDto, CommonCountry>(d => new CommonCountry
            {
                Code = d.Code,
                Name = d.Name,
                IsActive = d.IsActive
            });
            mapper.Register<ZoneDto, CommonZone>(d => new CommonZone
            {
                ZoneToken = d.ZoneToken,
                CountryCode = d.CountryCode,
                CountryName = d.CountryName,
                Code = d.Code,
                Name = d.Name,
                IsActive = d.IsActive
            });
            mapper.Register<TaxCategoryDto, CommonTaxCategory>(d => new CommonTaxCategory
            {
                TaxCategoryToken = d.TaxCategoryToken,
                Code = d.Code,
                IsActive = d.IsActive
            });
            mapper.Register<TaxJurisdictionDto, CommonTaxJurisdiction>(d => new CommonTaxJurisdiction
            {
                TaxJurisdictionToken = d.TaxJurisdictionToken,
                Code = d.Code,
                Name = d.Name,
                IsActive = d.IsActive,
                CountryCode = d.CountryCode,
                CountryName = d.CountryName
            });
            mapper.Register<TaxRateGridRowDto, CommonTaxRateGridRow>(d => new CommonTaxRateGridRow
            {
                TaxJurisdictionToken = d.TaxJurisdictionToken,
                TaxJurisdictionCode = d.TaxJurisdictionCode,
                TaxJurisdictionName = d.TaxJurisdictionName,
                TaxCategoryToken = d.TaxCategoryToken,
                TaxCategoryCode = d.TaxCategoryCode,
                TaxRateToken = d.TaxRateToken,
                RatePercent = d.RatePercent
            });
            mapper.Register<FamilyTaxCategoryOverrideDto, CommonFamilyTaxCategoryOverride>(d => new CommonFamilyTaxCategoryOverride
            {
                FamilyTaxCategoryOverrideToken = d.FamilyTaxCategoryOverrideToken,
                IsActive = d.IsActive,
                TaxJurisdictionToken = d.TaxJurisdictionToken,
                TaxJurisdictionCode = d.TaxJurisdictionCode,
                TaxJurisdictionName = d.TaxJurisdictionName,
                TaxCategoryToken = d.TaxCategoryToken,
                TaxCategoryCode = d.TaxCategoryCode
            });

            mapper.Register<SupplierInvoiceMatchToleranceDto, CommonSupplierInvoiceMatchTolerance>(d => new CommonSupplierInvoiceMatchTolerance
            {
                SupplierInvoiceMatchToleranceToken = d.SupplierInvoiceMatchToleranceToken,
                EffectiveOrganizationToken = d.EffectiveOrganizationToken,
                EffectiveOrganizationName = d.EffectiveOrganizationName,
                TolerancePercent = d.TolerancePercent,
                ToleranceAmount = d.ToleranceAmount,
                IsInherited = d.IsInherited
            });

            mapper.Register<SupplierInvoicePurchaseOrderPolicyDto, CommonSupplierInvoicePurchaseOrderPolicy>(d => new CommonSupplierInvoicePurchaseOrderPolicy
            {
                SupplierInvoicePurchaseOrderPolicyToken = d.SupplierInvoicePurchaseOrderPolicyToken,
                EffectiveOrganizationToken = d.EffectiveOrganizationToken,
                EffectiveOrganizationName = d.EffectiveOrganizationName,
                AllowMultiplePurchaseOrders = d.AllowMultiplePurchaseOrders,
                IsInherited = d.IsInherited
            });

            mapper.Register<GoodsReceiptForInvoicingDto, CommonGoodsReceiptForInvoicing>(d => new CommonGoodsReceiptForInvoicing
            {
                GoodsReceiptToken = d.GoodsReceiptToken,
                PurchaseOrderToken = d.PurchaseOrderToken,
                PurchaseOrderNumber = d.PurchaseOrderNumber,
                PurchaseOrderSentUtc = d.PurchaseOrderSentUtc,
                PurchaseOrderStatus = d.PurchaseOrderStatus,
                DeliveryNoteNumber = d.DeliveryNoteNumber,
                GoodsReceiptCreatedUtc = d.GoodsReceiptCreatedUtc,
                WarehouseName = d.WarehouseName,
                TotalTaxableAmount = d.TotalTaxableAmount,
                TotalAmount = d.TotalAmount
            });

            mapper.Register<GoodsReceiptSummaryDto, CommonGoodsReceiptSummary>(d => new CommonGoodsReceiptSummary
            {
                GoodsReceiptToken = d.GoodsReceiptToken,
                PurchaseOrderToken = d.PurchaseOrderToken,
                PurchaseOrderNumber = d.PurchaseOrderNumber,
                PurchaseOrderSentUtc = d.PurchaseOrderSentUtc,
                PurchaseOrderStatus = d.PurchaseOrderStatus,
                SupplierName = d.SupplierName,
                WarehouseName = d.WarehouseName,
                DeliveryNoteNumber = d.DeliveryNoteNumber,
                DeliveryNoteDate = d.DeliveryNoteDate,
                GoodsReceiptCreatedUtc = d.GoodsReceiptCreatedUtc,
                CreatedBy = d.CreatedBy,
                TotalTaxableAmount = d.TotalTaxableAmount,
                TotalAmount = d.TotalAmount,
                LineCount = d.LineCount
            });

            mapper.Register<GoodsReceiptTaxPreviewLineDto, CommonGoodsReceiptTaxPreviewLine>(d => new CommonGoodsReceiptTaxPreviewLine
            {
                PurchaseOrderLineToken = d.PurchaseOrderLineToken,
                TaxCategoryCode = d.TaxCategoryCode,
                TaxRatePercent = d.TaxRatePercent
            });

            mapper.Register<SupplierInvoicePurchaseOrderDto, CommonSupplierInvoicePurchaseOrder>(d => new CommonSupplierInvoicePurchaseOrder
            {
                PurchaseOrderToken = d.PurchaseOrderToken,
                PurchaseOrderNumber = d.PurchaseOrderNumber
            });

            mapper.Register<SupplierInvoiceTaxBreakdownDto, CommonSupplierInvoiceTaxBreakdown>(d => new CommonSupplierInvoiceTaxBreakdown
            {
                SupplierInvoiceTaxBreakdownToken = d.SupplierInvoiceTaxBreakdownToken,
                TaxRatePercent = d.TaxRatePercent,
                BaseAmount = d.BaseAmount,
                TaxAmount = d.TaxAmount,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            // SupplierInvoiceLine (registered before SupplierInvoice since it embeds a List<CommonSupplierInvoiceLine>)
            mapper.Register<SupplierInvoiceLineDto, CommonSupplierInvoiceLine>(d => new CommonSupplierInvoiceLine
            {
                SupplierInvoiceLineToken = d.SupplierInvoiceLineToken,
                PurchaseOrderLineToken = d.PurchaseOrderLineToken,
                OrderedQuantity = d.OrderedQuantity,
                PurchaseOrderNumber = d.PurchaseOrderNumber,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                QuantityInvoiced = d.QuantityInvoiced,
                UnitPriceInvoiced = d.UnitPriceInvoiced,
                CurrencyCode = d.CurrencyCode,
                TaxCategoryCode = d.TaxCategoryCode,
                TaxRatePercent = d.TaxRatePercent,
                TaxableAmount = d.TaxableAmount,
                TaxAmount = d.TaxAmount,
                TotalAmount = d.TotalAmount,
                IsWithinTolerance = d.IsWithinTolerance,
                DeliveryNoteNumber = d.DeliveryNoteNumber,
                WarehouseName = d.WarehouseName,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<SupplierInvoiceDto, CommonSupplierInvoice>(d => new CommonSupplierInvoice
            {
                SupplierInvoiceToken = d.SupplierInvoiceToken,
                OrganizationToken = d.OrganizationToken,
                OrganizationName = d.OrganizationName,
                SupplierToken = d.SupplierToken,
                SupplierName = d.SupplierName,
                SupplierInvoiceNumber = d.SupplierInvoiceNumber,
                InternalSequentialNumber = d.InternalSequentialNumber,
                InvoiceDate = d.InvoiceDate,
                Status = d.Status,
                AttachmentUrl = d.AttachmentUrl,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LineCount = d.LineCount,
                TotalTaxableAmount = d.TotalTaxableAmount,
                TotalAmount = d.TotalAmount,
                PurchaseOrderNumbers = d.PurchaseOrderNumbers,
                WarehouseNames = d.WarehouseNames,
                Lines = mapper.MapList<CommonSupplierInvoiceLine>(d.Lines),
                PurchaseOrders = mapper.MapList<CommonSupplierInvoicePurchaseOrder>(d.PurchaseOrders),
                TaxBreakdown = mapper.MapList<CommonSupplierInvoiceTaxBreakdown>(d.TaxBreakdown)
            });

            mapper.Register<SupplierCreditNoteLineDto, CommonSupplierCreditNoteLine>(d => new CommonSupplierCreditNoteLine
            {
                SupplierCreditNoteLineToken = d.SupplierCreditNoteLineToken,
                SupplierReturnLineToken = d.SupplierReturnLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                QuantityCredited = d.QuantityCredited,
                UnitPrice = d.UnitPrice,
                CurrencyCode = d.CurrencyCode,
                TaxCategoryCode = d.TaxCategoryCode,
                TaxRatePercent = d.TaxRatePercent,
                TaxableAmount = d.TaxableAmount,
                TaxAmount = d.TaxAmount,
                TotalAmount = d.TotalAmount,
                WasManuallyEntered = d.WasManuallyEntered
            });

            mapper.Register<SupplierCreditNoteTaxBreakdownDto, CommonSupplierCreditNoteTaxBreakdown>(d => new CommonSupplierCreditNoteTaxBreakdown
            {
                TaxRatePercent = d.TaxRatePercent,
                TaxableAmount = d.TaxableAmount,
                TaxAmount = d.TaxAmount,
                CurrencyCode = d.CurrencyCode
            });

            mapper.Register<SupplierCreditNoteInvoiceRefDto, CommonSupplierCreditNoteInvoiceRef>(d => new CommonSupplierCreditNoteInvoiceRef
            {
                SupplierInvoiceToken = d.SupplierInvoiceToken,
                InternalSequentialNumber = d.InternalSequentialNumber,
                SupplierInvoiceNumber = d.SupplierInvoiceNumber
            });

            mapper.Register<SupplierCreditNoteDto, CommonSupplierCreditNote>(d => new CommonSupplierCreditNote
            {
                SupplierCreditNoteToken = d.SupplierCreditNoteToken,
                SupplierReturnToken = d.SupplierReturnToken,
                PurchaseOrderToken = d.PurchaseOrderToken,
                PurchaseOrderNumber = d.PurchaseOrderNumber,
                OrganizationToken = d.OrganizationToken,
                OrganizationName = d.OrganizationName,
                SupplierToken = d.SupplierToken,
                SupplierName = d.SupplierName,
                CreditNoteNumber = d.CreditNoteNumber,
                InternalSequentialNumber = d.InternalSequentialNumber,
                CreditNoteDate = d.CreditNoteDate,
                Reason = d.Reason,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LineCount = d.LineCount,
                TotalAmount = d.TotalAmount,
                Lines = mapper.MapList<CommonSupplierCreditNoteLine>(d.Lines),
                TaxBreakdown = mapper.MapList<CommonSupplierCreditNoteTaxBreakdown>(d.TaxBreakdown),
                CorrectedInvoices = mapper.MapList<CommonSupplierCreditNoteInvoiceRef>(d.CorrectedInvoices)
            });

            mapper.Register<SupplierDeliveryZoneDto, CommonSupplierDeliveryZone>(d => new CommonSupplierDeliveryZone
            {
                SupplierDeliveryZoneToken = d.SupplierDeliveryZoneToken,
                SupplierToken = d.SupplierToken,
                SupplierName = d.SupplierName,
                ZoneToken = d.ZoneToken,
                ZoneCode = d.ZoneCode,
                ZoneName = d.ZoneName,
                CountryCode = d.CountryCode,
                CountryName = d.CountryName,
                DayOfWeek = d.DayOfWeek,
                CreatedUtc = d.CreatedUtc
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
                LanguageCode = r.LanguageCode,
                IsGlobal = r.IsGlobal,
                SupplierType = r.SupplierType,
                HasAccessToSystem = r.HasAccessToSystem,
                LoginEmail = r.LoginEmail,
                Password = r.Password,
                OrganizationToken = r.OrganizationToken
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
                LanguageCode = d.LanguageCode,
                IsGlobal = d.IsGlobal ?? false,
                SupplierType = d.SupplierType ?? SupplierTypeCodes.Product,
                LogoUrl = d.LogoUrl,
                HasAccessToSystem = d.HasAccessToSystem ?? false,
                IsActive = d.IsActive,
                OrganizationToken = d.OrganizationTokenResult,
                OrganizationName = d.OrganizationName
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
                LanguageCode = r.LanguageCode,
                IsGlobal = r.IsGlobal,
                SupplierType = r.SupplierType,
                HasAccessToSystem = r.HasAccessToSystem,
                LoginEmail = r.LoginEmail,
                Password = r.Password,
                OrganizationToken = r.OrganizationToken,
                ConfirmPrivatizationImpact = r.ConfirmPrivatizationImpact
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
                LanguageCode = d.LanguageCode,
                IsGlobal = d.IsGlobal ?? false,
                SupplierType = d.SupplierType ?? SupplierTypeCodes.Product,
                LogoUrl = d.LogoUrl,
                HasAccessToSystem = d.HasAccessToSystem ?? false,
                IsActive = d.IsActive,
                OrganizationToken = d.OrganizationTokenResult,
                OrganizationName = d.OrganizationName
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
                LanguageCode = d.LanguageCode,
                IsGlobal = d.IsGlobal ?? false,
                SupplierType = d.SupplierType ?? SupplierTypeCodes.Product,
                LogoUrl = d.LogoUrl,
                HasAccessToSystem = d.HasAccessToSystem ?? false,
                IsActive = d.IsActive,
                OrganizationToken = d.OrganizationTokenResult,
                OrganizationName = d.OrganizationName
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
                NameTranslations = d.NameTranslations,
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
                NameTranslations = d.NameTranslations,
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
                NameTranslations = d.NameTranslations,
                IsSystem = d.IsSystem,
                IsActive = d.IsActive,
                DefaultTaxCategoryToken = d.DefaultTaxCategoryToken,
                DefaultTaxCategoryCode = d.DefaultTaxCategoryCode
            });
            mapper.Register<SubFamilyDto, CommonSubFamily>(d => new CommonSubFamily
            {
                SubFamilyToken = d.SubFamilyToken,
                FamilyId = d.FamilyId,
                Code = d.Code,
                NameTranslations = d.NameTranslations,
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
                NameTranslations = d.NameTranslations,
                IsSystem = d.IsSystem,
                IsActive = d.IsActive,
                OrganizationToken = d.OrganizationTokenResult,
                OrganizationName = d.OrganizationName
            });
            mapper.Register<SubCategoryDto, CommonSubCategory>(d => new CommonSubCategory
            {
                SubCategoryToken = d.SubCategoryToken,
                CategoryId = d.CategoryId,
                Code = d.Code,
                NameTranslations = d.NameTranslations,
                IsSystem = d.IsSystem,
                IsActive = d.IsActive,
                OrganizationToken = d.OrganizationTokenResult,
                OrganizationName = d.OrganizationName
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
                SupplierType = d.SupplierType,
                Name = d.Name,
                Description = d.Description,
                SupplierSku = d.SupplierSku,
                Barcode = d.Barcode,
                Brand = d.Brand,
                FamilyId = d.FamilyId,
                FamilyCode = d.FamilyCode,
                FamilyNameTranslations = d.FamilyNameTranslations,
                SubFamilyId = d.SubFamilyId,
                SubFamilyCode = d.SubFamilyCode,
                SubFamilyNameTranslations = d.SubFamilyNameTranslations,
                PurchaseUnitToken = d.PurchaseUnitToken,
                PurchaseUnitCode = d.PurchaseUnitCode,
                PurchaseUnitSymbol = d.PurchaseUnitSymbol,
                PurchaseUnitNameTranslations = d.PurchaseUnitNameTranslations,
                PackagingLevels = mapper.MapList<CommonArticlePackagingLevel>(d.PackagingLevels),
                MinimumOrderQty = d.MinimumOrderQty,
                LeadTimeDays = d.LeadTimeDays,
                IsActive = d.IsActive,
                ReplacedByArticleToken = d.ReplacedByArticleToken,
                IsFavorite = d.IsFavorite,
                IsInherited = d.IsInherited,
                FavoriteOrganizationName = d.FavoriteOrganizationName,
                CategoryToken = d.CategoryToken,
                CategoryCode = d.CategoryCode,
                CategoryNameTranslations = d.CategoryNameTranslations,
                SubCategoryToken = d.SubCategoryToken,
                SubCategoryCode = d.SubCategoryCode,
                SubCategoryNameTranslations = d.SubCategoryNameTranslations,
                IsCategoryInherited = d.IsCategoryInherited,
                ClassificationOrganizationName = d.ClassificationOrganizationName,
                TaxCategoryToken = d.TaxCategoryToken,
                TaxCategoryCode = d.TaxCategoryCode,
                EffectiveTaxCategoryCode = d.EffectiveTaxCategoryCode,
                DefaultReceivingUnitToken = d.DefaultReceivingUnitToken,
                DefaultReceivingUnitCode = d.DefaultReceivingUnitCode,
                DefaultReceivingUnitNameTranslations = d.DefaultReceivingUnitNameTranslations
            });
            mapper.Register<ArticlePackagingLevelDto, CommonArticlePackagingLevel>(d => new CommonArticlePackagingLevel
            {
                ArticlePackagingLevelToken = d.ArticlePackagingLevelToken,
                SequenceOrder = d.SequenceOrder,
                UnitOfMeasureToken = d.UnitOfMeasureToken,
                UnitOfMeasureCode = d.UnitOfMeasureCode,
                UnitOfMeasureSymbol = d.UnitOfMeasureSymbol,
                UnitOfMeasureNameTranslations = d.UnitOfMeasureNameTranslations,
                QuantityInParentUnit = d.QuantityInParentUnit,
                IsDefinedUnit = d.IsDefinedUnit
            });
            mapper.Register<ArticlePackagingConversionLevelDto, CommonArticlePackagingConversionLevel>(d => new CommonArticlePackagingConversionLevel
            {
                SequenceOrder = d.SequenceOrder,
                UnitCode = d.UnitCode,
                UnitNameTranslations = d.UnitNameTranslations,
                QuantityPerPurchaseUnit = d.QuantityPerPurchaseUnit,
                IsDefinedUnit = d.IsDefinedUnit
            });
            mapper.Register<ArticlePackagingConversionDto, CommonArticlePackagingConversion>(d => new CommonArticlePackagingConversion
            {
                ArticleToken = d.ArticleToken,
                Name = d.Name,
                PurchaseUnitCode = d.PurchaseUnitCode,
                PurchaseUnitNameTranslations = d.PurchaseUnitNameTranslations,
                Levels = mapper.MapList<CommonArticlePackagingConversionLevel>(d.Levels)
            });
            mapper.Register<ArticlePriceComparisonDto, CommonArticlePriceComparison>(d => new CommonArticlePriceComparison
            {
                ArticleToken = d.ArticleToken,
                Name = d.Name,
                SupplierName = d.SupplierName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                PurchaseUnitNameTranslations = d.PurchaseUnitNameTranslations,
                Price = d.Price,
                CurrencyCode = d.CurrencyCode,
                DefinedUnitCode = d.DefinedUnitCode,
                DefinedUnitNameTranslations = d.DefinedUnitNameTranslations,
                DefinedUnitQuantityPerPurchaseUnit = d.DefinedUnitQuantityPerPurchaseUnit,
                PricePerDefinedUnit = d.PricePerDefinedUnit,
                NormalizedUnitCode = d.NormalizedUnitCode,
                NormalizedUnitNameTranslations = d.NormalizedUnitNameTranslations,
                PricePerNormalizedUnit = d.PricePerNormalizedUnit,
                IsComparable = d.IsComparable,
                IsCheapest = d.IsCheapest
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

            // ArticleFavorite
            mapper.Register<ArticleFavoriteDto, CommonArticleFavorite>(d => new CommonArticleFavorite
            {
                ArticleFavoriteToken = d.ArticleFavoriteToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                SupplierSku = d.SupplierSku,
                SupplierName = d.SupplierName,
                OrganizationToken = d.OrganizationToken,
                OrganizationName = d.OrganizationName,
                IsInherited = d.IsInherited,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            // ArticleClassification
            mapper.Register<ArticleClassificationDto, CommonArticleClassification>(d => new CommonArticleClassification
            {
                ArticleClassificationToken = d.ArticleClassificationToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                SupplierSku = d.SupplierSku,
                SupplierName = d.SupplierName,
                OrganizationToken = d.OrganizationToken,
                OrganizationName = d.OrganizationName,
                CategoryToken = d.CategoryToken,
                CategoryCode = d.CategoryCode,
                SubCategoryToken = d.SubCategoryToken,
                SubCategoryCode = d.SubCategoryCode,
                IsInherited = d.IsInherited,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy
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

            // Warehouse
            mapper.Register<CreateWarehouseCommandRequest, WarehouseDto>(r => new WarehouseDto
            {
                OrganizationToken = r.OrganizationToken,
                Name = r.Name,
                Code = r.Code,
                Description = r.Description,
                AddressLine1 = r.AddressLine1,
                AddressLine2 = r.AddressLine2,
                City = r.City,
                State = r.State,
                PostalCode = r.PostalCode,
                Country = r.Country,
                ZoneToken = r.ZoneToken,
                TaxJurisdictionToken = r.TaxJurisdictionToken,
                IsInventoriable = r.IsInventoriable,
                CanReceivePurchases = r.CanReceivePurchases,
                CanReceiveTransfers = r.CanReceiveTransfers,
                CanTransferOut = r.CanTransferOut,
                CanConsumeInventory = r.CanConsumeInventory,
                CanProduceItems = r.CanProduceItems,
                CanSellItems = r.CanSellItems,
                CanAdjustInventory = r.CanAdjustInventory,
                CanReceiveReturns = r.CanReceiveReturns,
                CanCountInventory = r.CanCountInventory,
                CanIssueToDepartment = r.CanIssueToDepartment,
                TrackLotNumbers = r.TrackLotNumbers,
                TrackExpirationDates = r.TrackExpirationDates,
                TrackSerialNumbers = r.TrackSerialNumbers,
                RequireApproval = r.RequireApproval,
                IsDefaultReceivingWarehouse = r.IsDefaultReceivingWarehouse,
                IsDefaultConsumptionWarehouse = r.IsDefaultConsumptionWarehouse,
                IsMainWarehouse = r.IsMainWarehouse
            });
            mapper.Register<EditWarehouseCommandRequest, WarehouseDto>(r => new WarehouseDto
            {
                WarehouseToken = r.WarehouseToken,
                Name = r.Name,
                Code = r.Code,
                Description = r.Description,
                AddressLine1 = r.AddressLine1,
                AddressLine2 = r.AddressLine2,
                City = r.City,
                State = r.State,
                PostalCode = r.PostalCode,
                Country = r.Country,
                ZoneToken = r.ZoneToken,
                TaxJurisdictionToken = r.TaxJurisdictionToken,
                IsInventoriable = r.IsInventoriable,
                CanReceivePurchases = r.CanReceivePurchases,
                CanReceiveTransfers = r.CanReceiveTransfers,
                CanTransferOut = r.CanTransferOut,
                CanConsumeInventory = r.CanConsumeInventory,
                CanProduceItems = r.CanProduceItems,
                CanSellItems = r.CanSellItems,
                CanAdjustInventory = r.CanAdjustInventory,
                CanReceiveReturns = r.CanReceiveReturns,
                CanCountInventory = r.CanCountInventory,
                CanIssueToDepartment = r.CanIssueToDepartment,
                TrackLotNumbers = r.TrackLotNumbers,
                TrackExpirationDates = r.TrackExpirationDates,
                TrackSerialNumbers = r.TrackSerialNumbers,
                RequireApproval = r.RequireApproval,
                IsDefaultReceivingWarehouse = r.IsDefaultReceivingWarehouse,
                IsDefaultConsumptionWarehouse = r.IsDefaultConsumptionWarehouse,
                IsMainWarehouse = r.IsMainWarehouse
            });
            mapper.Register<WarehouseDto, CreateWarehouseCommandResponse>(d => new CreateWarehouseCommandResponse
            {
                WarehouseId = d.WarehouseId,
                WarehouseToken = d.WarehouseToken,
                OrganizationId = d.OrganizationId,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description,
                AddressLine1 = d.AddressLine1,
                AddressLine2 = d.AddressLine2,
                City = d.City,
                State = d.State,
                PostalCode = d.PostalCode,
                Country = d.Country,
                ZoneToken = d.ZoneToken,
                ZoneCode = d.ZoneCode,
                ZoneName = d.ZoneName,
                CountryCode = d.CountryCode,
                CountryName = d.CountryName,
                TaxJurisdictionToken = d.TaxJurisdictionToken,
                TaxJurisdictionCode = d.TaxJurisdictionCode,
                TaxJurisdictionName = d.TaxJurisdictionName,
                IsInventoriable = d.IsInventoriable,
                CanReceivePurchases = d.CanReceivePurchases,
                CanReceiveTransfers = d.CanReceiveTransfers,
                CanTransferOut = d.CanTransferOut,
                CanConsumeInventory = d.CanConsumeInventory,
                CanProduceItems = d.CanProduceItems,
                CanSellItems = d.CanSellItems,
                CanAdjustInventory = d.CanAdjustInventory,
                CanReceiveReturns = d.CanReceiveReturns,
                CanCountInventory = d.CanCountInventory,
                CanIssueToDepartment = d.CanIssueToDepartment,
                TrackLotNumbers = d.TrackLotNumbers,
                TrackExpirationDates = d.TrackExpirationDates,
                TrackSerialNumbers = d.TrackSerialNumbers,
                RequireApproval = d.RequireApproval,
                IsDefaultReceivingWarehouse = d.IsDefaultReceivingWarehouse,
                IsDefaultConsumptionWarehouse = d.IsDefaultConsumptionWarehouse,
                IsMainWarehouse = d.IsMainWarehouse,
                IsActive = d.IsActive
            });
            mapper.Register<WarehouseDto, EditWarehouseCommandResponse>(d => new EditWarehouseCommandResponse
            {
                WarehouseId = d.WarehouseId,
                WarehouseToken = d.WarehouseToken,
                OrganizationId = d.OrganizationId,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description,
                AddressLine1 = d.AddressLine1,
                AddressLine2 = d.AddressLine2,
                City = d.City,
                State = d.State,
                PostalCode = d.PostalCode,
                Country = d.Country,
                ZoneToken = d.ZoneToken,
                ZoneCode = d.ZoneCode,
                ZoneName = d.ZoneName,
                CountryCode = d.CountryCode,
                CountryName = d.CountryName,
                TaxJurisdictionToken = d.TaxJurisdictionToken,
                TaxJurisdictionCode = d.TaxJurisdictionCode,
                TaxJurisdictionName = d.TaxJurisdictionName,
                IsInventoriable = d.IsInventoriable,
                CanReceivePurchases = d.CanReceivePurchases,
                CanReceiveTransfers = d.CanReceiveTransfers,
                CanTransferOut = d.CanTransferOut,
                CanConsumeInventory = d.CanConsumeInventory,
                CanProduceItems = d.CanProduceItems,
                CanSellItems = d.CanSellItems,
                CanAdjustInventory = d.CanAdjustInventory,
                CanReceiveReturns = d.CanReceiveReturns,
                CanCountInventory = d.CanCountInventory,
                CanIssueToDepartment = d.CanIssueToDepartment,
                TrackLotNumbers = d.TrackLotNumbers,
                TrackExpirationDates = d.TrackExpirationDates,
                TrackSerialNumbers = d.TrackSerialNumbers,
                RequireApproval = d.RequireApproval,
                IsDefaultReceivingWarehouse = d.IsDefaultReceivingWarehouse,
                IsDefaultConsumptionWarehouse = d.IsDefaultConsumptionWarehouse,
                IsMainWarehouse = d.IsMainWarehouse,
                IsActive = d.IsActive
            });
            mapper.Register<WarehouseDto, CommonWarehouse>(d => new CommonWarehouse
            {
                WarehouseId = d.WarehouseId,
                WarehouseToken = d.WarehouseToken,
                OrganizationId = d.OrganizationId,
                Name = d.Name,
                Code = d.Code,
                Description = d.Description,
                AddressLine1 = d.AddressLine1,
                AddressLine2 = d.AddressLine2,
                City = d.City,
                State = d.State,
                PostalCode = d.PostalCode,
                Country = d.Country,
                ZoneToken = d.ZoneToken,
                ZoneCode = d.ZoneCode,
                ZoneName = d.ZoneName,
                CountryCode = d.CountryCode,
                CountryName = d.CountryName,
                TaxJurisdictionToken = d.TaxJurisdictionToken,
                TaxJurisdictionCode = d.TaxJurisdictionCode,
                TaxJurisdictionName = d.TaxJurisdictionName,
                IsInventoriable = d.IsInventoriable,
                CanReceivePurchases = d.CanReceivePurchases,
                CanReceiveTransfers = d.CanReceiveTransfers,
                CanTransferOut = d.CanTransferOut,
                CanConsumeInventory = d.CanConsumeInventory,
                CanProduceItems = d.CanProduceItems,
                CanSellItems = d.CanSellItems,
                CanAdjustInventory = d.CanAdjustInventory,
                CanReceiveReturns = d.CanReceiveReturns,
                CanCountInventory = d.CanCountInventory,
                CanIssueToDepartment = d.CanIssueToDepartment,
                TrackLotNumbers = d.TrackLotNumbers,
                TrackExpirationDates = d.TrackExpirationDates,
                TrackSerialNumbers = d.TrackSerialNumbers,
                RequireApproval = d.RequireApproval,
                IsDefaultReceivingWarehouse = d.IsDefaultReceivingWarehouse,
                IsDefaultConsumptionWarehouse = d.IsDefaultConsumptionWarehouse,
                IsMainWarehouse = d.IsMainWarehouse,
                IsActive = d.IsActive
            });

            // WarehouseContact
            mapper.Register<CreateWarehouseContactCommandRequest, WarehouseContactDto>(r => new WarehouseContactDto
            {
                WarehouseToken = r.WarehouseToken,
                ContactName = r.ContactName,
                ContactType = r.ContactType,
                Department = r.Department,
                Phone = r.Phone,
                Mobile = r.Mobile,
                Fax = r.Fax,
                Email = r.Email,
                Notes = r.Notes,
                IsPrimary = r.IsPrimary,
                HasAccessToSystem = r.HasAccessToSystem,
                LoginEmail = r.LoginEmail,
                Password = r.Password
            });
            mapper.Register<EditWarehouseContactCommandRequest, WarehouseContactDto>(r => new WarehouseContactDto
            {
                WarehouseContactToken = r.WarehouseContactToken,
                ContactName = r.ContactName,
                ContactType = r.ContactType,
                Department = r.Department,
                Phone = r.Phone,
                Mobile = r.Mobile,
                Fax = r.Fax,
                Email = r.Email,
                Notes = r.Notes,
                IsPrimary = r.IsPrimary,
                HasAccessToSystem = r.HasAccessToSystem,
                LoginEmail = r.LoginEmail,
                Password = r.Password
            });
            mapper.Register<WarehouseContactDto, CreateWarehouseContactCommandResponse>(d => new CreateWarehouseContactCommandResponse
            {
                WarehouseContactId = d.WarehouseContactId,
                WarehouseContactToken = d.WarehouseContactToken,
                WarehouseId = d.WarehouseId,
                ContactName = d.ContactName,
                ContactType = d.ContactType,
                Department = d.Department,
                Phone = d.Phone,
                Mobile = d.Mobile,
                Fax = d.Fax,
                Email = d.Email,
                Notes = d.Notes,
                IsPrimary = d.IsPrimary,
                HasAccessToSystem = d.HasAccessToSystem ?? false,
                IsActive = d.IsActive
            });
            mapper.Register<WarehouseContactDto, EditWarehouseContactCommandResponse>(d => new EditWarehouseContactCommandResponse
            {
                WarehouseContactId = d.WarehouseContactId,
                WarehouseContactToken = d.WarehouseContactToken,
                WarehouseId = d.WarehouseId,
                ContactName = d.ContactName,
                ContactType = d.ContactType,
                Department = d.Department,
                Phone = d.Phone,
                Mobile = d.Mobile,
                Fax = d.Fax,
                Email = d.Email,
                Notes = d.Notes,
                IsPrimary = d.IsPrimary,
                HasAccessToSystem = d.HasAccessToSystem ?? false,
                IsActive = d.IsActive
            });
            mapper.Register<WarehouseContactDto, CommonWarehouseContact>(d => new CommonWarehouseContact
            {
                WarehouseContactId = d.WarehouseContactId,
                WarehouseContactToken = d.WarehouseContactToken,
                WarehouseId = d.WarehouseId,
                ContactName = d.ContactName,
                ContactType = d.ContactType,
                Department = d.Department,
                Phone = d.Phone,
                Mobile = d.Mobile,
                Fax = d.Fax,
                Email = d.Email,
                Notes = d.Notes,
                IsPrimary = d.IsPrimary,
                HasAccessToSystem = d.HasAccessToSystem ?? false,
                IsActive = d.IsActive
            });

            // OrderLine (registered before Order since it embeds a List<CommonOrderLine>)
            mapper.Register<OrderLineDto, CommonOrderLine>(d => new CommonOrderLine
            {
                OrderLineToken = d.OrderLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                SupplierId = d.SupplierId,
                SupplierName = d.SupplierName,
                Quantity = d.Quantity,
                PurchaseUnitCode = d.PurchaseUnitCode,
                PurchaseQuantity = d.PurchaseQuantity,
                ContentUnitCode = d.ContentUnitCode,
                ContentQuantity = d.ContentQuantity,
                UnitPrice = d.UnitPrice,
                CurrencyCode = d.CurrencyCode,
                BaseUnitPrice = d.BaseUnitPrice,
                DiscountTypeCode = d.DiscountTypeCode,
                DiscountValue = d.DiscountValue,
                CategoryCode = d.CategoryCode,
                SubCategoryCode = d.SubCategoryCode,
                FamilyCode = d.FamilyCode,
                SubFamilyCode = d.SubFamilyCode,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc
            });

            // PurchaseOrderLine (registered before PurchaseOrder since it embeds a List<CommonPurchaseOrderLine>)
            mapper.Register<PurchaseOrderLineDto, CommonPurchaseOrderLine>(d => new CommonPurchaseOrderLine
            {
                PurchaseOrderLineToken = d.PurchaseOrderLineToken,
                OrderLineToken = d.OrderLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                SupplierId = d.SupplierId,
                SupplierName = d.SupplierName,
                Quantity = d.Quantity,
                PurchaseUnitCode = d.PurchaseUnitCode,
                PurchaseQuantity = d.PurchaseQuantity,
                ContentUnitCode = d.ContentUnitCode,
                ContentQuantity = d.ContentQuantity,
                UnitPrice = d.UnitPrice,
                CurrencyCode = d.CurrencyCode,
                BaseUnitPrice = d.BaseUnitPrice,
                DiscountTypeCode = d.DiscountTypeCode,
                DiscountValue = d.DiscountValue,
                CategoryCode = d.CategoryCode,
                SubCategoryCode = d.SubCategoryCode,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                IsCancelled = d.IsCancelled
            });

            // PurchaseOrderLineRectification (registered before PurchaseOrderRectification since
            // it embeds a List<CommonPurchaseOrderLineRectification>)
            mapper.Register<PurchaseOrderLineRectificationDto, CommonPurchaseOrderLineRectification>(d => new CommonPurchaseOrderLineRectification
            {
                PurchaseOrderLineRectificationToken = d.PurchaseOrderLineRectificationToken,
                PurchaseOrderLineToken = d.PurchaseOrderLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                Action = d.Action,
                PreviousQuantity = d.PreviousQuantity,
                NewQuantity = d.NewQuantity,
                PreviousUnitPrice = d.PreviousUnitPrice,
                NewUnitPrice = d.NewUnitPrice,
                PreviousCurrencyCode = d.PreviousCurrencyCode,
                NewCurrencyCode = d.NewCurrencyCode,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<PurchaseOrderRectificationDto, CommonPurchaseOrderRectification>(d => new CommonPurchaseOrderRectification
            {
                PurchaseOrderRectificationToken = d.PurchaseOrderRectificationToken,
                PurchaseOrderToken = d.PurchaseOrderToken,
                SequenceNumber = d.SequenceNumber,
                Reason = d.Reason,
                Notes = d.Notes,
                Status = d.Status,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                AppliedUtc = d.AppliedUtc,
                Lines = mapper.MapList<CommonPurchaseOrderLineRectification>(d.Lines)
            });

            // GoodsReceiptLine (registered before GoodsReceipt since it embeds a List<CommonGoodsReceiptLine>)
            mapper.Register<GoodsReceiptLineDto, CommonGoodsReceiptLine>(d => new CommonGoodsReceiptLine
            {
                GoodsReceiptLineToken = d.GoodsReceiptLineToken,
                PurchaseOrderLineToken = d.PurchaseOrderLineToken,
                OrderedQuantity = d.OrderedQuantity,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                QuantityAccepted = d.QuantityAccepted,
                QuantityCourtesy = d.QuantityCourtesy,
                QuantityRejected = d.QuantityRejected,
                RejectionReason = d.RejectionReason,
                LotNumber = d.LotNumber,
                ExpirationDate = d.ExpirationDate,
                SerialNumber = d.SerialNumber,
                Notes = d.Notes,
                UnitPrice = d.UnitPrice,
                CurrencyCode = d.CurrencyCode,
                TaxCategoryCode = d.TaxCategoryCode,
                TaxRatePercent = d.TaxRatePercent,
                TaxableAmount = d.TaxableAmount,
                TaxAmount = d.TaxAmount,
                TotalAmount = d.TotalAmount,
                PurchaseUnitCode = d.PurchaseUnitCode,
                EnteredUnitCode = d.EnteredUnitCode,
                EnteredUnitNameTranslations = d.EnteredUnitNameTranslations,
                AcceptedQuantityInUnit = d.AcceptedQuantityInUnit,
                CourtesyQuantityInUnit = d.CourtesyQuantityInUnit,
                RejectedQuantityInUnit = d.RejectedQuantityInUnit,
                DefinedUnitCode = d.DefinedUnitCode,
                DefinedUnitNameTranslations = d.DefinedUnitNameTranslations,
                AcceptedDefinedUnitQuantity = d.AcceptedDefinedUnitQuantity,
                CourtesyDefinedUnitQuantity = d.CourtesyDefinedUnitQuantity,
                RejectedDefinedUnitQuantity = d.RejectedDefinedUnitQuantity,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<GoodsReceiptDto, CommonGoodsReceipt>(d => new CommonGoodsReceipt
            {
                GoodsReceiptToken = d.GoodsReceiptToken,
                PurchaseOrderToken = d.PurchaseOrderToken,
                PurchaseOrderNumber = d.PurchaseOrderNumber,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                DeliveryNoteNumber = d.DeliveryNoteNumber,
                DeliveryNoteDate = d.DeliveryNoteDate,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LineCount = d.LineCount,
                Lines = mapper.MapList<CommonGoodsReceiptLine>(d.Lines)
            });

            mapper.Register<StockLevelDto, CommonStockLevel>(d => new CommonStockLevel
            {
                StockLevelToken = d.StockLevelToken,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                SupplierName = d.SupplierName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                FamilyCode = d.FamilyCode,
                SubFamilyCode = d.SubFamilyCode,
                CategoryCode = d.CategoryCode,
                SubCategoryCode = d.SubCategoryCode,
                QuantityOnHand = d.QuantityOnHand,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy
            });

            mapper.Register<InventoryMovementDto, CommonInventoryMovement>(d => new CommonInventoryMovement
            {
                InventoryMovementToken = d.InventoryMovementToken,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                Type = d.Type,
                Quantity = d.Quantity,
                EnteredUnitCode = d.EnteredUnitCode,
                EnteredQuantity = d.EnteredQuantity,
                DefinedUnitCode = d.DefinedUnitCode,
                DefinedUnitNameTranslations = d.DefinedUnitNameTranslations,
                DefinedUnitQuantity = d.DefinedUnitQuantity,
                GoodsReceiptToken = d.GoodsReceiptToken,
                InventoryTransferToken = d.InventoryTransferToken,
                InventoryPeriodCountToken = d.InventoryPeriodCountToken,
                InternalOrderShipmentToken = d.InternalOrderShipmentToken,
                InternalOrderReceiptToken = d.InternalOrderReceiptToken,
                Reason = d.Reason,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<NotificationDto, CommonNotification>(d => new CommonNotification
            {
                NotificationToken = d.NotificationToken,
                Type = d.Type,
                DataJson = d.DataJson,
                LinkUrl = d.LinkUrl,
                IsRead = d.IsRead,
                ReadUtc = d.ReadUtc,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<SupplierPriceChangeSubscriptionDto, CommonSupplierPriceChangeSubscription>(d => new CommonSupplierPriceChangeSubscription
            {
                SupplierPriceChangeSubscriptionToken = d.SupplierPriceChangeSubscriptionToken,
                SupplierToken = d.SupplierToken,
                SupplierName = d.SupplierName,
                CreatedUtc = d.CreatedUtc
            });

            // InventoryTransferLine (registered before InventoryTransfer since it embeds a List<CommonInventoryTransferLine>)
            mapper.Register<InventoryTransferLineDto, CommonInventoryTransferLine>(d => new CommonInventoryTransferLine
            {
                InventoryTransferLineToken = d.InventoryTransferLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                Quantity = d.Quantity,
                TransferredUnitCode = d.TransferredUnitCode,
                TransferredQuantity = d.TransferredQuantity,
                DefinedUnitCode = d.DefinedUnitCode,
                DefinedUnitNameTranslations = d.DefinedUnitNameTranslations,
                DefinedUnitQuantity = d.DefinedUnitQuantity,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<InventoryTransferDto, CommonInventoryTransfer>(d => new CommonInventoryTransfer
            {
                InventoryTransferToken = d.InventoryTransferToken,
                FromWarehouseToken = d.FromWarehouseToken,
                FromWarehouseName = d.FromWarehouseName,
                ToWarehouseToken = d.ToWarehouseToken,
                ToWarehouseName = d.ToWarehouseName,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LineCount = d.LineCount,
                Lines = mapper.MapList<CommonInventoryTransferLine>(d.Lines)
            });

            // InternalOrderLine (registered before InternalOrder since it embeds a List<CommonInternalOrderLine>)
            mapper.Register<InternalOrderLineDto, CommonInternalOrderLine>(d => new CommonInternalOrderLine
            {
                InternalOrderLineToken = d.InternalOrderLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                SupplierSku = d.SupplierSku,
                Quantity = d.Quantity,
                PurchaseUnitCode = d.PurchaseUnitCode,
                UnitPrice = d.UnitPrice,
                CurrencyCode = d.CurrencyCode,
                Notes = d.Notes,
                QuantityShipped = d.QuantityShipped,
                QuantityAccepted = d.QuantityAccepted,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            // InternalOrderShipmentLine (registered before InternalOrderShipment since it embeds a List<CommonInternalOrderShipmentLine>)
            mapper.Register<InternalOrderShipmentLineDto, CommonInternalOrderShipmentLine>(d => new CommonInternalOrderShipmentLine
            {
                InternalOrderShipmentLineToken = d.InternalOrderShipmentLineToken,
                InternalOrderLineToken = d.InternalOrderLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                QuantityShipped = d.QuantityShipped,
                QuantityReceived = d.QuantityReceived,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<InternalOrderShipmentDto, CommonInternalOrderShipment>(d => new CommonInternalOrderShipment
            {
                InternalOrderShipmentToken = d.InternalOrderShipmentToken,
                SourceWarehouseToken = d.SourceWarehouseToken,
                SourceWarehouseName = d.SourceWarehouseName,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                Lines = mapper.MapList<CommonInternalOrderShipmentLine>(d.Lines)
            });

            // InternalOrderReceiptLine (registered before InternalOrderReceipt since it embeds a List<CommonInternalOrderReceiptLine>)
            mapper.Register<InternalOrderReceiptLineDto, CommonInternalOrderReceiptLine>(d => new CommonInternalOrderReceiptLine
            {
                InternalOrderReceiptLineToken = d.InternalOrderReceiptLineToken,
                InternalOrderShipmentLineToken = d.InternalOrderShipmentLineToken,
                QuantityShipped = d.QuantityShipped,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                QuantityAccepted = d.QuantityAccepted,
                QuantityRejected = d.QuantityRejected,
                RejectionReason = d.RejectionReason,
                TaxCategoryCode = d.TaxCategoryCode,
                TaxRatePercent = d.TaxRatePercent,
                TaxableAmount = d.TaxableAmount,
                TaxAmount = d.TaxAmount,
                TotalAmount = d.TotalAmount,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<InternalOrderReceiptDto, CommonInternalOrderReceipt>(d => new CommonInternalOrderReceipt
            {
                InternalOrderReceiptToken = d.InternalOrderReceiptToken,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                Lines = mapper.MapList<CommonInternalOrderReceiptLine>(d.Lines)
            });

            mapper.Register<InternalOrderDto, CommonInternalOrder>(d => new CommonInternalOrder
            {
                InternalOrderToken = d.InternalOrderToken,
                InternalOrderNumber = d.InternalOrderNumber,
                RequestingOrganizationToken = d.RequestingOrganizationToken,
                RequestingOrganizationName = d.RequestingOrganizationName,
                SourceOrganizationToken = d.SourceOrganizationToken,
                SourceOrganizationName = d.SourceOrganizationName,
                DestinationWarehouseToken = d.DestinationWarehouseToken,
                DestinationWarehouseName = d.DestinationWarehouseName,
                Status = d.Status,
                Notes = d.Notes,
                CancelledUtc = d.CancelledUtc,
                CancelledBy = d.CancelledBy,
                CancelledReason = d.CancelledReason,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy,
                LineCount = d.LineCount,
                Lines = mapper.MapList<CommonInternalOrderLine>(d.Lines),
                Shipments = mapper.MapList<CommonInternalOrderShipment>(d.Shipments),
                Receipts = mapper.MapList<CommonInternalOrderReceipt>(d.Receipts)
            });

            // InventoryPeriodCountDto registered before InventoryPeriodDto since it embeds a List<CommonInventoryPeriodCount>
            mapper.Register<InventoryPeriodCountDto, CommonInventoryPeriodCount>(d => new CommonInventoryPeriodCount
            {
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                FamilyCode = d.FamilyCode,
                SubFamilyCode = d.SubFamilyCode,
                CategoryCode = d.CategoryCode,
                SubCategoryCode = d.SubCategoryCode,
                OpeningQuantity = d.OpeningQuantity,
                CountedQuantity = d.CountedQuantity,
                SystemQuantityAtClose = d.SystemQuantityAtClose,
                VarianceQuantity = d.VarianceQuantity,
                CountedUnitCode = d.CountedUnitCode,
                CountedQuantityInUnit = d.CountedQuantityInUnit,
                DefinedUnitCode = d.DefinedUnitCode,
                DefinedUnitNameTranslations = d.DefinedUnitNameTranslations,
                DefinedUnitQuantity = d.DefinedUnitQuantity,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy
            });

            mapper.Register<InventoryPeriodDto, CommonInventoryPeriod>(d => new CommonInventoryPeriod
            {
                InventoryPeriodToken = d.InventoryPeriodToken,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                Status = d.Status,
                StartDate = d.StartDate,
                ClosedUtc = d.ClosedUtc,
                ClosedBy = d.ClosedBy,
                ReopenedUtc = d.ReopenedUtc,
                ReopenedBy = d.ReopenedBy,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LineCount = d.LineCount,
                Lines = mapper.MapList<CommonInventoryPeriodCount>(d.Lines)
            });

            mapper.Register<MonthlySpendDto, CommonMonthlySpend>(d => new CommonMonthlySpend
            {
                Year = d.Year,
                Month = d.Month,
                Total = d.Total
            });

            mapper.Register<RecentActivityItemDto, CommonRecentActivityItem>(d => new CommonRecentActivityItem
            {
                ActivityType = d.ActivityType,
                ReferenceLabel = d.ReferenceLabel,
                ActorName = d.ActorName,
                OccurredUtc = d.OccurredUtc
            });

            mapper.Register<SupplierSpendDto, CommonSupplierSpend>(d => new CommonSupplierSpend
            {
                SupplierToken = d.SupplierToken,
                SupplierName = d.SupplierName,
                Total = d.Total
            });

            mapper.Register<SupplierReturnLineDto, CommonSupplierReturnLine>(d => new CommonSupplierReturnLine
            {
                SupplierReturnLineToken = d.SupplierReturnLineToken,
                GoodsReceiptLineToken = d.GoodsReceiptLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                QuantityRejected = d.QuantityRejected,
                RejectionReason = d.RejectionReason,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                UnitPrice = d.UnitPrice,
                CurrencyCode = d.CurrencyCode,
                TaxRatePercent = d.TaxRatePercent
            });

            mapper.Register<SupplierReturnDto, CommonSupplierReturn>(d => new CommonSupplierReturn
            {
                SupplierReturnToken = d.SupplierReturnToken,
                PurchaseOrderToken = d.PurchaseOrderToken,
                PurchaseOrderNumber = d.PurchaseOrderNumber,
                SupplierToken = d.SupplierToken,
                SupplierName = d.SupplierName,
                Status = d.Status,
                ResolutionType = d.ResolutionType,
                Notes = d.Notes,
                ClosedUtc = d.ClosedUtc,
                ClosedBy = d.ClosedBy,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LineCount = d.LineCount,
                Lines = mapper.MapList<CommonSupplierReturnLine>(d.Lines)
            });

            mapper.Register<EligibleReturnLineDto, CommonEligibleReturnLine>(d => new CommonEligibleReturnLine
            {
                GoodsReceiptLineToken = d.GoodsReceiptLineToken,
                DeliveryNoteNumber = d.DeliveryNoteNumber,
                ReceivedUtc = d.ReceivedUtc,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                QuantityRejected = d.QuantityRejected,
                RejectionReason = d.RejectionReason
            });

            mapper.Register<SupplierScorecardDto, CommonSupplierScorecard>(d => new CommonSupplierScorecard
            {
                SupplierToken = d.SupplierToken,
                TotalReceiptLines = d.TotalReceiptLines,
                TotalAccepted = d.TotalAccepted,
                TotalCourtesy = d.TotalCourtesy,
                TotalRejected = d.TotalRejected,
                RejectionRatePercent = d.RejectionRatePercent,
                OtdEligibleLines = d.OtdEligibleLines,
                OnTimeDeliveryPercent = d.OnTimeDeliveryPercent,
                OnTimeInFullPercent = d.OnTimeInFullPercent,
                AvgLeadTimeDays = d.AvgLeadTimeDays
            });

            mapper.Register<DashboardSummaryDto, CommonDashboardSummary>(d => new CommonDashboardSummary
            {
                PendingApprovalsCount = d.PendingApprovalsCount,
                BelowParCount = d.BelowParCount,
                SpendThisMonth = d.SpendThisMonth,
                SpendLastMonth = d.SpendLastMonth,
                SpendCurrencyCode = d.SpendCurrencyCode,
                MonthlySpend = mapper.MapList<CommonMonthlySpend>(d.MonthlySpend),
                OpenPurchaseOrdersAwaitingReceiptCount = d.OpenPurchaseOrdersAwaitingReceiptCount,
                TopSuppliersBySpend = mapper.MapList<CommonSupplierSpend>(d.TopSuppliersBySpend),
                RecentActivity = mapper.MapList<CommonRecentActivityItem>(d.RecentActivity)
            });

            mapper.Register<ParLevelDto, CommonParLevel>(d => new CommonParLevel
            {
                ParLevelToken = d.ParLevelToken,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                MinimumQuantity = d.MinimumQuantity,
                ReorderQuantity = d.ReorderQuantity,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy
            });

            mapper.Register<ParLevelOverrideDto, CommonParLevelOverride>(d => new CommonParLevelOverride
            {
                ParLevelOverrideToken = d.ParLevelOverrideToken,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                Type = d.Type,
                Label = d.Label,
                MinimumQuantity = d.MinimumQuantity,
                ReorderQuantity = d.ReorderQuantity,
                StartMonth = d.StartMonth,
                StartDay = d.StartDay,
                EndMonth = d.EndMonth,
                EndDay = d.EndDay,
                StartDate = d.StartDate,
                EndDate = d.EndDate,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<ParLevelEffectiveDto, CommonParLevelEffective>(d => new CommonParLevelEffective
            {
                BaseMinimumQuantity = d.BaseMinimumQuantity,
                BaseReorderQuantity = d.BaseReorderQuantity,
                EffectiveMinimumQuantity = d.EffectiveMinimumQuantity,
                EffectiveReorderQuantity = d.EffectiveReorderQuantity,
                EffectiveSource = d.EffectiveSource,
                EffectiveOverrideToken = d.EffectiveOverrideToken,
                EffectiveOverrideLabel = d.EffectiveOverrideLabel
            });

            mapper.Register<BelowParRowDto, CommonBelowParRow>(d => new CommonBelowParRow
            {
                ParLevelToken = d.ParLevelToken,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                LeadTimeDays = d.LeadTimeDays,
                SupplierName = d.SupplierName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                FamilyCode = d.FamilyCode,
                SubFamilyCode = d.SubFamilyCode,
                CategoryCode = d.CategoryCode,
                SubCategoryCode = d.SubCategoryCode,
                QuantityOnHand = d.QuantityOnHand,
                EffectiveMinimumQuantity = d.EffectiveMinimumQuantity,
                EffectiveReorderQuantity = d.EffectiveReorderQuantity,
                EffectiveSource = d.EffectiveSource,
                OverrideLabel = d.OverrideLabel
            });

            // OrderApprovalStep (registered before Order since it embeds a List<CommonOrderApprovalStep>)
            mapper.Register<OrderApprovalStepDto, CommonOrderApprovalStep>(d => new CommonOrderApprovalStep
            {
                OrderApprovalStepToken = d.OrderApprovalStepToken,
                OrderToken = d.OrderToken,
                OrganizationToken = d.OrganizationToken,
                OrganizationName = d.OrganizationName,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                FamilyCode = d.FamilyCode,
                Level = d.Level,
                ThresholdAmount = d.ThresholdAmount,
                ActualFamilyAmount = d.ActualFamilyAmount,
                CurrencyCode = d.CurrencyCode,
                ApproverUserToken = d.ApproverUserToken,
                ApproverName = d.ApproverName,
                Status = d.Status,
                DecidedUtc = d.DecidedUtc,
                DecidedBy = d.DecidedBy,
                RejectionReason = d.RejectionReason,
                CreatedUtc = d.CreatedUtc
            });

            // Anonymous single-use email-approval — see .claude/OrderApprovalModule.md
            mapper.Register<OrderApprovalEmailPreviewDto, OrderApprovalEmailPreviewResponse>(d => new OrderApprovalEmailPreviewResponse
            {
                Status = d.Status,
                OrganizationName = d.OrganizationName,
                WarehouseName = d.WarehouseName,
                FamilyCode = d.FamilyCode,
                Level = d.Level,
                ThresholdAmount = d.ThresholdAmount,
                ActualFamilyAmount = d.ActualFamilyAmount,
                CurrencyCode = d.CurrencyCode,
                OrderReference = d.OrderReference
            });
            mapper.Register<OrderApprovalEmailApproveResultDto, OrderApprovalEmailApproveResultResponse>(d => new OrderApprovalEmailApproveResultResponse
            {
                FamilyCode = d.FamilyCode,
                Level = d.Level,
                OrderFullyApproved = d.OrderFullyApproved
            });

            mapper.Register<FamilyApprovalThresholdDto, CommonFamilyApprovalThreshold>(d => new CommonFamilyApprovalThreshold
            {
                FamilyApprovalThresholdToken = d.FamilyApprovalThresholdToken,
                OrganizationToken = d.OrganizationToken,
                OrganizationName = d.OrganizationName,
                FamilyToken = d.FamilyToken,
                FamilyCode = d.FamilyCode,
                Level = d.Level,
                ThresholdAmount = d.ThresholdAmount,
                ApproverUserToken = d.ApproverUserToken,
                ApproverName = d.ApproverName,
                IsActive = d.IsActive,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy
            });

            mapper.Register<ArticleDiscountDto, CommonArticleDiscount>(d => new CommonArticleDiscount
            {
                ArticleDiscountToken = d.ArticleDiscountToken,
                SupplierToken = d.SupplierToken,
                SupplierName = d.SupplierName,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                SubFamilyToken = d.SubFamilyToken,
                SubFamilyCode = d.SubFamilyCode,
                FamilyToken = d.FamilyToken,
                FamilyCode = d.FamilyCode,
                DiscountTypeCode = d.DiscountTypeCode,
                DiscountValue = d.DiscountValue,
                CurrencyCode = d.CurrencyCode,
                EffectiveFrom = d.EffectiveFrom,
                EffectiveUntil = d.EffectiveUntil,
                Description = d.Description,
                IsActive = d.IsActive,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy
            });

            // Order
            mapper.Register<OrderDto, CommonOrder>(d => new CommonOrder
            {
                OrderToken = d.OrderToken,
                OrganizationToken = d.OrganizationToken,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                Status = d.Status,
                Notes = d.Notes,
                SubmittedUtc = d.SubmittedUtc,
                PdfUrl = d.PdfUrl,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                CreatedByEmail = d.CreatedByEmail,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy,
                LastUpdatedByEmail = d.LastUpdatedByEmail,
                LineCount = d.LineCount,
                Lines = mapper.MapList<CommonOrderLine>(d.Lines),
                ApprovalSteps = mapper.MapList<CommonOrderApprovalStep>(d.ApprovalSteps)
            });

            // PurchaseOrder
            mapper.Register<PurchaseOrderDto, CommonPurchaseOrder>(d => new CommonPurchaseOrder
            {
                PurchaseOrderToken = d.PurchaseOrderToken,
                PurchaseOrderNumber = d.PurchaseOrderNumber,
                OrderToken = d.OrderToken,
                SupplierId = d.SupplierId,
                SupplierToken = d.SupplierToken,
                SupplierName = d.SupplierName,
                OrganizationToken = d.OrganizationToken,
                OrganizationName = d.OrganizationName,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                Status = d.Status,
                SentUtc = d.SentUtc,
                CancelledUtc = d.CancelledUtc,
                CancelledBy = d.CancelledBy,
                ClosedShortUtc = d.ClosedShortUtc,
                ClosedShortBy = d.ClosedShortBy,
                ClosedShortReason = d.ClosedShortReason,
                CreatedUtc = d.CreatedUtc,
                LineCount = d.LineCount,
                Lines = mapper.MapList<CommonPurchaseOrderLine>(d.Lines)
            });

            // ConsolidatedPurchaseOrderMember (registered before ConsolidatedPurchaseOrder since
            // it embeds a List<CommonConsolidatedPurchaseOrderMember>)
            mapper.Register<ConsolidatedPurchaseOrderMemberDto, CommonConsolidatedPurchaseOrderMember>(d => new CommonConsolidatedPurchaseOrderMember
            {
                PurchaseOrderToken = d.PurchaseOrderToken,
                PurchaseOrderNumber = d.PurchaseOrderNumber,
                OrderToken = d.OrderToken,
                SupplierId = d.SupplierId,
                SupplierName = d.SupplierName,
                OrganizationToken = d.OrganizationToken,
                OrganizationName = d.OrganizationName,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                Status = d.Status,
                SentUtc = d.SentUtc,
                LineCount = d.LineCount,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<ConsolidatedPurchaseOrderDto, CommonConsolidatedPurchaseOrder>(d => new CommonConsolidatedPurchaseOrder
            {
                ConsolidatedPurchaseOrderToken = d.ConsolidatedPurchaseOrderToken,
                SupplierId = d.SupplierId,
                SupplierName = d.SupplierName,
                SuperAssociateOrganizationToken = d.SuperAssociateOrganizationToken,
                SuperAssociateOrganizationName = d.SuperAssociateOrganizationName,
                Title = d.Title,
                Notes = d.Notes,
                DateRangeFrom = d.DateRangeFrom,
                DateRangeTo = d.DateRangeTo,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                MemberCount = d.MemberCount,
                Members = mapper.MapList<CommonConsolidatedPurchaseOrderMember>(d.Members)
            });

            // OrderTemplateLine (registered before OrderTemplate since it embeds a List<CommonOrderTemplateLine>)
            mapper.Register<OrderTemplateLineDto, CommonOrderTemplateLine>(d => new CommonOrderTemplateLine
            {
                OrderTemplateLineToken = d.OrderTemplateLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                SupplierId = d.SupplierId,
                SupplierName = d.SupplierName,
                SupplierSku = d.SupplierSku,
                SupplierType = d.SupplierType,
                PurchaseUnitCode = d.PurchaseUnitCode,
                PurchaseUnitSymbol = d.PurchaseUnitSymbol,
                IsArticleActive = d.IsArticleActive,
                IsArticleDeleted = d.IsArticleDeleted,
                ReplacedByArticleToken = d.ReplacedByArticleToken,
                Quantity = d.Quantity,
                CreatedUtc = d.CreatedUtc
            });

            // OrderTemplate
            mapper.Register<OrderTemplateDto, CommonOrderTemplate>(d => new CommonOrderTemplate
            {
                OrderTemplateToken = d.OrderTemplateToken,
                OrganizationToken = d.OrganizationToken,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                IsWarehouseActive = d.IsWarehouseActive,
                OwnerUserToken = d.OwnerUserToken,
                OwnerFirstName = d.OwnerFirstName,
                OwnerLastName = d.OwnerLastName,
                OwnerEmail = d.OwnerEmail,
                Name = d.Name,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy,
                LineCount = d.LineCount,
                Lines = mapper.MapList<CommonOrderTemplateLine>(d.Lines)
            });

            // ApplyOrderTemplate result (Orders module — template-to-order apply summary)
            mapper.Register<ApplyOrderTemplateLineResultDto, CommonApplyOrderTemplateLineResult>(d => new CommonApplyOrderTemplateLineResult
            {
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                SupplierName = d.SupplierName,
                SupplierType = d.SupplierType,
                Quantity = d.Quantity,
                Outcome = d.Outcome,
                ErrorCode = d.ErrorCode,
                ErrorMessage = d.ErrorMessage
            });
            mapper.Register<ApplyOrderTemplateResultDto, ApplyOrderTemplateCommandResponse>(d => new ApplyOrderTemplateCommandResponse
            {
                OrderTemplateToken = d.OrderTemplateToken,
                OrderToken = d.OrderToken,
                TotalLines = d.TotalLines,
                SucceededCount = d.SucceededCount,
                NeedsManualPriceCount = d.NeedsManualPriceCount,
                FailedCount = d.FailedCount,
                Lines = mapper.MapList<CommonApplyOrderTemplateLineResult>(d.Lines)
            });

            // ImportOrderLines result (Orders module — Excel-into-order bulk add summary)
            mapper.Register<ImportOrderLinesRowErrorDto, ImportOrderLinesRowError>(d => new ImportOrderLinesRowError
            {
                RowNumber = d.RowNumber,
                Identifier = d.Identifier,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<ImportOrderLinesResultDto, ImportOrderLinesCommandResponse>(d => new ImportOrderLinesCommandResponse
            {
                TotalRows = d.TotalRows,
                SucceededCount = d.SucceededCount,
                FailureCount = d.FailureCount,
                Errors = mapper.MapList<ImportOrderLinesRowError>(d.Errors)
            });

            // CopyOrder result (Orders module — copy a SUBMITTED order into a new Draft)
            mapper.Register<CopyOrderSkippedLineDto, CopyOrderSkippedLineResponse>(d => new CopyOrderSkippedLineResponse
            {
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                Code = d.Code,
                Description = d.Description
            });
            mapper.Register<CopyOrderResultDto, CopyOrderCommandResponse>(d => new CopyOrderCommandResponse
            {
                NewOrderToken = d.NewOrderToken,
                TotalLines = d.TotalLines,
                CopiedCount = d.CopiedCount,
                SkippedCount = d.SkippedCount,
                SkippedLines = mapper.MapList<CopyOrderSkippedLineResponse>(d.SkippedLines)
            });

            // Department
            mapper.Register<CreateDepartmentCommandRequest, DepartmentDto>(r => new DepartmentDto
            {
                OrganizationToken = r.OrganizationToken,
                Name = r.Name,
                Code = r.Code
            });
            mapper.Register<EditDepartmentCommandRequest, DepartmentDto>(r => new DepartmentDto
            {
                DepartmentToken = r.DepartmentToken,
                Name = r.Name,
                Code = r.Code
            });
            mapper.Register<DepartmentDto, CommonDepartment>(d => new CommonDepartment
            {
                DepartmentToken = d.DepartmentToken,
                OrganizationToken = d.OrganizationToken,
                OrganizationName = d.OrganizationName,
                Name = d.Name,
                Code = d.Code,
                IsActive = d.IsActive,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy
            });

            // RequisitionLine (registered before Requisition since it embeds a List<CommonRequisitionLine>)
            mapper.Register<RequisitionLineDto, CommonRequisitionLine>(d => new CommonRequisitionLine
            {
                RequisitionLineToken = d.RequisitionLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                QuantityRequested = d.QuantityRequested,
                QuantityIssued = d.QuantityIssued,
                RequestedUnitCode = d.RequestedUnitCode,
                RequestedQuantity = d.RequestedQuantity,
                DefinedUnitCode = d.DefinedUnitCode,
                DefinedUnitNameTranslations = d.DefinedUnitNameTranslations,
                DefinedUnitQuantity = d.DefinedUnitQuantity,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            // RequisitionIssueLine (registered before RequisitionIssue since it embeds a List<CommonRequisitionIssueLine>)
            mapper.Register<RequisitionIssueLineDto, CommonRequisitionIssueLine>(d => new CommonRequisitionIssueLine
            {
                RequisitionIssueLineToken = d.RequisitionIssueLineToken,
                RequisitionLineToken = d.RequisitionLineToken,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                QuantityIssued = d.QuantityIssued,
                IssuedUnitCode = d.IssuedUnitCode,
                IssuedQuantity = d.IssuedQuantity,
                DefinedUnitCode = d.DefinedUnitCode,
                DefinedUnitNameTranslations = d.DefinedUnitNameTranslations,
                DefinedUnitQuantity = d.DefinedUnitQuantity,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy
            });

            mapper.Register<RequisitionIssueDto, CommonRequisitionIssue>(d => new CommonRequisitionIssue
            {
                RequisitionIssueToken = d.RequisitionIssueToken,
                Notes = d.Notes,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                Lines = mapper.MapList<CommonRequisitionIssueLine>(d.Lines)
            });

            mapper.Register<RequisitionDto, CommonRequisition>(d => new CommonRequisition
            {
                RequisitionToken = d.RequisitionToken,
                RequisitionNumber = d.RequisitionNumber,
                OrganizationToken = d.OrganizationToken,
                OrganizationName = d.OrganizationName,
                WarehouseToken = d.WarehouseToken,
                WarehouseName = d.WarehouseName,
                DepartmentToken = d.DepartmentToken,
                DepartmentName = d.DepartmentName,
                Status = d.Status,
                Notes = d.Notes,
                ApprovedUtc = d.ApprovedUtc,
                ApprovedBy = d.ApprovedBy,
                RejectedUtc = d.RejectedUtc,
                RejectedBy = d.RejectedBy,
                RejectedReason = d.RejectedReason,
                CancelledUtc = d.CancelledUtc,
                CancelledBy = d.CancelledBy,
                CancelledReason = d.CancelledReason,
                ClosedShortUtc = d.ClosedShortUtc,
                ClosedShortBy = d.ClosedShortBy,
                ClosedShortReason = d.ClosedShortReason,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy,
                LineCount = d.LineCount,
                Lines = mapper.MapList<CommonRequisitionLine>(d.Lines),
                Issues = mapper.MapList<CommonRequisitionIssue>(d.Issues)
            });

            mapper.Register<DepartmentParLevelDto, CommonDepartmentParLevel>(d => new CommonDepartmentParLevel
            {
                DepartmentParLevelToken = d.DepartmentParLevelToken,
                DepartmentToken = d.DepartmentToken,
                DepartmentName = d.DepartmentName,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                MinimumQuantity = d.MinimumQuantity,
                ReorderQuantity = d.ReorderQuantity,
                IsActive = d.IsActive,
                CreatedUtc = d.CreatedUtc,
                CreatedBy = d.CreatedBy,
                LastUpdatedUtc = d.LastUpdatedUtc,
                LastUpdatedBy = d.LastUpdatedBy
            });

            mapper.Register<SuggestedRequisitionDto, CommonSuggestedRequisition>(d => new CommonSuggestedRequisition
            {
                DepartmentParLevelToken = d.DepartmentParLevelToken,
                DepartmentToken = d.DepartmentToken,
                DepartmentName = d.DepartmentName,
                ArticleToken = d.ArticleToken,
                ArticleName = d.ArticleName,
                PurchaseUnitCode = d.PurchaseUnitCode,
                MinimumQuantity = d.MinimumQuantity,
                SuggestedQuantity = d.SuggestedQuantity,
                AvgDailyConsumption = d.AvgDailyConsumption,
                LastIssuedUtc = d.LastIssuedUtc,
                DaysSinceLastIssued = d.DaysSinceLastIssued,
                ExpectedCycleDays = d.ExpectedCycleDays
            });
        }
    }
}
