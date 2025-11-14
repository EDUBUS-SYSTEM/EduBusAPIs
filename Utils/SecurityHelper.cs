using System.Security.Cryptography;
using System.Text;

namespace Utils
{
    public static class SecurityHelper
    {
        public static string GenerateRandomPassword(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Generates a random password and returns both raw and hashed versions
        /// </summary>
        /// <param name="length">Length of password to generate (default 8)</param>
        /// <returns>Tuple of (rawPassword, hashedPasswordBytes)</returns>
        public static (string rawPassword, byte[] hashedPassword) GenerateAndHashPassword(int length = 8)
        {
            var rawPassword = GenerateRandomPassword(length);
            var hashedPassword = HashPassword(rawPassword);
            return (rawPassword, hashedPassword);
        }

        //1-way encryption
        public static byte[] HashPassword(string value)
        {
            string hashedString = BCrypt.Net.BCrypt.HashPassword(value);
            return Encoding.UTF8.GetBytes(hashedString);
        }
        public static bool VerifyPassword(string value, byte[] hashedBytes)
        {
            string hashedString = Encoding.UTF8.GetString(hashedBytes);
            return BCrypt.Net.BCrypt.Verify(value, hashedString);
        }

        //2-way encryption
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("ThisIsASecretKey1234567890123456"); // 32 bytes = 256-bit key

        public static byte[] EncryptToBytes(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV(); 

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            return ms.ToArray(); 
        }

        public static string DecryptFromBytes(byte[] cipherBytes)
        {
            using var aes = Aes.Create();
            aes.Key = Key;

            // Lấy IV từ đầu
            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[cipherBytes.Length - iv.Length];
            Array.Copy(cipherBytes, iv, iv.Length);
            Array.Copy(cipherBytes, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }

        public static string DecryptFromBase64String(string base64EncryptedString)
        {
            if (string.IsNullOrEmpty(base64EncryptedString))
            {
                return string.Empty;
            }

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(base64EncryptedString);
                return DecryptFromBytes(cipherBytes);
            }
            catch (Exception)
            {
                // Return empty string if decryption fails
                return string.Empty;
            }
        }
    }
}
