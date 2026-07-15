namespace InnNou.Shared.Localization;

public static class BulkExcelLocalization
{
    private static readonly HashSet<string> SupportedLanguages = ["en", "es", "ca"];

    private static readonly Dictionary<string, Dictionary<string, string>> Headers = new()
    {
        ["FirstName"] = new() { ["en"] = "First Name", ["es"] = "Nombre", ["ca"] = "Nom" },
        ["LastName"] = new() { ["en"] = "Last Name", ["es"] = "Apellido", ["ca"] = "Cognom" },
        ["Email"] = new() { ["en"] = "Email", ["es"] = "Correo electrónico", ["ca"] = "Correu electrònic" },
        ["UserName"] = new() { ["en"] = "Username", ["es"] = "Nombre de usuario", ["ca"] = "Nom d'usuari" },
        ["Password"] = new() { ["en"] = "Password", ["es"] = "Contraseña", ["ca"] = "Contrasenya" },
        ["RoleName"] = new() { ["en"] = "Role", ["es"] = "Rol", ["ca"] = "Rol" },
        ["OrganizationName"] = new() { ["en"] = "Organization", ["es"] = "Organización", ["ca"] = "Organització" },
        ["Status"] = new() { ["en"] = "Status", ["es"] = "Estado", ["ca"] = "Estat" },
        ["Name"] = new() { ["en"] = "Name", ["es"] = "Nombre", ["ca"] = "Nom" },
        ["LegalName"] = new() { ["en"] = "Legal Name", ["es"] = "Razón social", ["ca"] = "Raó social" },
        ["TaxId"] = new() { ["en"] = "Tax ID", ["es"] = "NIF/CIF", ["ca"] = "NIF/CIF" },
        ["Phone"] = new() { ["en"] = "Phone", ["es"] = "Teléfono", ["ca"] = "Telèfon" },
        ["AddressLine1"] = new() { ["en"] = "Address Line 1", ["es"] = "Dirección línea 1", ["ca"] = "Adreça línia 1" },
        ["AddressLine2"] = new() { ["en"] = "Address Line 2", ["es"] = "Dirección línea 2", ["ca"] = "Adreça línia 2" },
        ["City"] = new() { ["en"] = "City", ["es"] = "Ciudad", ["ca"] = "Ciutat" },
        ["State"] = new() { ["en"] = "State", ["es"] = "Provincia", ["ca"] = "Província" },
        ["PostalCode"] = new() { ["en"] = "Postal Code", ["es"] = "Código postal", ["ca"] = "Codi postal" },
        ["Country"] = new() { ["en"] = "Country", ["es"] = "País", ["ca"] = "País" },
        ["IsGlobal"] = new() { ["en"] = "Is Global", ["es"] = "Es global", ["ca"] = "És global" },
        ["SupplierType"] = new() { ["en"] = "Supplier Type", ["es"] = "Tipo de proveedor", ["ca"] = "Tipus de proveïdor" },
        ["Code"] = new() { ["en"] = "Code", ["es"] = "Código", ["ca"] = "Codi" },
        ["ParentOrganizationName"] = new() { ["en"] = "Parent Organization", ["es"] = "Organización padre", ["ca"] = "Organització pare" },
        ["TimeZone"] = new() { ["en"] = "Time Zone", ["es"] = "Zona horaria", ["ca"] = "Fus horari" },
        ["CurrencyCode"] = new() { ["en"] = "Currency", ["es"] = "Moneda", ["ca"] = "Moneda" },
        ["LanguageCode"] = new() { ["en"] = "Language", ["es"] = "Idioma", ["ca"] = "Idioma" },
        ["OrganizationTypeCode"] = new() { ["en"] = "Organization Type", ["es"] = "Tipo de organización", ["ca"] = "Tipus d'organització" },
        ["SupplierName"] = new() { ["en"] = "Supplier", ["es"] = "Proveedor", ["ca"] = "Proveïdor" },
        ["SupplierSku"] = new() { ["en"] = "Supplier SKU", ["es"] = "SKU del proveedor", ["ca"] = "SKU del proveïdor" },
        ["Description"] = new() { ["en"] = "Description", ["es"] = "Descripción", ["ca"] = "Descripció" },
        ["Barcode"] = new() { ["en"] = "Barcode", ["es"] = "Código de barras", ["ca"] = "Codi de barres" },
        ["Brand"] = new() { ["en"] = "Brand", ["es"] = "Marca", ["ca"] = "Marca" },
        ["FamilyCode"] = new() { ["en"] = "Family Code", ["es"] = "Código de familia", ["ca"] = "Codi de família" },
        ["SubFamilyCode"] = new() { ["en"] = "Sub-Family Code", ["es"] = "Código de subfamilia", ["ca"] = "Codi de subfamília" },
        ["PurchaseUnitCode"] = new() { ["en"] = "Purchase Unit", ["es"] = "Unidad de compra", ["ca"] = "Unitat de compra" },
        ["PurchaseQuantity"] = new() { ["en"] = "Purchase Quantity", ["es"] = "Cantidad de compra", ["ca"] = "Quantitat de compra" },
        ["ContentUnitCode"] = new() { ["en"] = "Content Unit", ["es"] = "Unidad de contenido", ["ca"] = "Unitat de contingut" },
        ["ContentQuantity"] = new() { ["en"] = "Content Quantity", ["es"] = "Cantidad de contenido", ["ca"] = "Quantitat de contingut" },
        ["BaseUnitCode"] = new() { ["en"] = "Base Unit", ["es"] = "Unidad base", ["ca"] = "Unitat base" },
        ["MinimumOrderQty"] = new() { ["en"] = "Minimum Order Qty", ["es"] = "Cantidad mínima de pedido", ["ca"] = "Quantitat mínima de comanda" },
        ["LeadTimeDays"] = new() { ["en"] = "Lead Time (Days)", ["es"] = "Plazo de entrega (días)", ["ca"] = "Termini de lliurament (dies)" },
        ["UnitType"] = new() { ["en"] = "Unit Type", ["es"] = "Tipo de unidad", ["ca"] = "Tipus d'unitat" },
        ["ArticleName"] = new() { ["en"] = "Article", ["es"] = "Artículo", ["ca"] = "Article" },
        ["Price"] = new() { ["en"] = "Price", ["es"] = "Precio", ["ca"] = "Preu" },
        ["EffectiveDate"] = new() { ["en"] = "Effective Date", ["es"] = "Fecha de vigencia", ["ca"] = "Data de vigència" },
        ["Notes"] = new() { ["en"] = "Notes", ["es"] = "Notas", ["ca"] = "Notes" },
        ["CreatedUtc"] = new() { ["en"] = "Created At", ["es"] = "Fecha de creación", ["ca"] = "Data de creació" },
        ["CategoryCode"] = new() { ["en"] = "Category Code", ["es"] = "Código de categoría", ["ca"] = "Codi de categoria" },
        ["ArticleToken"] = new() { ["en"] = "Article Token (do not edit)", ["es"] = "Token de artículo (no editar)", ["ca"] = "Token d'article (no editar)" },
    };

    public static string NormalizeLanguage(string? languageCode)
    {
        var lang = languageCode?.Trim().ToLowerInvariant();
        return lang is not null && SupportedLanguages.Contains(lang) ? lang : "en";
    }

    public static string Header(string key, string? languageCode)
    {
        var lang = NormalizeLanguage(languageCode);
        if (!Headers.TryGetValue(key, out var byLang))
            return key;

        return byLang.TryGetValue(lang, out var value) ? value : byLang["en"];
    }
}
