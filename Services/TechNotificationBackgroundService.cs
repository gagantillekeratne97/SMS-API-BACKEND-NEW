using Dapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.VisualBasic;
using ServvistaWebAppAPI.Classes;
using ServvistaWebAppAPI.Models;
using System.Data;
using System.Data.SqlClient;

namespace ServvistaWebAppAPI.Services
{
    public class TechNotificationBackgroundService : BackgroundService
    {
        private readonly ILogger<TechNotificationBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider; 
        private readonly IHubContext<NotificationHub> _hubContext;
        //private readonly ITenantService _tenantService;        

        public TechNotificationBackgroundService(
                                                ILogger<TechNotificationBackgroundService> logger, 
                                                IConfiguration configuration, 
                                                IServiceProvider serviceProvider, 
                                                IHubContext<NotificationHub> hubContext)  
        {
            _logger = logger;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;            
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdateEmptyTechNotified();
                await ProcessAndSendNotification(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task UpdateEmptyTechNotified() { 
            string connectionString = _configuration.GetConnectionString("DefaultConnection"); 
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                UPDATE TBL_DAILY_JOBS
                SET IS_TECH_NOTIFIED = '0'
                WHERE IS_TECH_NOTIFIED IS NULL AND 
                JOB_STATUS = 'TECH ALLOCATED' AND                 
                DJ_DATE >= CAST(GETDATE() AS DATE)
                ";
                await connection.ExecuteAsync(query);
            }
        }


        private async Task ProcessAndSendNotification(CancellationToken token)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection"); 
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                SELECT 
                DJ_ID, 
                DJ_DATE, 
                SERIAL_NO, 
                MACHINE_REF_NO, 
                NOTE, 
                CONTACT_TYPE, 
                CUS_ADD1, 
                CUS_ADD2, 
                CUS_ADD3, 
                CUS_CONTACT, 
                CUS_TEL_NO, 
                CUS_SMS_NO, 
                MACHINE_MODEL_ID, 
                MACHINE_MODEL_NAME, 
                CUS_STATUS,       
                TECH_CODE,
                IS_TECH_NOTIFIED
                FROM TBL_DAILY_JOBS
                WHERE IS_TECH_NOTIFIED = '0'
                ";

                var jobs = (await connection.QueryAsync<BreakdownModel>(query)).ToList();
                var sentJobIds = new List<string>();

                foreach (var item in jobs)
                {
                    if (!string.IsNullOrWhiteSpace(item.TECH_CODE))
                    {
                        await _hubContext.Clients
                            .Group(item.TECH_CODE.Trim())
                            .SendAsync("ReceivingNotifications", item, cancellationToken: token);

                        sentJobIds.Add(item.DJ_ID);
                    }
                }

                if (!sentJobIds.Any())
                    return;

                await connection.ExecuteAsync(
                    "UPDATE TBL_DAILY_JOBS SET IS_TECH_NOTIFIED = '1' WHERE DJ_ID IN @Ids",
                    new { Ids = sentJobIds.Distinct().ToList() }
                );

            }
        }
    }
    }
