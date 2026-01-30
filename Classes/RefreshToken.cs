namespace ServvistaWebAppAPI.Classes
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = null; 
        public string UserId { get; set; } = null;

        public DateTime Expires { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Revoked { get; set; }
        public string? ReplacedByToken { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsActive => Revoked == null && !IsExpired;
    }
}
