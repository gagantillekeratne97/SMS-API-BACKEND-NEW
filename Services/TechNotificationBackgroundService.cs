using Dapper;
using Microsoft.AspNetCore.SignalR;
using System.Data;
using System.Data.SqlClient;

namespace ServvistaWebAppAPI.Services
{
    public class TechNotificationBackgroundService : BackgroundService
    {
        private readonly ILogger<TechNotificationBackgroundService> _logger;
        private readonly IConfiguration _configuration;

        public TechNotificationBackgroundService(ILogger<TechNotificationBackgroundService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration; 
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("UserRecordCheckerService started.");

            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //using (IDbConnection db = new SqlConnection(connectionString))
                    //{
                    //    string sql = @"
                    //    SELECT TOP 1 *
                    //    FROM YourTable
                    //    WHERE UserCode = @UserCode
                    //    ORDER BY Id DESC
                    //";                        
                    //}
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking for latest user record.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("UserRecordCheckerService stopped.");
        }
    }
    }
