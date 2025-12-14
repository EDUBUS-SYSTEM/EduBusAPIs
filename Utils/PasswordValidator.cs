using System.Text.RegularExpressions;

namespace Utils
{
    public static class PasswordValidator
    {
        private const int MinPasswordLength = 8;
        private static readonly Regex UppercaseRegex = new Regex(@"[A-Z]");
        private static readonly Regex LowercaseRegex = new Regex(@"[a-z]");
        private static readonly Regex DigitRegex = new Regex(@"\d");

        public static (bool IsValid, string? ErrorMessage) ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return (false, "Password cannot be empty");
            }

            if (password.Length < MinPasswordLength)
            {
                return (false, $"Password must be at least {MinPasswordLength} characters long");
            }

            if (!UppercaseRegex.IsMatch(password))
            {
                return (false, "Password must contain at least one uppercase letter");
            }

            if (!LowercaseRegex.IsMatch(password))
            {
                return (false, "Password must contain at least one lowercase letter");
            }

            if (!DigitRegex.IsMatch(password))
            {
                return (false, "Password must contain at least one number");
            }

            return (true, null);
        }

        public static bool IsSamePassword(string password1, string password2)
        {
            return string.Equals(password1, password2, StringComparison.Ordinal);
        }
    }
}
