using Dapper;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ServvistaWebAppAPI.Classes;
using ServvistaWebAppAPI.Models;
using System.Data.SqlClient;

namespace ServvistaWebAppAPI.Controllers
{
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _repo;
        private readonly JwtTokenService _jwt;
        public string _connectionString;
        private readonly IConfiguration _config; 

        public AuthController(UserRepository repo, JwtTokenService jwt)
        {
            _connectionString = _config.GetConnectionString("DefaultConnection");
            _repo = repo;
            _jwt = jwt;
        }    

        [HttpPost("resetPassword")]
        public IActionResult ResetPassword(string techCode, [FromBody] string newPassword)
        {
            try
            {
                string connectionString = @"Data Source=sql5079.site4now.net;Initial Catalog=DB_A67CC4_Servvistagcp;User ID=DB_A67CC4_Servvistagcp_admin;Password=Ssg789.541351;";
                var hashedPassword = PasswordHasher.Hash(newPassword);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string query = @"
                    UPDATE MTBL_TECH_OFFICERS 
                    SET PASSWORD_HASH = @hashedPassword 
                    WHERE TECH_CODE = @techCode";
                    int rowsAffected = connection.Execute(query, new { hashedPassword, techCode });
                    if (rowsAffected > 0)
                    {
                        return Ok("Password reset successful.");
                    }
                    else
                    {
                        return NotFound("Technician not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginModel request)
        {
            // 1️⃣ Try machine login first
            var machineTransaction =
                await _repo.GetCustomerInfoBySerial(request.SERIAL_NO);

            // 2️⃣ If machine not found, try tech login
            var techInformation =
                machineTransaction == null
                    ? await _repo.GetByUserNameAsync(request.TECH_CODE)
                    : null;

            // 3️⃣ If neither exists → invalid credentials
            if (machineTransaction == null && techInformation == null)
            {
                return Unauthorized("Invalid Credentials");
            }

            string token;
            DateTime expiresAt;

            // ================= TECH LOGIN =================
            if (techInformation != null)
            {
                var hashedPassword = PasswordHasher.Hash(request.Password);

                if (hashedPassword != techInformation.PASSWORD_HASH)
                {
                    return Unauthorized("Invalid Credentials");
                }

                if (!techInformation.IS_ACTIVE)
                {
                    return Unauthorized("User is Inactive");
                }

                (token, expiresAt) =
                    _jwt.GenerateToken(techInformation.TECH_CODE);

                var refreshToken = _jwt.GenerateRefreshToken(techInformation.TECH_CODE).Token;                

                return Ok(new LoginResponseModel
                {
                    TOKEN = token,
                    REFRESH_TOKEN = refreshToken,
                    TECH_CODE = techInformation.TECH_CODE,
                    TECH_NAME = techInformation.TECH_NAME,
                    AREA = techInformation.AREA,
                    CITY = techInformation.CITY,
                    EMAIL = techInformation.EMAIL,
                    IS_ACTIVE = techInformation.IS_ACTIVE,
                    MOBILE_NO = techInformation.MOBILE_NO
                });
            }

            // ================= MACHINE LOGIN =================
            (token, expiresAt) =
                _jwt.GenerateCustomerToken(machineTransaction.SERIAL_NO);

            return Ok(new LoginCustomerResponseModel
            {
                TOKEN = token,
                SERIAL_NO = machineTransaction.SERIAL_NO,
                MACHINE_REF_CODE = machineTransaction.MACHINE_REF_CODE
            });
        }

    }
}