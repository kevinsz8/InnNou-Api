using System.Text.RegularExpressions;

namespace InnNou.Application.Common
{
    // Single source of truth for User email format + password strength — reused by
    // CreateUserCommandHandler/EditUserCommandHandler (single-row) and
    // UserService.BulkImportUsersAsync (bulk), so the two paths can never silently diverge.
    public static class UserValidation
    {
        private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        public static bool IsValidEmail(string? email) =>
            !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);

        public static bool IsStrongPassword(string? password) =>
            !string.IsNullOrWhiteSpace(password) &&
            password.Length >= 8 &&
            password.Any(char.IsUpper) &&
            password.Any(char.IsLower) &&
            password.Any(char.IsDigit) &&
            password.Any(c => !char.IsLetterOrDigit(c));
    }
}
