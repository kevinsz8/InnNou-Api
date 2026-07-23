using InnNou.Infrastructure.Repositories.DbEntities;
using InnNou.Shared.Localization;

namespace InnNou.Infrastructure.Documents
{
    // Builds the HTML "your turn to approve" email — one prominent CTA button linking to the
    // frontend's anonymous /approve-order/:token confirmation page (see
    // .claude/OrderApprovalModule.md). Deliberately does NOT perform the approval itself; the
    // link only opens a read-only page, exactly so an email security scanner pre-visiting this
    // link can never burn the single-use token before the human clicks Approve there.
    public static class OrderApprovalEmailContent
    {
        public static string BuildApprovalRequestEmailHtml(Order order, string organizationName, OrderApprovalStep step, string approvalLink, string signInLink, string? languageCode)
        {
            var orderReference = order.OrderToken.ToString()[..8].ToUpperInvariant();

            return $$"""
                <div style="font-family:Segoe UI,Arial,sans-serif;max-width:640px;margin:0 auto;color:#1f2937;">
                  <div style="background:#0C8470;padding:20px 24px;border-radius:8px 8px 0 0;">
                    <div style="color:#ffffff;font-size:20px;font-weight:600;">InnNou</div>
                    <div style="color:#d1fae5;font-size:13px;">{{OrderApprovalEmailLocalization.Label("ApprovalNeededHeading", languageCode)}}</div>
                  </div>
                  <div style="border:1px solid #e5e7eb;border-top:none;padding:20px 24px;border-radius:0 0 8px 8px;">
                    <p style="font-size:13px;color:#374151;margin:0 0 16px;">{{OrderApprovalEmailLocalization.Label("IntroText", languageCode)}}</p>
                    <table style="font-size:12.5px;margin:0 0 20px;border-collapse:collapse;">
                      <tr><td style="padding:2px 10px 2px 0;color:#6b7280;white-space:nowrap;">{{OrderApprovalEmailLocalization.Label("OrderLabel", languageCode)}}</td><td style="padding:2px 0;color:#374151;">{{orderReference}}</td></tr>
                      <tr><td style="padding:2px 10px 2px 0;color:#6b7280;white-space:nowrap;">{{OrderApprovalEmailLocalization.Label("OrganizationLabel", languageCode)}}</td><td style="padding:2px 0;color:#374151;">{{organizationName}}</td></tr>
                      <tr><td style="padding:2px 10px 2px 0;color:#6b7280;white-space:nowrap;">{{OrderApprovalEmailLocalization.Label("WarehouseLabel", languageCode)}}</td><td style="padding:2px 0;color:#374151;">{{order.WarehouseName}}</td></tr>
                      <tr><td style="padding:2px 10px 2px 0;color:#6b7280;white-space:nowrap;">{{OrderApprovalEmailLocalization.Label("FamilyLabel", languageCode)}}</td><td style="padding:2px 0;color:#374151;">{{step.FamilyCode}}</td></tr>
                      <tr><td style="padding:2px 10px 2px 0;color:#6b7280;white-space:nowrap;">{{OrderApprovalEmailLocalization.Label("LevelLabel", languageCode)}}</td><td style="padding:2px 0;color:#374151;">{{step.Level}}</td></tr>
                      <tr><td style="padding:2px 10px 2px 0;color:#6b7280;white-space:nowrap;">{{OrderApprovalEmailLocalization.Label("ThresholdLabel", languageCode)}}</td><td style="padding:2px 0;color:#374151;">{{step.ThresholdAmount:0.00}} {{step.CurrencyCode}}</td></tr>
                      <tr><td style="padding:2px 10px 2px 0;color:#6b7280;white-space:nowrap;">{{OrderApprovalEmailLocalization.Label("ActualAmountLabel", languageCode)}}</td><td style="padding:2px 0;color:#374151;">{{step.ActualFamilyAmount:0.00}} {{step.CurrencyCode}}</td></tr>
                    </table>
                    <div style="text-align:center;margin:24px 0;">
                      <a href="{{approvalLink}}" style="display:inline-block;background:#0C8470;color:#ffffff;font-size:14px;font-weight:600;padding:12px 32px;border-radius:8px;text-decoration:none;">{{OrderApprovalEmailLocalization.Label("ApproveButton", languageCode)}}</a>
                    </div>
                    <p style="font-size:11px;color:#9ca3af;text-align:center;margin:0 0 20px;">{{OrderApprovalEmailLocalization.Label("LinkExpiresNote", languageCode)}}</p>
                    <hr style="border:none;border-top:1px solid #e5e7eb;margin:20px 0 12px;">
                    <p style="font-size:11.5px;color:#6b7280;margin:0;">{{OrderApprovalEmailLocalization.Label("SignInInsteadNote", languageCode)}} <a href="{{signInLink}}" style="color:#0C8470;">{{OrderApprovalEmailLocalization.Label("SignInLinkText", languageCode)}}</a></p>
                  </div>
                </div>
                """;
        }
    }
}
