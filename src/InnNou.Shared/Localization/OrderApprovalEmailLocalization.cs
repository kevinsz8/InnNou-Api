namespace InnNou.Shared.Localization;

// Same lightweight static-dictionary-lookup shape as OrderConfirmationLocalization/
// BulkExcelLocalization, for the "your turn to approve" email — kept as a separate class since
// it's a distinct concern (the approval-request email, not the order-confirmation email).
public static class OrderApprovalEmailLocalization
{
    private static readonly HashSet<string> SupportedLanguages = ["en", "es", "ca"];

    private static readonly Dictionary<string, Dictionary<string, string>> Labels = new()
    {
        ["ApprovalNeededHeading"] = new() { ["en"] = "Approval needed", ["es"] = "Aprobación requerida", ["ca"] = "Aprovació requerida" },
        ["OrganizationLabel"] = new() { ["en"] = "Organization", ["es"] = "Organización", ["ca"] = "Organització" },
        ["WarehouseLabel"] = new() { ["en"] = "Warehouse", ["es"] = "Almacén", ["ca"] = "Magatzem" },
        ["OrderLabel"] = new() { ["en"] = "Order #", ["es"] = "Pedido n.°", ["ca"] = "Comanda núm." },
        ["FamilyLabel"] = new() { ["en"] = "Family", ["es"] = "Familia", ["ca"] = "Família" },
        ["LevelLabel"] = new() { ["en"] = "Level", ["es"] = "Nivel", ["ca"] = "Nivell" },
        ["ThresholdLabel"] = new() { ["en"] = "Threshold", ["es"] = "Límite", ["ca"] = "Límit" },
        ["ActualAmountLabel"] = new() { ["en"] = "This order's amount", ["es"] = "Monto de este pedido", ["ca"] = "Import d'aquesta comanda" },
        ["ApproveButton"] = new() { ["en"] = "Approve", ["es"] = "Aprobar", ["ca"] = "Aprovar" },
        ["IntroText"] = new()
        {
            ["en"] = "An order requires your approval before it can proceed. Review the details below and click Approve to sign off.",
            ["es"] = "Un pedido requiere tu aprobación antes de continuar. Revisá los detalles y hacé clic en Aprobar para confirmar.",
            ["ca"] = "Una comanda requereix la teva aprovació abans de continuar. Revisa els detalls i fes clic a Aprovar per confirmar."
        },
        ["SignInInsteadNote"] = new()
        {
            ["en"] = "Need to reject this or something looks wrong? Sign in to the system instead.",
            ["es"] = "¿Necesitás rechazarlo o algo no se ve bien? Iniciá sesión en el sistema en su lugar.",
            ["ca"] = "Necessites rebutjar-la o alguna cosa no sembla correcta? Inicia sessió al sistema."
        },
        ["SignInLinkText"] = new() { ["en"] = "Sign in", ["es"] = "Iniciar sesión", ["ca"] = "Iniciar sessió" },
        ["LinkExpiresNote"] = new()
        {
            ["en"] = "This link is single-use and expires in 7 days.",
            ["es"] = "Este enlace es de un solo uso y vence en 7 días.",
            ["ca"] = "Aquest enllaç és d'un sol ús i caduca en 7 dies."
        },
    };

    public static string NormalizeLanguage(string? languageCode)
    {
        var lang = languageCode?.Trim().ToLowerInvariant();
        return lang is not null && SupportedLanguages.Contains(lang) ? lang : "en";
    }

    public static string Label(string key, string? languageCode)
    {
        var lang = NormalizeLanguage(languageCode);
        if (!Labels.TryGetValue(key, out var byLang))
            return key;

        return byLang.TryGetValue(lang, out var value) ? value : byLang["en"];
    }
}
