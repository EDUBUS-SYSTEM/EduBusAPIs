namespace Utils
{
    public static class EmailHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim();

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return string.Equals(addr.Address, email, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        public static string NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLowerInvariant();

        public static string NormalizeEmailForKey(string? email)
        {
            var e = NormalizeEmail(email);
            if (string.IsNullOrEmpty(e)) return e;

            var at = e.IndexOf('@');
            if (at <= 0) return e;

            var local = e[..at];
            var domain = e[(at + 1)..];

            if (domain == "gmail.com" || domain == "googlemail.com")
            {
                var plus = local.IndexOf('+');
                if (plus >= 0) local = local[..plus];
                local = local.Replace(".", "");
            }

            return $"{local}@{domain}";
        }
    }
}
