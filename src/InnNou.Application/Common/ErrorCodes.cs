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

        // Article
        public const string ArticleNotFound = "ARTICLE_NOT_FOUND";
        public const string ArticleCreateFailed = "ARTICLE_CREATE_FAILED";
        public const string ArticleSkuExists = "ARTICLE_SKU_EXISTS";
        public const string ArticleAlreadyReplaced = "ARTICLE_ALREADY_REPLACED";
        public const string ArticleSupersedeFailed = "ARTICLE_SUPERSEDE_FAILED";
        public const string ArticleStructuralChangeNotAllowed = "ARTICLE_STRUCTURAL_CHANGE_NOT_ALLOWED";
        public const string NoStructuralChange = "NO_STRUCTURAL_CHANGE";
        public const string ArticleSupplierForbidden = "ARTICLE_SUPPLIER_FORBIDDEN";

        // ArticlePrice
        public const string ArticlePriceNotFound = "ARTICLE_PRICE_NOT_FOUND";
        public const string ArticlePriceCreateFailed = "ARTICLE_PRICE_CREATE_FAILED";
        public const string ArticlePriceSupplierForbidden = "ARTICLE_PRICE_SUPPLIER_FORBIDDEN";
        public const string ArticlePriceArticleReplaced = "ARTICLE_PRICE_ARTICLE_REPLACED";
        public const string ArticlePriceInvalidCurrency = "ARTICLE_PRICE_INVALID_CURRENCY";
        public const string ArticlePriceInvalidAmount = "ARTICLE_PRICE_INVALID_AMOUNT";
        public const string ArticlePriceDuplicateEffectiveDate = "ARTICLE_PRICE_DUPLICATE_EFFECTIVE_DATE";
        public const string ArticlePriceCurrencyRequired = "ARTICLE_PRICE_CURRENCY_REQUIRED";

        // Family
        public const string FamilyNotFound = "FAMILY_NOT_FOUND";
        public const string FamilyCodeExists = "FAMILY_CODE_EXISTS";
        public const string FamilyCreateFailed = "FAMILY_CREATE_FAILED";

        // SubFamily
        public const string SubFamilyNotFound = "SUB_FAMILY_NOT_FOUND";
        public const string SubFamilyCodeExists = "SUB_FAMILY_CODE_EXISTS";
        public const string SubFamilyCreateFailed = "SUB_FAMILY_CREATE_FAILED";

        // Category
        public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
        public const string CategoryCodeExists = "CATEGORY_CODE_EXISTS";
        public const string CategoryCreateFailed = "CATEGORY_CREATE_FAILED";

        // SubCategory
        public const string SubCategoryNotFound = "SUB_CATEGORY_NOT_FOUND";
        public const string SubCategoryCodeExists = "SUB_CATEGORY_CODE_EXISTS";
        public const string SubCategoryCreateFailed = "SUB_CATEGORY_CREATE_FAILED";

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

        // Role
        public const string RoleNotFound = "ROLE_NOT_FOUND";

        // Generic / cross-cutting
        public const string UnhandledError = "UNHANDLED_ERROR";
        public const string InvalidRequest = "INVALID_REQUEST";
    }
}
