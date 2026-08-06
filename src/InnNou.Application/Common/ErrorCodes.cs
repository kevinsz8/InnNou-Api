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
        public const string UserInvalidEmail = "USER_INVALID_EMAIL";
        public const string UserWeakPassword = "USER_WEAK_PASSWORD";
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
        public const string SupplierLogoInvalidFile = "SUPPLIER_LOGO_INVALID_FILE";
        public const string SupplierLogoTooLarge = "SUPPLIER_LOGO_TOO_LARGE";

        // Article
        public const string ArticleNameRequired = "ARTICLE_NAME_REQUIRED";
        public const string ArticleInvalidMinimumOrderQty = "ARTICLE_INVALID_MINIMUM_ORDER_QTY";
        public const string ArticleInvalidLeadTimeDays = "ARTICLE_INVALID_LEAD_TIME_DAYS";
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

        // SupplierPriceChangeSubscription
        public const string SupplierPriceSubscriptionTooManySuppliers = "SUPPLIER_PRICE_SUBSCRIPTION_TOO_MANY_SUPPLIERS";

        // ArticleClassification
        public const string ArticleClassificationNotFound = "ARTICLE_CLASSIFICATION_NOT_FOUND";
        public const string ArticleClassificationCreateFailed = "ARTICLE_CLASSIFICATION_CREATE_FAILED";
        public const string ArticleClassificationForbidden = "ARTICLE_CLASSIFICATION_FORBIDDEN";
        public const string ArticleClassificationCategoryNotFound = "ARTICLE_CLASSIFICATION_CATEGORY_NOT_FOUND";
        public const string ArticleClassificationSubCategoryMismatch = "ARTICLE_CLASSIFICATION_SUB_CATEGORY_MISMATCH";
        public const string ArticleClassificationOutsideScope = "ARTICLE_CLASSIFICATION_OUTSIDE_SCOPE";

        // Family
        public const string FamilyForbidden = "FAMILY_FORBIDDEN";
        public const string FamilyNotFound = "FAMILY_NOT_FOUND";
        public const string FamilyCodeExists = "FAMILY_CODE_EXISTS";
        public const string FamilyCreateFailed = "FAMILY_CREATE_FAILED";
        public const string FamilyBulkImportForbidden = "FAMILY_BULK_IMPORT_FORBIDDEN";
        public const string FamilyBulkImportInvalidFile = "FAMILY_BULK_IMPORT_INVALID_FILE";
        public const string FamilyBulkImportTooManyRows = "FAMILY_BULK_IMPORT_TOO_MANY_ROWS";
        public const string FamilyBulkImportRowInvalid = "FAMILY_BULK_IMPORT_ROW_INVALID";
        public const string FamilyBulkImportRowFailed = "FAMILY_BULK_IMPORT_ROW_FAILED";
        public const string FamilySystemReadonly = "FAMILY_SYSTEM_READONLY";

        // SubFamily
        public const string SubFamilyForbidden = "SUB_FAMILY_FORBIDDEN";
        public const string SubFamilyNotFound = "SUB_FAMILY_NOT_FOUND";
        public const string SubFamilyCodeExists = "SUB_FAMILY_CODE_EXISTS";
        public const string SubFamilyCreateFailed = "SUB_FAMILY_CREATE_FAILED";
        public const string SubFamilyBulkImportForbidden = "SUB_FAMILY_BULK_IMPORT_FORBIDDEN";
        public const string SubFamilyBulkImportInvalidFile = "SUB_FAMILY_BULK_IMPORT_INVALID_FILE";
        public const string SubFamilyBulkImportTooManyRows = "SUB_FAMILY_BULK_IMPORT_TOO_MANY_ROWS";
        public const string SubFamilyBulkImportRowInvalid = "SUB_FAMILY_BULK_IMPORT_ROW_INVALID";
        public const string SubFamilyBulkImportRowFailed = "SUB_FAMILY_BULK_IMPORT_ROW_FAILED";
        public const string SubFamilySystemReadonly = "SUB_FAMILY_SYSTEM_READONLY";

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
        public const string CategorySystemReadonly = "CATEGORY_SYSTEM_READONLY";

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
        public const string SubCategorySystemReadonly = "SUB_CATEGORY_SYSTEM_READONLY";

        // UnitType
        public const string UnitTypeForbidden = "UNIT_TYPE_FORBIDDEN";
        public const string UnitTypeNotFound = "UNIT_TYPE_NOT_FOUND";
        public const string UnitTypeCodeExists = "UNIT_TYPE_CODE_EXISTS";
        public const string UnitTypeCreateFailed = "UNIT_TYPE_CREATE_FAILED";
        public const string UnitTypeSystemReadonly = "UNIT_TYPE_SYSTEM_READONLY";

        // UnitOfMeasure
        public const string UnitOfMeasureForbidden = "UNIT_OF_MEASURE_FORBIDDEN";
        public const string UnitOfMeasureNotFound = "UNIT_OF_MEASURE_NOT_FOUND";
        public const string UnitOfMeasureCodeExists = "UNIT_OF_MEASURE_CODE_EXISTS";
        public const string UnitOfMeasureCreateFailed = "UNIT_OF_MEASURE_CREATE_FAILED";
        public const string UnitOfMeasureSystemReadonly = "UNIT_OF_MEASURE_SYSTEM_READONLY";
        public const string PurchaseUnitNotFound = "PURCHASE_UNIT_NOT_FOUND";
        public const string ContentUnitNotFound = "CONTENT_UNIT_NOT_FOUND";
        public const string BaseUnitNotFound = "BASE_UNIT_NOT_FOUND";
        public const string PurchaseUnitInvalidType = "PURCHASE_UNIT_INVALID_TYPE";
        public const string ContentUnitInvalidType = "CONTENT_UNIT_INVALID_TYPE";
        public const string BaseUnitTypeMismatch = "BASE_UNIT_TYPE_MISMATCH";

        // UnitConversionRate
        public const string UnitConversionRateForbidden = "UNIT_CONVERSION_RATE_FORBIDDEN";
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
        public const string WarehouseInvalidZone = "WAREHOUSE_INVALID_ZONE";

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
        public const string OrderPdfNotAvailable = "ORDER_PDF_NOT_AVAILABLE";

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

        // OrderApprovalEmailToken — anonymous single-use email-approval link
        public const string OrderApprovalEmailTokenNotFound = "ORDER_APPROVAL_EMAIL_TOKEN_NOT_FOUND";
        public const string OrderApprovalEmailTokenExpired = "ORDER_APPROVAL_EMAIL_TOKEN_EXPIRED";
        public const string OrderApprovalEmailTokenAlreadyUsed = "ORDER_APPROVAL_EMAIL_TOKEN_ALREADY_USED";
        public const string OrderApprovalEmailTokenStepAlreadyDecided = "ORDER_APPROVAL_EMAIL_TOKEN_STEP_ALREADY_DECIDED";
        public const string OrderApprovalEmailTokenPriorLevelPending = "ORDER_APPROVAL_EMAIL_TOKEN_PRIOR_LEVEL_PENDING";

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
        public const string PurchaseOrderCloseShortNotAllowed = "PURCHASE_ORDER_CLOSE_SHORT_NOT_ALLOWED";
        public const string PurchaseOrderCloseShortReasonRequired = "PURCHASE_ORDER_CLOSE_SHORT_REASON_REQUIRED";

        // PurchaseOrderRectification ("rectificacion de pedido")
        public const string PurchaseOrderLineNotFound = "PURCHASE_ORDER_LINE_NOT_FOUND";
        public const string PurchaseOrderLineAlreadyCancelled = "PURCHASE_ORDER_LINE_ALREADY_CANCELLED";
        public const string PurchaseOrderRectificationNotFound = "PURCHASE_ORDER_RECTIFICATION_NOT_FOUND";
        public const string PurchaseOrderRectificationEmpty = "PURCHASE_ORDER_RECTIFICATION_EMPTY";
        public const string PurchaseOrderRectificationInvalidQuantity = "PURCHASE_ORDER_RECTIFICATION_INVALID_QUANTITY";
        public const string PurchaseOrderRectificationInvalidStatus = "PURCHASE_ORDER_RECTIFICATION_INVALID_STATUS";
        public const string PurchaseOrderRectificationBelowAccepted = "PURCHASE_ORDER_RECTIFICATION_BELOW_ACCEPTED";
        public const string PurchaseOrderRectificationNewLineSupplierMismatch = "PURCHASE_ORDER_RECTIFICATION_NEW_LINE_SUPPLIER_MISMATCH";
        public const string PurchaseOrderRectificationNewLineAlreadyOnOrder = "PURCHASE_ORDER_RECTIFICATION_NEW_LINE_ALREADY_ON_ORDER";

        // GoodsReceipt (recepcion de mercaderia)
        public const string GoodsReceiptNotFound = "GOODS_RECEIPT_NOT_FOUND";
        public const string GoodsReceiptForbidden = "GOODS_RECEIPT_FORBIDDEN";
        public const string GoodsReceiptPurchaseOrderNotReceivable = "GOODS_RECEIPT_PURCHASE_ORDER_NOT_RECEIVABLE";
        public const string GoodsReceiptWarehouseCannotReceive = "GOODS_RECEIPT_WAREHOUSE_CANNOT_RECEIVE";
        public const string GoodsReceiptEmpty = "GOODS_RECEIPT_EMPTY";
        public const string GoodsReceiptDeliveryNoteNumberRequired = "GOODS_RECEIPT_DELIVERY_NOTE_NUMBER_REQUIRED";
        public const string GoodsReceiptLineNotFound = "GOODS_RECEIPT_LINE_NOT_FOUND";
        public const string GoodsReceiptLineEmpty = "GOODS_RECEIPT_LINE_EMPTY";
        public const string GoodsReceiptLineAlreadyCancelled = "GOODS_RECEIPT_LINE_ALREADY_CANCELLED";
        public const string GoodsReceiptOverReceiptNotAllowed = "GOODS_RECEIPT_OVER_RECEIPT_NOT_ALLOWED";
        public const string GoodsReceiptLotNumberRequired = "GOODS_RECEIPT_LOT_NUMBER_REQUIRED";
        public const string GoodsReceiptExpirationDateRequired = "GOODS_RECEIPT_EXPIRATION_DATE_REQUIRED";
        public const string GoodsReceiptSerialNumberRequired = "GOODS_RECEIPT_SERIAL_NUMBER_REQUIRED";
        public const string GoodsReceiptRejectionReasonRequired = "GOODS_RECEIPT_REJECTION_REASON_REQUIRED";

        // SupplierReturn (devolucion a proveedor / RMA)
        public const string SupplierReturnNotFound = "SUPPLIER_RETURN_NOT_FOUND";
        public const string SupplierReturnForbidden = "SUPPLIER_RETURN_FORBIDDEN";
        public const string SupplierReturnWarehouseCannotReceiveReturns = "SUPPLIER_RETURN_WAREHOUSE_CANNOT_RECEIVE_RETURNS";
        public const string SupplierReturnEmpty = "SUPPLIER_RETURN_EMPTY";
        public const string SupplierReturnLineNotEligible = "SUPPLIER_RETURN_LINE_NOT_ELIGIBLE";
        public const string SupplierReturnInvalidResolutionType = "SUPPLIER_RETURN_INVALID_RESOLUTION_TYPE";
        public const string SupplierReturnAlreadyClosed = "SUPPLIER_RETURN_ALREADY_CLOSED";

        // Inventory
        public const string InventoryForbidden = "INVENTORY_FORBIDDEN";
        public const string InventoryWarehouseNotFound = "INVENTORY_WAREHOUSE_NOT_FOUND";
        public const string InventoryWarehouseNotInventoriable = "INVENTORY_WAREHOUSE_NOT_INVENTORIABLE";
        public const string InventoryWarehouseCannotAdjust = "INVENTORY_WAREHOUSE_CANNOT_ADJUST";
        public const string InventoryWarehouseCannotTransferOut = "INVENTORY_WAREHOUSE_CANNOT_TRANSFER_OUT";
        public const string InventoryWarehouseCannotReceiveTransfers = "INVENTORY_WAREHOUSE_CANNOT_RECEIVE_TRANSFERS";
        public const string InventoryTransferSameWarehouse = "INVENTORY_TRANSFER_SAME_WAREHOUSE";
        public const string InventoryTransferCrossOrganization = "INVENTORY_TRANSFER_CROSS_ORGANIZATION";
        public const string InventoryTransferEmpty = "INVENTORY_TRANSFER_EMPTY";
        public const string InventoryTransferNotFound = "INVENTORY_TRANSFER_NOT_FOUND";
        public const string InventoryNegativeStockNotAllowed = "INVENTORY_NEGATIVE_STOCK_NOT_ALLOWED";
        public const string InventoryArticleNotFound = "INVENTORY_ARTICLE_NOT_FOUND";
        public const string InventoryInvalidAdjustment = "INVENTORY_INVALID_ADJUSTMENT";
        public const string InventoryWarehouseCountInProgress = "INVENTORY_WAREHOUSE_COUNT_IN_PROGRESS";

        // InventoryPeriod (state-machine counting periods — OPEN/IN_PROGRESS/PRE_CLOSED/CLOSED)
        public const string InventoryPeriodNotFound = "INVENTORY_PERIOD_NOT_FOUND";
        public const string InventoryPeriodAlreadyOpen = "INVENTORY_PERIOD_ALREADY_OPEN";
        public const string InventoryPeriodWarehouseCannotCount = "INVENTORY_PERIOD_WAREHOUSE_CANNOT_COUNT";
        public const string InventoryPeriodArticleNotInPeriod = "INVENTORY_PERIOD_ARTICLE_NOT_IN_PERIOD";
        public const string InventoryPeriodInvalidCount = "INVENTORY_PERIOD_INVALID_COUNT";
        public const string InventoryPeriodIncomplete = "INVENTORY_PERIOD_INCOMPLETE";
        public const string InventoryPeriodAlreadyClosed = "INVENTORY_PERIOD_ALREADY_CLOSED";
        public const string InventoryPeriodNotClosed = "INVENTORY_PERIOD_NOT_CLOSED";
        public const string InventoryPeriodNotMostRecent = "INVENTORY_PERIOD_NOT_MOST_RECENT";
        public const string InventoryPeriodReopenForbidden = "INVENTORY_PERIOD_REOPEN_FORBIDDEN";

        // ParLevel (par levels / suggested replenishment)
        public const string ParLevelNotFound = "PAR_LEVEL_NOT_FOUND";
        public const string ParLevelForbidden = "PAR_LEVEL_FORBIDDEN";
        public const string ParLevelWarehouseNotFound = "PAR_LEVEL_WAREHOUSE_NOT_FOUND";
        public const string ParLevelArticleNotFound = "PAR_LEVEL_ARTICLE_NOT_FOUND";
        public const string ParLevelAlreadyExists = "PAR_LEVEL_ALREADY_EXISTS";
        public const string ParLevelInvalidQuantity = "PAR_LEVEL_INVALID_QUANTITY";
        public const string ParLevelBaseRequired = "PAR_LEVEL_BASE_REQUIRED";
        public const string ParLevelOverrideNotFound = "PAR_LEVEL_OVERRIDE_NOT_FOUND";
        public const string ParLevelOverrideInvalidDateRange = "PAR_LEVEL_OVERRIDE_INVALID_DATE_RANGE";
        public const string ParLevelOverrideOverlap = "PAR_LEVEL_OVERRIDE_OVERLAP";

        // ConsolidatedPurchaseOrder (multi-property spend consolidation)
        public const string ConsolidatedPurchaseOrderNotFound = "CONSOLIDATED_PURCHASE_ORDER_NOT_FOUND";
        public const string ConsolidatedPurchaseOrderForbidden = "CONSOLIDATED_PURCHASE_ORDER_FORBIDDEN";
        public const string ConsolidatedPurchaseOrderOrganizationNotFound = "CONSOLIDATED_PURCHASE_ORDER_ORGANIZATION_NOT_FOUND";
        public const string ConsolidatedPurchaseOrderInvalidDateRange = "CONSOLIDATED_PURCHASE_ORDER_INVALID_DATE_RANGE";
        public const string ConsolidatedPurchaseOrderEmpty = "CONSOLIDATED_PURCHASE_ORDER_EMPTY";
        public const string ConsolidatedPurchaseOrderInvalidMember = "CONSOLIDATED_PURCHASE_ORDER_INVALID_MEMBER";

        // InternalOrder (Pedidos Internos — stock moved between two different Asociado
        // Organizations under the same Super Asociado; distinct from InventoryTransfer's own
        // same-Organization codes above)
        public const string InternalOrderNotFound = "INTERNAL_ORDER_NOT_FOUND";
        public const string InternalOrderForbidden = "INTERNAL_ORDER_FORBIDDEN";
        public const string InternalOrderSourceOrganizationNotFound = "INTERNAL_ORDER_SOURCE_ORGANIZATION_NOT_FOUND";
        public const string InternalOrderSameOrganization = "INTERNAL_ORDER_SAME_ORGANIZATION";
        public const string InternalOrderDifferentSuperAssociate = "INTERNAL_ORDER_DIFFERENT_SUPER_ASSOCIATE";
        public const string InternalOrderDestinationWarehouseNotFound = "INTERNAL_ORDER_DESTINATION_WAREHOUSE_NOT_FOUND";
        public const string InternalOrderDestinationWarehouseNotInventoriable = "INTERNAL_ORDER_DESTINATION_WAREHOUSE_NOT_INVENTORIABLE";
        public const string InternalOrderSourceWarehouseNotFound = "INTERNAL_ORDER_SOURCE_WAREHOUSE_NOT_FOUND";
        public const string InternalOrderEmpty = "INTERNAL_ORDER_EMPTY";
        public const string InternalOrderDuplicateLine = "INTERNAL_ORDER_DUPLICATE_LINE";
        public const string InternalOrderInvalidQuantity = "INTERNAL_ORDER_INVALID_QUANTITY";
        public const string InternalOrderArticleNotFound = "INTERNAL_ORDER_ARTICLE_NOT_FOUND";
        public const string InternalOrderPriceNotFound = "INTERNAL_ORDER_PRICE_NOT_FOUND";
        public const string InternalOrderNotCancellable = "INTERNAL_ORDER_NOT_CANCELLABLE";
        public const string InternalOrderNotShippable = "INTERNAL_ORDER_NOT_SHIPPABLE";
        public const string InternalOrderShipmentEmpty = "INTERNAL_ORDER_SHIPMENT_EMPTY";
        public const string InternalOrderShipmentSourceWarehouseMismatch = "INTERNAL_ORDER_SHIPMENT_SOURCE_WAREHOUSE_MISMATCH";
        public const string InternalOrderShipmentWarehouseNotInventoriable = "INTERNAL_ORDER_SHIPMENT_WAREHOUSE_NOT_INVENTORIABLE";
        public const string InternalOrderShipmentWarehouseCannotTransferOut = "INTERNAL_ORDER_SHIPMENT_WAREHOUSE_CANNOT_TRANSFER_OUT";
        public const string InternalOrderShipmentLineNotFound = "INTERNAL_ORDER_SHIPMENT_LINE_NOT_FOUND";
        public const string InternalOrderOverShipmentNotAllowed = "INTERNAL_ORDER_OVER_SHIPMENT_NOT_ALLOWED";
        public const string InternalOrderInsufficientStock = "INTERNAL_ORDER_INSUFFICIENT_STOCK";
        public const string InternalOrderNotReceivable = "INTERNAL_ORDER_NOT_RECEIVABLE";
        public const string InternalOrderReceiptEmpty = "INTERNAL_ORDER_RECEIPT_EMPTY";
        public const string InternalOrderReceiptWarehouseCannotReceive = "INTERNAL_ORDER_RECEIPT_WAREHOUSE_CANNOT_RECEIVE";
        public const string InternalOrderReceiptLineNotFound = "INTERNAL_ORDER_RECEIPT_LINE_NOT_FOUND";
        public const string InternalOrderReceiptLineEmpty = "INTERNAL_ORDER_RECEIPT_LINE_EMPTY";
        public const string InternalOrderOverReceiptNotAllowed = "INTERNAL_ORDER_OVER_RECEIPT_NOT_ALLOWED";
        public const string InternalOrderRejectionReasonRequired = "INTERNAL_ORDER_REJECTION_REASON_REQUIRED";
        public const string InternalOrderReceiptWarehouseTaxJurisdictionMissing = "INTERNAL_ORDER_RECEIPT_WAREHOUSE_TAX_JURISDICTION_MISSING";
        public const string InternalOrderReceiptArticleTaxCategoryMissing = "INTERNAL_ORDER_RECEIPT_ARTICLE_TAX_CATEGORY_MISSING";
        public const string InternalOrderReceiptTaxRateMissing = "INTERNAL_ORDER_RECEIPT_TAX_RATE_MISSING";

        // Department (per-Organization, owner of Requisitions below)
        public const string DepartmentForbidden = "DEPARTMENT_FORBIDDEN";
        public const string DepartmentOutsideScope = "DEPARTMENT_OUTSIDE_SCOPE";
        public const string DepartmentNameExists = "DEPARTMENT_NAME_EXISTS";
        public const string DepartmentOrganizationNotFound = "DEPARTMENT_ORGANIZATION_NOT_FOUND";
        public const string DepartmentNotFound = "DEPARTMENT_NOT_FOUND";

        // Requisition (Requisiciones internas — a Department pulling stock from a Warehouse for
        // internal use; the first "stock out for an operational reason, not a sale" flow)
        public const string RequisitionNotFound = "REQUISITION_NOT_FOUND";
        public const string RequisitionForbidden = "REQUISITION_FORBIDDEN";
        public const string RequisitionWarehouseNotFound = "REQUISITION_WAREHOUSE_NOT_FOUND";
        public const string RequisitionWarehouseCannotIssue = "REQUISITION_WAREHOUSE_CANNOT_ISSUE";
        public const string RequisitionDepartmentNotFound = "REQUISITION_DEPARTMENT_NOT_FOUND";
        public const string RequisitionDepartmentOrganizationMismatch = "REQUISITION_DEPARTMENT_ORGANIZATION_MISMATCH";
        public const string RequisitionArticleNotFound = "REQUISITION_ARTICLE_NOT_FOUND";
        public const string RequisitionEmpty = "REQUISITION_EMPTY";
        public const string RequisitionInvalidQuantity = "REQUISITION_INVALID_QUANTITY";
        public const string RequisitionNotEditable = "REQUISITION_NOT_EDITABLE";
        public const string RequisitionLineNotFound = "REQUISITION_LINE_NOT_FOUND";
        public const string RequisitionNotApprovable = "REQUISITION_NOT_APPROVABLE";
        public const string RequisitionNotRejectable = "REQUISITION_NOT_REJECTABLE";
        public const string RequisitionRejectReasonRequired = "REQUISITION_REJECT_REASON_REQUIRED";
        public const string RequisitionNotCancellable = "REQUISITION_NOT_CANCELLABLE";
        public const string RequisitionNotCloseableShort = "REQUISITION_NOT_CLOSEABLE_SHORT";
        public const string RequisitionCloseShortReasonRequired = "REQUISITION_CLOSE_SHORT_REASON_REQUIRED";
        public const string RequisitionNotIssuable = "REQUISITION_NOT_ISSUABLE";
        public const string RequisitionIssueEmpty = "REQUISITION_ISSUE_EMPTY";
        public const string RequisitionIssueLineNotFound = "REQUISITION_ISSUE_LINE_NOT_FOUND";
        public const string RequisitionIssueDuplicateLine = "REQUISITION_ISSUE_DUPLICATE_LINE";
        public const string RequisitionOverIssueNotAllowed = "REQUISITION_OVER_ISSUE_NOT_ALLOWED";
        public const string RequisitionInsufficientStock = "REQUISITION_INSUFFICIENT_STOCK";

        // DepartmentParLevel (Department-level reorder suggestion, feeds the Requisitions
        // "Sugeridas" tab — see .claude/RequisitionsModule.md)
        public const string DepartmentParLevelNotFound = "DEPARTMENT_PAR_LEVEL_NOT_FOUND";
        public const string DepartmentParLevelForbidden = "DEPARTMENT_PAR_LEVEL_FORBIDDEN";
        public const string DepartmentParLevelAlreadyExists = "DEPARTMENT_PAR_LEVEL_ALREADY_EXISTS";
        public const string DepartmentParLevelInvalidQuantity = "DEPARTMENT_PAR_LEVEL_INVALID_QUANTITY";

        // Unit-aware quantities (Requisitions + Inventory Adjustments/Transfers/Period Counts —
        // see InnNou.Application.Common.ArticleUnitConversion and .claude/RequisitionsModule.md)
        public const string ArticleUnitNotValidForArticle = "ARTICLE_UNIT_NOT_VALID_FOR_ARTICLE";

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

        // Tax (Families.DefaultTaxCategoryId cascade, Articles.TaxCategoryId override,
        // Warehouses.TaxJurisdictionId, GoodsReceiptLine tax snapshot)
        public const string TaxCategoryNotFound = "TAX_CATEGORY_NOT_FOUND";
        public const string TaxJurisdictionNotFound = "TAX_JURISDICTION_NOT_FOUND";
        public const string TaxRateForbidden = "TAX_RATE_FORBIDDEN";
        public const string TaxRateInvalidPercent = "TAX_RATE_INVALID_PERCENT";
        public const string TaxJurisdictionForbidden = "TAX_JURISDICTION_FORBIDDEN";
        public const string TaxJurisdictionCountryNotFound = "TAX_JURISDICTION_COUNTRY_NOT_FOUND";
        public const string TaxJurisdictionCodeAlreadyExists = "TAX_JURISDICTION_CODE_ALREADY_EXISTS";
        public const string TaxJurisdictionCodeRequired = "TAX_JURISDICTION_CODE_REQUIRED";
        public const string GoodsReceiptWarehouseTaxJurisdictionMissing = "GOODS_RECEIPT_WAREHOUSE_TAX_JURISDICTION_MISSING";
        public const string GoodsReceiptArticleTaxCategoryMissing = "GOODS_RECEIPT_ARTICLE_TAX_CATEGORY_MISSING";
        public const string GoodsReceiptTaxRateMissing = "GOODS_RECEIPT_TAX_RATE_MISSING";
        public const string TaxCategoryForbidden = "TAX_CATEGORY_FORBIDDEN";
        public const string TaxCategoryCodeRequired = "TAX_CATEGORY_CODE_REQUIRED";
        public const string TaxCategoryCodeAlreadyExists = "TAX_CATEGORY_CODE_ALREADY_EXISTS";
        public const string FamilyTaxCategoryOverrideForbidden = "FAMILY_TAX_CATEGORY_OVERRIDE_FORBIDDEN";
        public const string FamilyTaxCategoryOverrideNotFound = "FAMILY_TAX_CATEGORY_OVERRIDE_NOT_FOUND";

        // SupplierInvoice (Facturacion Phase B — 3-way matching PO<->Recepcion<->Factura)
        public const string SupplierInvoiceNotFound = "SUPPLIER_INVOICE_NOT_FOUND";
        public const string SupplierInvoiceForbidden = "SUPPLIER_INVOICE_FORBIDDEN";
        public const string SupplierInvoiceOrganizationNotFound = "SUPPLIER_INVOICE_ORGANIZATION_NOT_FOUND";
        public const string SupplierInvoiceSupplierNotFound = "SUPPLIER_INVOICE_SUPPLIER_NOT_FOUND";
        public const string SupplierInvoiceEmpty = "SUPPLIER_INVOICE_EMPTY";
        public const string SupplierInvoicePurchaseOrderNotFound = "SUPPLIER_INVOICE_PURCHASE_ORDER_NOT_FOUND";
        public const string SupplierInvoicePurchaseOrderNotReceived = "SUPPLIER_INVOICE_PURCHASE_ORDER_NOT_RECEIVED";
        public const string SupplierInvoiceGoodsReceiptNotFound = "SUPPLIER_INVOICE_GOODS_RECEIPT_NOT_FOUND";
        public const string SupplierInvoiceGoodsReceiptAlreadyInvoiced = "SUPPLIER_INVOICE_GOODS_RECEIPT_ALREADY_INVOICED";
        public const string SupplierInvoicePurchaseOrderDifferentSupplier = "SUPPLIER_INVOICE_PURCHASE_ORDER_DIFFERENT_SUPPLIER";
        public const string SupplierInvoiceLineIncomplete = "SUPPLIER_INVOICE_LINE_INCOMPLETE";
        public const string SupplierInvoiceLineInvalid = "SUPPLIER_INVOICE_LINE_INVALID";
        public const string SupplierInvoiceTaxBreakdownRequired = "SUPPLIER_INVOICE_TAX_BREAKDOWN_REQUIRED";
        public const string SupplierInvoiceTaxBreakdownInvalid = "SUPPLIER_INVOICE_TAX_BREAKDOWN_INVALID";
        public const string SupplierInvoiceToleranceForbidden = "SUPPLIER_INVOICE_TOLERANCE_FORBIDDEN";
        public const string SupplierInvoiceToleranceInvalid = "SUPPLIER_INVOICE_TOLERANCE_INVALID";
        public const string SupplierInvoiceAttachmentInvalidFile = "SUPPLIER_INVOICE_ATTACHMENT_INVALID_FILE";
        public const string SupplierInvoicePurchaseOrderPolicyForbidden = "SUPPLIER_INVOICE_PURCHASE_ORDER_POLICY_FORBIDDEN";
        public const string SupplierInvoicePurchaseOrderPolicyInvalid = "SUPPLIER_INVOICE_PURCHASE_ORDER_POLICY_INVALID";
        public const string SupplierInvoiceMultiplePurchaseOrdersNotAllowed = "SUPPLIER_INVOICE_MULTIPLE_PURCHASE_ORDERS_NOT_ALLOWED";

        // Generic / cross-cutting
        public const string UnhandledError = "UNHANDLED_ERROR";
        public const string InvalidRequest = "INVALID_REQUEST";
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
        public const string IdempotencyRequestInProgress = "IDEMPOTENCY_REQUEST_IN_PROGRESS";
        public const string IdempotencyKeyPayloadMismatch = "IDEMPOTENCY_KEY_PAYLOAD_MISMATCH";
    }
}
