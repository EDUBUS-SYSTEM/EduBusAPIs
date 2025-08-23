using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
    }
}
