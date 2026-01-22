using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServvistaWebAppAPI.Classes;
using ServvistaWebAppAPI.Models;

namespace ServvistaWebAppAPI.Controllers
{
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _repo;
        private readonly JwtTokenService _jwt; 

        public AuthController(UserRepository repo, JwtTokenService jwt)
        {
            _repo = repo; 
            _jwt = jwt; 
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel request)
        {
            var user = await _repo.GetByUserNameAsync(request.TECH_CODE);

            if (user == null || !user.IS_ACTIVE)
                return Unauthorized("Invalid Credentials");

            var hashed = PasswordHasher.Hash(request.Password);

            if (hashed != user.PASSWORD_HASH)
                return Unauthorized("Invalid Credentials");

            var (token, expiresAt) = _jwt.GenerateToken(user.TECH_CODE);

            return Ok(new LoginResponseModel { 
                TOKEN = token, 
                TECH_CODE = user.TECH_CODE,
                TECH_NAME = user.TECH_NAME,
                AREA = user.AREA, 
                CITY = user.CITY, 
                EMAIL = user.EMAIL,
                IS_ACTIVE = user.IS_ACTIVE,
                MOBILE_NO = user.MOBILE_NO,
            });
        }
    }
}
