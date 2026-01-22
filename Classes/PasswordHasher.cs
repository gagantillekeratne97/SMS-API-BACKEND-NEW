using System.Security.Cryptography;
using System.Text;

namespace ServvistaWebAppAPI.Classes
{
    public class PasswordHasher
    {
        public static string Hash(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }
}
