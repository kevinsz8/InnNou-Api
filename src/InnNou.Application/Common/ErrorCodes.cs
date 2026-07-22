namespace InnNou.Application.Common
{
    /// <summary>
    /// Single source of truth for every <see cref="ApiError.Code"/> literal returned by the API.
    /// The frontend team mirrors these values for i18n translation keys, so codes must never
    /// change once published — only add new ones.
    /// </summary>
    public static class ErrorCodes
    {
        // Auth
        public const string InvalidCredentials = "INVALID_CREDENTIALS";
        public const string InvalidToken = "INVALID_TOKEN";
        public const string NotAuthenticated = "NOT_AUTHENTICATED";
        public const string NotImpersonating = "NOT_IMPERSONATING";
        public const string StopImpersonationFailed = "STOP_IMPERSONATION_FAILED";
        public const string Unauthorized = "UNAUTHORIZED";
        public const string Forbidden = "FORBIDDEN";

        // User
        public const string UserNotFound = "USER_NOT_FOUND";
        public const string UserAlreadyExists = "USER_ALREADY_EXISTS";
        public const string UserCreationFailed = "USER_CREATION_FAILED";
        public const string UserCannotAssignHigherRole = "USER_CANNOT_ASSIGN_HIGHER_ROLE";
        public const string UserCannotEditHigherRole = "USER_CANNOT_EDIT_HIGHER_ROLE";
        public const string UserCannotDeleteHigherRole = "USER_CANNOT_DELETE_HIGHER_ROLE";
        public const string UserInvalidRole = "USER_INVALID_ROLE";
        public const string UserInvalidOrganizationAssignment = "USER_INVALID_ORGANIZATION_ASSIGNMENT";
        public const string UserInvalidOrganizationContext = "USER_INVALID_ORGANIZATION_CONTEXT";
        public const string UserOrgAndSupplierConflict = "USER_ORG_AND_SUPPLIER_CONFLICT";
        public const string UserOutsideOrganization = "USER_OUTSIDE_ORGANIZATION";
        public const string UserSupplierCreateSuperadminOnly = "USER_SUPPLIER_CREATE_SUPERADMIN_ONLY";
        public const string UserBulkImportForbidden = "USER_BULK_IMPORT_FORBIDDEN";
        public const string UserBulkImportInvalidFile = "USER_BULK_IMPORT_INVALID_FILE";
        public const string UserBulkImportTooManyRows = "USER_BULK_IMPORT_TOO_MANY_ROWS";
        public const string UserBulkImportRowInvalid = "USER_BULK_IMPORT_ROW_INVALID";
        public const string UserBulkImportWeakPassword = "USER_BULK_IMPORT_WEAK_PASSWORD";
        public const string UserBulkImportRowFailed = "USER_BULK_IMPORT_ROW_FAILED";

        // Organization
        public const string OrganizationNotFound = "ORGANIZATION_NOT_FOUND";
        public const string OrganizationAlreadyExists = "ORGANIZATION_ALREADY_EXISTS";
        public const string OrganizationCreationFailed = "ORGANIZATION_CREATION_FAILED";
        public const string OrganizationCreateForbidden = "ORGANIZATION_CREATE_FORBIDDEN";
        public const string OrganizationDeleteForbidden = "ORGANIZATION_DELETE_FORBIDDEN";
        public const string OrganizationOutsideScope = "ORGANIZATION_OUTSIDE_SCOPE";
        public const string OrganizationParentOutsideScope = "ORGANIZATION_PARENT_OUTSIDE_SCOPE";
        public const string OrganizationInvalidCurrency = "ORGANIZATION_INVALID_CURRENCY";
        public const string OrganizationInvalidZone = "ORGANIZATION_INVALID_ZONE";
        public const string OrganizationZoneRequiresAssociateType = "ORGANIZATION_ZONE_REQUIRES_ASSOCIATE_TYPE";
        public const string OrganizationBulkImportForbidden = "ORGANIZATION_BULK_IMPORT_FORBIDDEN";
        public const string OrganizationBulkImportInvalidFile = "ORGANIZATION_BULK_IMPORT_INVALID_FILE";
        public const string OrganizationBulkImportTooManyRows = "ORGANIZATION_BULK_IMPORT_TOO_MANY_ROWS";
        public const string OrganizationBulkImportRowInvalid = "ORGANIZATION_BULK_IMPORT_ROW_INVALID";
        public const string OrganizationBulkImportRowFailed = "ORGANIZATION_BULK_IMPORT_ROW_FAILED";

        // Supplier
        public const string SupplierNotFound = "SUPPLIER_NOT_FOUND";
        public const string SupplierAlreadyExists = "SUPPLIER_ALREADY_EXISTS";
        public const string SupplierCreationFailed = "SUPPLIER_CREATION_FAILED";
        public const string SupplierOutsideScope = "SUPPLIER_OUTSIDE_SCOPE";
        public const string SupplierAccessSuperadminOnly = "SUPPLIER_ACCESS_SUPERADMIN_ONLY";
        public const string SupplierCreateSuperadminOnly = "SUPPLIER_CREATE_SUPERADMIN_ONLY";
        public const string SupplierDeleteSuperadminOnly = "SUPPLIER_DELETE_SUPERADMIN_ONLY";
        public const string SupplierLoginCredentialsRequired = "SUPPLIER_LOGIN_CREDENTIALS_REQUIRED";
        public const string SupplierLoginEmailExists = "SUPPLIER_LOGIN_EMAIL_EXISTS";
        public const string SupplierInvalidType = "SUPPLIER_INVALID_TYPE";
        public const string SupplierBulkImportForbidden = "SUPPLIER_BULK_IMPORT_FORBIDDEN";
        public const string SupplierBulkImportInvalidFile = "SUPPLIER_BULK_IMPORT_INVALID_FILE";
        public const string SupplierBulkImportTooManyRows = "SUPPLIER_BULK_IMPORT_TOO_MANY_ROWS";
        public const string SupplierBulkImportRowInvalid = "SUPPLIER_BULK_IMPORT_ROW_INVALID";
        public const string SupplierBulkImportRowFailed = "SUPPLIER_BULK_IMPORT_ROW_FAILED";
        public const string SupplierCreateForbidden = "SUPPLIER_CREATE_FORBIDDEN";
        public const string SupplierCreateGlobalForbidden = "SUPPLIER_CREATE_GLOBAL_FORBIDDEN";
        public const string SupplierOrganizationTokenRequired = "SUPPLIER_ORGANIZATION_TOKEN_REQUIRED";
        public const string SupplierOrganizationNotFound = "SUPPLIER_ORGANIZATION_NOT_FOUND";
        public const string SupplierOwnershipChangeSuperadminOnly = "SUPPLIER_OWNERSHIP_CHANGE_SUPERADMIN_ONLY";
        public const string SupplierPrivatizationImpact = "SUPPLIER_PRIVATIZATION_IMPACT";

        // Article
        public const string ArticleNotFound = "ARTICLE_NOT_FOUND";
        public const string ArticleCreateFailed = "ARTICLE_CREATE_FAILED";
        public const string ArticleSkuExists = "ARTICLE_SKU_EXISTS";
        public const string ArticleAlreadyReplaced = "ARTICLE_ALREADY_REPLACED";
        public const string ArticleSupersedeFailed = "ARTICLE_SUPERSEDE_FAILED";
        public const string ArticleStructuralChangeNotAllowed = "ARTICLE_STRUCTURAL_CHANGE_NOT_ALLOWED";
        public const string NoStructuralChange = "NO_STRUCTURAL_CHANGE";
        public const string ArticleSupplierForbidden = "ARTICLE_SUPPLIER_FORBIDDEN";
        public const string ArticleSupplierZoneNotCovered = "ARTICLE_SUPPLIER_ZONE_NOT_COVERED";
        public const string ArticleBulkImportForbidden = "ARTICLE_BULK_IMPORT_FORBIDDEN";
        public const string ArticleBulkImportInvalidFile = "ARTICLE_BULK_IMPORT_INVALID_FILE";
        public const string ArticleBulkImportTooManyRows = "ARTICLE_BULK_IMPORT_TOO_MANY_ROWS";
        public const string ArticleBulkImportRowInvalid = "ARTICLE_BULK_IMPORT_ROW_INVALID";
        public const string ArticleBulkImportRowFailed = "ARTICLE_BULK_IMPORT_ROW_FAILED";

        // Article packaging levels
        public const string ArticlePackagingLevelsRequired = "ARTICLE_PACKAGING_LEVELS_REQUIRED";
        public const string ArticlePackagingLevelInvalidSequence = "ARTICLE_PACKAGING_LEVEL_INVALID_SEQUENCE";
        public const string ArticlePackagingLevelUnitNotFound = "ARTICLE_PACKAGING_LEVEL_UNIT_NOT_FOUND";
        public const string ArticlePackagingLevelDefinedUnitRequired = "ARTICLE_PACKAGING_LEVEL_DEFINED_UNIT_REQUIRED";
        public const string ArticlePackagingLevelDefinedUnitMustBeLast = "ARTICLE_PACKAGING_LEVEL_DEFINED_UNIT_MUST_BE_LAST";
        public const string ArticlePackagingLevelIndefiniteUnitMustBeCount = "ARTICLE_PACKAGING_LEVEL_INDEFINITE_UNIT_MUST_BE_COUNT";
        public const string ArticlePackagingLevelInvalidQuantity = "ARTICLE_PACKAGING_LEVEL_INVALID_QUANTITY";

        // ArticlePrice
        public const string ArticlePriceNotFound = "ARTICLE_PRICE_NOT_FOUND";
        public const string ArticlePriceCreateFailed = "ARTICLE_PRICE_CREATE_FAILED";
        public const string ArticlePriceSupplierForbidden = "ARTICLE_PRICE_SUPPLIER_FORBIDDEN";
        public const string ArticlePriceArticleReplaced = "ARTICLE_PRICE_ARTICLE_REPLACED";
        public const string ArticlePriceInvalidCurrency = "ARTICLE_PRICE_INVALID_CURRENCY";
        public const string ArticlePriceInvalidAmount = "ARTICLE_PRICE_INVALID_AMOUNT";
        public const string ArticlePriceDuplicateEffectiveDate = "ARTICLE_PRICE_DUPLICATE_EFFECTIVE_DATE";
        public const string ArticlePriceCurrencyRequired = "ARTICLE_PRICE_CURRENCY_REQUIRED";
        public const string ArticlePriceManualRequired = "ARTICLE_PRICE_MANUAL_REQUIRED";
        public const string ArticlePriceBulkImportForbidden = "ARTICLE_PRICE_BULK_IMPORT_FORBIDDEN";
        public const string ArticlePriceBulkImportInvalidFile = "ARTICLE_PRICE_BULK_IMPORT_INVALID_FILE";
        public const string ArticlePriceBulkImportTooManyRows = "ARTICLE_PRICE_BULK_IMPORT_TOO_MANY_ROWS";
        public const string ArticlePriceBulkImportRowInvalid = "ARTICLE_PRICE_BULK_IMPORT_ROW_INVALID";
        public const string ArticlePriceBulkImportRowFailed = "ARTICLE_PRICE_BULK_IMPORT_ROW_FAILED";

        // ArticleFavorite
        public const string ArticleFavoriteNotFound = "ARTICLE_FAVORITE_NOT_FOUND";
        public const string ArticleFavoriteCreateFailed = "ARTICLE_FAVORITE_CREATE_FAILED";
        public const string ArticleFavoriteArticleReplaced = "ARTICLE_FAVORITE_ARTICLE_REPLACED";
        public const string ArticleFavoriteNoOrganizationContext = "ARTICLE_FAVORITE_NO_ORGANIZATION_CONTEXT";
        public const string ArticleFavoriteForbidden = "ARTICLE_FAVORITE_FORBIDDEN";

        // ArticleClassification
        public const string ArticleClassificationNotFound = "ARTICLE_CLASSIFICATION_NOT_FOUND";
        public const string ArticleClassificationCreateFailed = "ARTICLE_CLASSIFICATION_CREATE_FAILED";
        public const string ArticleClassificationForbidden = "ARTICLE_CLASSIFICATION_FORBIDDEN";
        public const string ArticleClassificationCategoryNotFound = "ARTICLE_CLASSIFICATION_CATEGORY_NOT_FOUND";
        public const string ArticleClassificationSubCategoryMismatch = "ARTICLE_CLASSIFICATION_SUB_CATEGORY_MISMATCH";
        public const string ArticleClassificationOutsideScope = "ARTICLE_CLASSIFICATION_OUTSIDE_SCOPE";

        // Family
        public const string FamilyNotFound = "FAMILY_NOT_FOUND";
        public const string FamilyCodeExists = "FAMILY_CODE_EXISTS";
        public const string FamilyCreateFailed = "FAMILY_CREATE_FAILED";
        public const string FamilyBulkImportForbidden = "FAMILY_BULK_IMPORT_FORBIDDEN";
        public const string FamilyBulkImportInvalidFile = "FAMILY_BULK_IMPORT_INVALID_FILE";
        public const string FamilyBulkImportTooManyRows = "FAMILY_BULK_IMPORT_TOO_MANY_ROWS";
        public const string FamilyBulkImportRowInvalid = "FAMILY_BULK_IMPORT_ROW_INVALID";
        public const string FamilyBulkImportRowFailed = "FAMILY_BULK_IMPORT_ROW_FAILED";

        // SubFamily
        public const string SubFamilyNotFound = "SUB_FAMILY_NOT_FOUND";
        public const string SubFamilyCodeExists = "SUB_FAMILY_CODE_EXISTS";
        public const string SubFamilyCreateFailed = "SUB_FAMILY_CREATE_FAILED";
        public const string SubFamilyBulkImportForbidden = "SUB_FAMILY_BULK_IMPORT_FORBIDDEN";
        public const string SubFamilyBulkImportInvalidFile = "SUB_FAMILY_BULK_IMPORT_INVALID_FILE";
        public const string SubFamilyBulkImportTooManyRows = "SUB_FAMILY_BULK_IMPORT_TOO_MANY_ROWS";
        public const string SubFamilyBulkImportRowInvalid = "SUB_FAMILY_BULK_IMPORT_ROW_INVALID";
        public const string SubFamilyBulkImportRowFailed = "SUB_FAMILY_BULK_IMPORT_ROW_FAILED";

        // Category
        public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
        public const string CategoryCodeExists = "CATEGORY_CODE_EXISTS";
        public const string CategoryCreateFailed = "CATEGORY_CREATE_FAILED";
        public const string CategoryBulkImportForbidden = "CATEGORY_BULK_IMPORT_FORBIDDEN";
        public const string CategoryBulkImportInvalidFile = "CATEGORY_BULK_IMPORT_INVALID_FILE";
        public const string CategoryBulkImportTooManyRows = "CATEGORY_BULK_IMPORT_TOO_MANY_ROWS";
        public const string CategoryBulkImportRowInvalid = "CATEGORY_BULK_IMPORT_ROW_INVALID";
        public const string CategoryBulkImportRowFailed = "CATEGORY_BULK_IMPORT_ROW_FAILED";
        public const string CategoryCreateForbidden = "CATEGORY_CREATE_FORBIDDEN";
        public const string CategoryOutsideScope = "CATEGORY_OUTSIDE_SCOPE";
        public const string CategoryOrganizationNotFound = "CATEGORY_ORGANIZATION_NOT_FOUND";

        // SubCategory
        public const string SubCategoryNotFound = "SUB_CATEGORY_NOT_FOUND";
        public const string SubCategoryCodeExists = "SUB_CATEGORY_CODE_EXISTS";
        public const string SubCategoryCreateFailed = "SUB_CATEGORY_CREATE_FAILED";
        public const string SubCategoryBulkImportForbidden = "SUB_CATEGORY_BULK_IMPORT_FORBIDDEN";
        public const string SubCategoryBulkImportInvalidFile = "SUB_CATEGORY_BULK_IMPORT_INVALID_FILE";
        public const string SubCategoryBulkImportTooManyRows = "SUB_CATEGORY_BULK_IMPORT_TOO_MANY_ROWS";
        public const string SubCategoryBulkImportRowInvalid = "SUB_CATEGORY_BULK_IMPORT_ROW_INVALID";
        public const string SubCategoryBulkImportRowFailed = "SUB_CATEGORY_BULK_IMPORT_ROW_FAILED";
        public const string SubCategoryOutsideScope = "SUB_CATEGORY_OUTSIDE_SCOPE";

        // UnitType
        public const string UnitTypeNotFound = "UNIT_TYPE_NOT_FOUND";
        public const string UnitTypeCodeExists = "UNIT_TYPE_CODE_EXISTS";
        public const string UnitTypeCreateFailed = "UNIT_TYPE_CREATE_FAILED";

        // UnitOfMeasure
        public const string UnitOfMeasureNotFound = "UNIT_OF_MEASURE_NOT_FOUND";
        public const string UnitOfMeasureCodeExists = "UNIT_OF_MEASURE_CODE_EXISTS";
        public const string UnitOfMeasureCreateFailed = "UNIT_OF_MEASURE_CREATE_FAILED";
        public const string PurchaseUnitNotFound = "PURCHASE_UNIT_NOT_FOUND";
        public const string ContentUnitNotFound = "CONTENT_UNIT_NOT_FOUND";
        public const string BaseUnitNotFound = "BASE_UNIT_NOT_FOUND";
        public const string PurchaseUnitInvalidType = "PURCHASE_UNIT_INVALID_TYPE";
        public const string ContentUnitInvalidType = "CONTENT_UNIT_INVALID_TYPE";
        public const string BaseUnitTypeMismatch = "BASE_UNIT_TYPE_MISMATCH";

        // UnitConversionRate
        public const string UnitConversionRateNotFound = "UNIT_CONVERSION_RATE_NOT_FOUND";
        public const string UnitConversionRateCreateFailed = "UNIT_CONVERSION_RATE_CREATE_FAILED";

        // OrganizationContact
        public const string OrganizationContactNotFound = "ORGANIZATION_CONTACT_NOT_FOUND";
        public const string OrganizationContactCreateFailed = "ORGANIZATION_CONTACT_CREATE_FAILED";
        public const string OrganizationContactOutsideScope = "ORGANIZATION_CONTACT_OUTSIDE_SCOPE";

        // Warehouse
        public const string WarehouseNotFound = "WAREHOUSE_NOT_FOUND";
        public const string WarehouseCreateFailed = "WAREHOUSE_CREATE_FAILED";
        public const string WarehouseOrganizationNotFound = "WAREHOUSE_ORGANIZATION_NOT_FOUND";
        public const string WarehouseNameExists = "WAREHOUSE_NAME_EXISTS";
        public const string WarehouseOutsideScope = "WAREHOUSE_OUTSIDE_SCOPE";
        public const string WarehouseForbidden = "WAREHOUSE_FORBIDDEN";
        public const string WarehouseDefaultReceivingConflict = "WAREHOUSE_DEFAULT_RECEIVING_CONFLICT";
        public const string WarehouseDefaultConsumptionConflict = "WAREHOUSE_DEFAULT_CONSUMPTION_CONFLICT";
        public const string WarehouseMainConflict = "WAREHOUSE_MAIN_CONFLICT";

        // WarehouseContact
        public const string WarehouseContactNotFound = "WAREHOUSE_CONTACT_NOT_FOUND";
        public const string WarehouseContactCreateFailed = "WAREHOUSE_CONTACT_CREATE_FAILED";
        public const string WarehouseContactOutsideScope = "WAREHOUSE_CONTACT_OUTSIDE_SCOPE";
        public const string WarehouseContactAccessSuperadminOnly = "WAREHOUSE_CONTACT_ACCESS_SUPERADMIN_ONLY";
        public const string WarehouseContactLoginCredentialsRequired = "WAREHOUSE_CONTACT_LOGIN_CREDENTIALS_REQUIRED";
        public const string WarehouseContactLoginEmailExists = "WAREHOUSE_CONTACT_LOGIN_EMAIL_EXISTS";

        // Order
        public const string OrderNotFound = "ORDER_NOT_FOUND";
        public const string OrderWarehouseNotFound = "ORDER_WAREHOUSE_NOT_FOUND";
        public const string OrderForbidden = "ORDER_FORBIDDEN";
        public const string OrderNotDraft = "ORDER_NOT_DRAFT";
        public const string OrderEmpty = "ORDER_EMPTY";
        public const string OrderImportLinesInvalidFile = "ORDER_IMPORT_LINES_INVALID_FILE";
        public const string OrderImportLinesTooManyRows = "ORDER_IMPORT_LINES_TOO_MANY_ROWS";
        public const string OrderImportLinesRowInvalid = "ORDER_IMPORT_LINES_ROW_INVALID";
        public const string OrderNotCancellable = "ORDER_NOT_CANCELLABLE";
        public const string OrderCopyInvalidSourceStatus = "ORDER_COPY_INVALID_SOURCE_STATUS";

        // FamilyApprovalThreshold
        public const string FamilyApprovalThresholdNotFound = "FAMILY_APPROVAL_THRESHOLD_NOT_FOUND";
        public const string FamilyApprovalThresholdForbidden = "FAMILY_APPROVAL_THRESHOLD_FORBIDDEN";
        public const string FamilyApprovalThresholdOutsideScope = "FAMILY_APPROVAL_THRESHOLD_OUTSIDE_SCOPE";
        public const string FamilyApprovalThresholdInvalidLevel = "FAMILY_APPROVAL_THRESHOLD_INVALID_LEVEL";
        public const string FamilyApprovalThresholdInvalidAmount = "FAMILY_APPROVAL_THRESHOLD_INVALID_AMOUNT";
        public const string FamilyApprovalThresholdLevelExists = "FAMILY_APPROVAL_THRESHOLD_LEVEL_EXISTS";
        public const string FamilyApprovalThresholdApproverNotFound = "FAMILY_APPROVAL_THRESHOLD_APPROVER_NOT_FOUND";
        public const string FamilyApprovalThresholdApproverOutsideHierarchy = "FAMILY_APPROVAL_THRESHOLD_APPROVER_OUTSIDE_HIERARCHY";
        public const string FamilyApprovalThresholdOrganizationNotAssociate = "FAMILY_APPROVAL_THRESHOLD_ORGANIZATION_NOT_ASSOCIATE";

        // OrderApprovalStep
        public const string OrderApprovalStepNotFound = "ORDER_APPROVAL_STEP_NOT_FOUND";
        public const string OrderApprovalStepForbidden = "ORDER_APPROVAL_STEP_FORBIDDEN";
        public const string OrderApprovalStepAlreadyDecided = "ORDER_APPROVAL_STEP_ALREADY_DECIDED";
        public const string OrderApprovalStepPriorLevelPending = "ORDER_APPROVAL_STEP_PRIOR_LEVEL_PENDING";

        // OrderTemplate
        public const string OrderTemplateNotFound = "ORDER_TEMPLATE_NOT_FOUND";
        public const string OrderTemplateLineNotFound = "ORDER_TEMPLATE_LINE_NOT_FOUND";
        public const string OrderTemplateForbidden = "ORDER_TEMPLATE_FORBIDDEN";
        public const string OrderTemplateNoOrganizationContext = "ORDER_TEMPLATE_NO_ORGANIZATION_CONTEXT";
        public const string OrderTemplateWarehouseNotFound = "ORDER_TEMPLATE_WAREHOUSE_NOT_FOUND";

        // PurchaseOrder
        public const string PurchaseOrderNotFound = "PURCHASE_ORDER_NOT_FOUND";
        public const string PurchaseOrderForbidden = "PURCHASE_ORDER_FORBIDDEN";
        public const string PurchaseOrderNotSent = "PURCHASE_ORDER_NOT_SENT";

        // Role
        public const string RoleNotFound = "ROLE_NOT_FOUND";

        // Country
        public const string CountryNotFound = "COUNTRY_NOT_FOUND";

        // Zone
        public const string ZoneNotFound = "ZONE_NOT_FOUND";
        public const string ZoneCodeExists = "ZONE_CODE_EXISTS";
        public const string ZoneForbidden = "ZONE_FORBIDDEN";
        public const string ZoneCreateFailed = "ZONE_CREATE_FAILED";

        // SupplierDeliveryZone
        public const string SupplierDeliveryZoneNoSupplierContext = "SUPPLIER_DELIVERY_ZONE_NO_SUPPLIER_CONTEXT";
        public const string SupplierDeliveryZoneForbidden = "SUPPLIER_DELIVERY_ZONE_FORBIDDEN";
        public const string SupplierDeliveryZoneInvalidDayOfWeek = "SUPPLIER_DELIVERY_ZONE_INVALID_DAY_OF_WEEK";
        public const string SupplierDeliveryZoneNotFound = "SUPPLIER_DELIVERY_ZONE_NOT_FOUND";

        // Generic / cross-cutting
        public const string UnhandledError = "UNHANDLED_ERROR";
        public const string InvalidRequest = "INVALID_REQUEST";
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
        public const string IdempotencyRequestInProgress = "IDEMPOTENCY_REQUEST_IN_PROGRESS";
        public const string IdempotencyKeyPayloadMismatch = "IDEMPOTENCY_KEY_PAYLOAD_MISMATCH";
    }
}
