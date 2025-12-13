using System.Security.Cryptography;
using System.Text;

namespace GameLibary.Source
{
    public static class EncryptionHelper
    {
        public static bool TestPassword(string? input, string? hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword))
                return true; // hehe

            if (string.IsNullOrWhiteSpace(input))
                return false;

            return string.Equals(EncryptPassword(input), hashedPassword);
        }

        public static string EncryptPassword(string input)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(bytes);
            }
        }
    }
}
