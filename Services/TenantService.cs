namespace ServvistaWebAppAPI.Services
{
    public interface ITenantService
    {
        string GetCompanyName();
        string GetConnectionString();
    }
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config; 

        public TenantService(IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _httpContextAccessor = httpContextAccessor;
            _config = config;
        }

        public string GetCompanyName()
        {
            return _httpContextAccessor.HttpContext?.User?
                .FindFirst("company")?.Value;
        }

        public string GetConnectionString()
        {
            var company = GetCompanyName();
            return company switch
            {
                "001" => _config.GetConnectionString("DefaultConnection"), 
                "002" => _config.GetConnectionString("FintekConnection"), 
                            _ => throw new UnauthorizedAccessException($"Unknown company: {company}")
            };
        }
    }
}
