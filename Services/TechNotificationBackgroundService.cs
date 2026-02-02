using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;
using ServvistaWebAppAPI.Classes;
using System.Data;
using System.Data.SqlClient;

namespace ServvistaWebAppAPI.Services
{
    public class TechNotificationBackgroundService : BackgroundService
    {
        private readonly ILogger<TechNotificationBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider; 

        public TechNotificationBackgroundService(ILogger<TechNotificationBackgroundService> logger, IConfiguration configuration, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("UserRecordCheckerService started.");

            var connectionString = _configuration.GetConnectionString("DefaultConnection");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = @"
                        SELECT DJ_ID AS jobID, DJ_DATE AS jobDate, SERIAL_NO AS serialNo, 
                        MACHINE_REF_NO AS machineRefNo, NOTE AS note, CUS_ADD1 AS customerName, JOB_STATUS AS jobStatus 
                        FROM TBL_DAILY_JOBS 
                        WHERE IS_TECH_NOTIFIED = '0'";                        

                        var result = await connection.QueryAsync<JobNotificationModel>(query);

                        if (result != null)
                        {
                            
                        }
                    }                                       
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
