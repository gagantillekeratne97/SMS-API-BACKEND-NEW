using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ServvistaWebAppAPI.Classes
{
    public class JwtTokenService
    {
        private readonly IConfiguration _config; 
        public JwtTokenService(IConfiguration config)
        {
            _config = config; 
        }

        public string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public (string token, DateTime expirationAt) GenerateCustomerToken(string serialNo)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, serialNo)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiryMinutes = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:DurationInMinutes"]!));
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"], 
                audience: _config["Jwt:Audience"], 
                claims: claims, 
                expires: expiryMinutes, 
                signingCredentials: creds
                );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiryMinutes);
        }

        public (string token, DateTime expirationAt) GenerateToken(string techCode)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, techCode)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiryMinutes = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:DurationInMinutes"]!));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"], 
                audience: _config["Jwt:Audience"], 
                claims: claims, 
                expires: expiryMinutes, 
                signingCredentials: creds);

            return (new JwtSecurityTokenHandler().WriteToken(token), expiryMinutes);
        }
    }
}
