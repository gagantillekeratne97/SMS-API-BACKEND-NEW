using System.Data;
using System.Data.SqlClient;
using ServvistaWebAppAPI.Models;
using ServvistaWebAppAPI.Models.Dtos;
using ServvistaWebAppAPI.Repositories;


namespace ServvistaWebAppAPI.Services
{
    public class ManagementServices : IManagementRepository
    {
        private readonly string _connectionString;
        private readonly ITenantService _tenantService; 

        public ManagementServices(IConfiguration config, ITenantService tenantService   )
        {
            _tenantService = tenantService;
            _connectionString = _tenantService.GetConnectionString();            
        }

        public JobCountAndRateDto GetJobCountAndRate()
        {
            string jobCountQuery = @"
            SELECT COUNT(*) 
            FROM TBL_DAILY_JOBS
            WHERE DJ_DATE >= DATEADD(DAY, -30, GETDATE())";

            string successCountQuery = @"
            SELECT COUNT(*) 
            FROM TBL_DAILY_JOBS 
            WHERE (JOB_STATUS = 'COMPLETED' OR JOB_STATUS = 'COMPLETE')
            AND DJ_DATE >= DATEADD(DAY, -30, GETDATE())";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                int jobCount = 0;
                int successCount = 0;

                using (var cmd = new SqlCommand(jobCountQuery, connection))
                {
                    jobCount = (int)cmd.ExecuteScalar();
                }

                using (var cmd = new SqlCommand(successCountQuery, connection))
                {
                    successCount = (int)cmd.ExecuteScalar();
                }

                double jobRate = jobCount > 0 ? (double)successCount / jobCount * 100 : 0;

                return new JobCountAndRateDto
                {
                    JobCount = jobCount,
                    SuccessCount = successCount,
                    JobRate = Math.Round(jobRate, 2)
                };
            }
        }

        public JobCountAndRateDto GetServiceCountAndRate()
        {
            string servicesCountQuery = @"
                SELECT COUNT(*) AS totalRow
                FROM dbo.TBL_SERVICE_SCEDULE_UPDATE s
                INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
                    ON c.CUS_CODE = s.CUS_ID
                CROSS APPLY
                (
                    VALUES
                        ('EXPT_SV1', s.EXPT_SV1, s.SV1),
                        ('EXPT_SV2', s.EXPT_SV2, s.SV2),
                        ('EXPT_SV3', s.EXPT_SV3, s.SV3),
                        ('EXPT_SV4', s.EXPT_SV4, s.SV4),
                        ('EXPT_SV5', s.EXPT_SV5, s.SV5),
                        ('EXPT_SV6', s.EXPT_SV6, s.SV6)
                ) v (VisitNo, ExpectedDate, ActualVisit)
                WHERE v.ExpectedDate IS NOT NULL
                AND v.ExpectedDate >= DATEADD(DAY, -30, GETDATE());
            ";

            string successCountQuery = @"
                SELECT COUNT(*) AS completeRow
                FROM dbo.TBL_SERVICE_SCEDULE_UPDATE s
                INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
                    ON c.CUS_CODE = s.CUS_ID
                CROSS APPLY
                (
                    VALUES
                        ('EXPT_SV1', s.EXPT_SV1, s.SV1),
                        ('EXPT_SV2', s.EXPT_SV2, s.SV2),
                        ('EXPT_SV3', s.EXPT_SV3, s.SV3),
                        ('EXPT_SV4', s.EXPT_SV4, s.SV4),
                        ('EXPT_SV5', s.EXPT_SV5, s.SV5),
                        ('EXPT_SV6', s.EXPT_SV6, s.SV6)
                ) v (VisitNo, ExpectedDate, ActualVisit)
                WHERE v.ExpectedDate IS NOT NULL
                  AND v.ExpectedDate >= DATEADD(DAY, -30, GETDATE())
                  AND v.ActualVisit IS NOT NULL;
            ";


            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                int jobCount = 0;
                int successCount = 0;

                using (var cmd = new SqlCommand(servicesCountQuery, connection))
                {
                    jobCount = (int)cmd.ExecuteScalar();
                }

                using (var cmd = new SqlCommand(successCountQuery, connection))
                {
                    successCount = (int)cmd.ExecuteScalar();
                }

                double jobRate = jobCount > 0 ? (double)successCount / jobCount * 100 : 0;

                return new JobCountAndRateDto
                {
                    JobCount = jobCount,
                    SuccessCount = successCount,
                    JobRate = Math.Round(jobRate, 2)
                };
            }
        }

        public List<BreakdownModel> GetPendingJobs()
        {
            string query = @"
            SELECT DJ_ID, SERIAL_NO, MACHINE_REF_NO, CUS_NAME, CUS_ADD1, CUS_ADD2, CUS_ADD3, CUS_CONTACT, 
            CUS_SMS_NO AS CUS_TEL_NO, TEAM_ID, TEAM_NAME, DJ_DATE, TECH_CODE, TECH_MOBILE, MACHINE_MODEL_ID, 
            MACHINE_MODEL_NAME, CUS_STATUS, JOB_STATUS, NOTE
            FROM TBL_DAILY_JOBS 
            WHERE JOB_STATUS = 'TECH ALLOCATED'";

            var jobs = new List<BreakdownModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var cmd = new SqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        jobs.Add(new BreakdownModel
                        {
                            DJ_ID = reader["DJ_ID"]?.ToString(),
                            SERIAL_NO = reader["SERIAL_NO"]?.ToString(),
                            MACHINE_REF_NO = reader["MACHINE_REF_NO"]?.ToString(),
                            CUS_NAME = reader["CUS_NAME"]?.ToString(),
                            CUS_ADD1 = reader["CUS_ADD1"]?.ToString(),
                            CUS_ADD2 = reader["CUS_ADD2"]?.ToString(),
                            CUS_ADD3 = reader["CUS_ADD3"]?.ToString(),
                            CUS_CONTACT = reader["CUS_CONTACT"]?.ToString(),
                            CUS_TEL_NO = reader["CUS_TEL_NO"]?.ToString(),
                            TEAM_ID = reader["TEAM_ID"]?.ToString(),
                            TEAM_NAME = reader["TEAM_NAME"]?.ToString(),
                            DJ_DATE = reader["DJ_DATE"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader["DJ_DATE"]),
                            TECH_CODE = reader["TECH_CODE"]?.ToString(),
                            TECH_NAME = null,         // not in query, default null
                            TECH_MOBILE = reader["TECH_MOBILE"]?.ToString(),
                            MACHINE_MODEL_ID = reader["MACHINE_MODEL_ID"]?.ToString(),
                            MACHINE_MODEL_NAME = reader["MACHINE_MODEL_NAME"]?.ToString(),
                            CUS_STATUS = reader["CUS_STATUS"]?.ToString(),
                            NOTE = reader["NOTE"]?.ToString(),
                            JOB_STATUS = reader["JOB_STATUS"]?.ToString(),
                            IS_TECH_NOTIFIED = false,       // not in query, default false
                            TYPE = null          // not in query, default null
                        });
                    }
                }
            }

            return jobs;
        }

        public List<ServiceVisitMonthlyInfo> GetPendingServices()
        {
            string query = @"
        SELECT
            s.T_ID AS rowId,
            s.CUS_ID AS customerID,
            c.CUS_NAME AS customerName,    
            c.CONTACT_PERSON AS contactPerson,
            c.TEL_NO AS customerTelephone,
            s.M_LOC1 AS machineLocation01, 
            s.M_LOC2 AS machineLocation02, 
            s.M_LOC3 AS machineLocation03, 
            s.MACHINE_REF AS machineRefNo,
            s.TECH_NAME AS techName,
            s.SERIAL_NO AS serialNo,
            v.VisitNo AS expectedVisitNo,
            CONVERT(char(10), v.ExpectedDate, 120) AS expectedVisitDate,
            CASE
                WHEN v.ActualVisit IS NOT NULL THEN 'COMPLETED'
                ELSE 'PENDING'
            END AS VisitStatus
        FROM dbo.TBL_SERVICE_SCEDULE_UPDATE s
        INNER JOIN dbo.MTBL_CUSTOMER_MASTER c
            ON c.CUS_CODE = s.CUS_ID
        CROSS APPLY
        (
            VALUES
                ('EXPT_SV1', s.EXPT_SV1, s.SV1),
                ('EXPT_SV2', s.EXPT_SV2, s.SV2),
                ('EXPT_SV3', s.EXPT_SV3, s.SV3),
                ('EXPT_SV4', s.EXPT_SV4, s.SV4),
                ('EXPT_SV5', s.EXPT_SV5, s.SV5),
                ('EXPT_SV6', s.EXPT_SV6, s.SV6)
        ) v(VisitNo, ExpectedDate, ActualVisit)
        WHERE v.ExpectedDate IS NOT NULL
        AND v.ExpectedDate >= DATEADD(DAY, -7, GETDATE())
        ORDER BY v.ExpectedDate, v.VisitNo";

            var services = new List<ServiceVisitMonthlyInfo>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var cmd = new SqlCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        services.Add(new ServiceVisitMonthlyInfo
                        {
                            RowId = reader["rowId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["rowId"]),
                            customerID = reader["customerID"]?.ToString(),
                            customerName = reader["customerName"]?.ToString(),
                            contactPerson = reader["contactPerson"] == DBNull.Value ? null : reader["contactPerson"].ToString(),
                            customerTelephone = reader["customerTelephone"]?.ToString(),
                            machineLocation01 = reader["machineLocation01"]?.ToString(),
                            machineLocation02 = reader["machineLocation02"]?.ToString(),
                            machineLocation03 = reader["machineLocation03"]?.ToString(),
                            machineRefNo = reader["machineRefNo"]?.ToString(),
                            techName = reader["techName"]?.ToString(),
                            serialNo = reader["serialNo"]?.ToString(),
                            expectedVisitNo = reader["expectedVisitNo"]?.ToString(),
                            expectedVisitDate = reader["expectedVisitDate"] == DBNull.Value
                            ? DateTime.MinValue
                            : DateTime.Parse(reader["expectedVisitDate"].ToString()),
                            VisitStatus = reader["VisitStatus"]?.ToString(),

                            // Not in query — set defaults
                            expectedVisitCount = 0,
                            machineModel = null
                        });
                    }
                }
            }

            return services;
        }

        public List<CompleteAndPendingPercentageDto> GetCompleteAndPendingJobPercentage()
        {
            string query = @"
            SELECT 
                SUM(CASE WHEN JOB_STATUS IN ('COMPLETE', 'COMPLETED') THEN 1 ELSE 0 END) AS completed,
                SUM(CASE WHEN JOB_STATUS = 'STARTED'        THEN 1 ELSE 0 END) AS started,
                SUM(CASE WHEN JOB_STATUS = 'TECH ALLOCATED' THEN 1 ELSE 0 END) AS pending,
	            SUM(CASE WHEN JOB_STATUS = 'CANCELLED' THEN 1 ELSE 0 END) AS cancel,
                COUNT(*) AS total
            FROM TBL_DAILY_JOBS WITH (NOLOCK)";


            Console.WriteLine("[GetCompleteAndPendingJobPercentage] Starting...");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    Console.WriteLine("[GetCompleteAndPendingJobPercentage] Opening connection...");
                    connection.Open();
                    Console.WriteLine("[GetCompleteAndPndingJobPercentage] Connection opened successfully.");

                    using (var cmd = new SqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("[GetCompleteAndPendingJobPercentage] Query executed.");

                        if (reader.Read())
                        {
                            int total = reader.GetInt32(reader.GetOrdinal("total"));
                            int completed = reader.GetInt32(reader.GetOrdinal("completed"));
                            int started = reader.GetInt32(reader.GetOrdinal("started"));
                            int pending = reader.GetInt32(reader.GetOrdinal("pending"));
                            int cansel = reader.GetInt32(reader.GetOrdinal("cancel"));

                            Console.WriteLine($"[GetCompleteAndPendingJobPercentage] Raw values => Total: {total}, Completed: {completed}, Started: {started}, Pending: {pending}");

                            int completedPct = total > 0 ? (int)Math.Round((double)completed * 100 / total) : 0;
                            int startedPct = total > 0 ? (int)Math.Round((double)started * 100 / total) : 0;
                            int pendingPct = total > 0 ? (int)Math.Round((double)pending * 100 / total) : 0;
                            int canselPct = total > 0 ? (int)Math.Round((double)cansel * 100 / total) : 0;

                            Console.WriteLine($"[GetCompleteAndPendingJobPercentage] Percentages => Completed: {completedPct}%, Started: {startedPct}%, Pending: {pendingPct}%");

                            return new List<CompleteAndPendingPercentageDto>
                            {
                                new CompleteAndPendingPercentageDto { name = "Completed", value = completedPct },
                                new CompleteAndPendingPercentageDto { name = "Started",   value = startedPct   },
                                new CompleteAndPendingPercentageDto { name = "Pending",   value = pendingPct   },
                                new CompleteAndPendingPercentageDto { name = "Cansel",   value = canselPct   }
                            };
                        }
                        else
                        {
                            Console.WriteLine("[GetCompleteAndPendingJobPercentage] WARNING: Query returned no rows.");
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"[GetCompleteAndPendingJobPercentage] SQL ERROR: {sqlEx.Message}");
                Console.WriteLine($"[GetCompleteAndPendingJobPercentage] SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"[GetCompleteAndPendingJobPercentage] SQL State: {sqlEx.State}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetCompleteAndPendingJobPercentage] UNEXPECTED ERROR: {ex.Message}");
                Console.WriteLine($"[GetCompleteAndPendingJobPercentage] StackTrace: {ex.StackTrace}");
                throw;
            }

            Console.WriteLine("[GetCompleteAndPendingJobPercentage] Returning fallback zeros.");
            return new List<CompleteAndPendingPercentageDto>
                {
                    new CompleteAndPendingPercentageDto { name = "Completed", value = 0 },
                    new CompleteAndPendingPercentageDto { name = "Started",   value = 0 },
                    new CompleteAndPendingPercentageDto { name = "Pending",   value = 0 }
                };
        }


        public List<CompleteAndPendingPercentageDto> GetCompleteAndPendingServicesPercentage()
        {
            string query = @"
            SELECT
                COUNT(*) AS total,
                SUM(CASE WHEN v.ActualVisit IS NOT NULL THEN 1 ELSE 0 END) AS completed,
                SUM(CASE WHEN v.ActualVisit IS NULL     THEN 1 ELSE 0 END) AS pending
            FROM dbo.TBL_SERVICE_SCEDULE_UPDATE s WITH (NOLOCK)
            INNER JOIN dbo.MTBL_CUSTOMER_MASTER c WITH (NOLOCK)
                ON c.CUS_CODE = s.CUS_ID
            CROSS APPLY
            (
                VALUES
                    ('EXPT_SV1', s.EXPT_SV1, s.SV1),
                    ('EXPT_SV2', s.EXPT_SV2, s.SV2),
                    ('EXPT_SV3', s.EXPT_SV3, s.SV3),
                    ('EXPT_SV4', s.EXPT_SV4, s.SV4),
                    ('EXPT_SV5', s.EXPT_SV5, s.SV5),
                    ('EXPT_SV6', s.EXPT_SV6, s.SV6)
            ) v (VisitNo, ExpectedDate, ActualVisit)
            WHERE v.ExpectedDate IS NOT NULL";

            Console.WriteLine("[GetCompleteAndPendingServicesPercentage] Starting...");

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    Console.WriteLine("[GetCompleteAndPendingServicesPercentage] Opening connection...");
                    connection.Open();
                    Console.WriteLine("[GetCompleteAndPendingServicesPercentage] Connection opened successfully.");

                    using (var cmd = new SqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("[GetCompleteAndPendingServicesPercentage] Query executed.");

                        if (reader.Read())
                        {
                            int total = reader.GetInt32(reader.GetOrdinal("total"));
                            int completed = reader.GetInt32(reader.GetOrdinal("completed"));
                            int pending = reader.GetInt32(reader.GetOrdinal("pending"));

                            Console.WriteLine($"[GetCompleteAndPendingServicesPercentage] Raw values => Total: {total}, Completed: {completed}, Pending: {pending}");

                            int completedPct = total > 0 ? (int)Math.Round((double)completed * 100 / total) : 0;
                            int pendingPct = total > 0 ? (int)Math.Round((double)pending * 100 / total) : 0;


                            Console.WriteLine($"[GetCompleteAndPendingServicesPercentage] Percentages => Completed: {completedPct}%, Pending: {pendingPct}%");

                            return new List<CompleteAndPendingPercentageDto>
                            {
                                new CompleteAndPendingPercentageDto { name = "Completed", value = completedPct },
                                new CompleteAndPendingPercentageDto { name = "Started",   value = 0            },
                                new CompleteAndPendingPercentageDto { name = "Pending",   value = pendingPct   }
                            };
                        }
                        else
                        {
                            Console.WriteLine("[GetCompleteAndPendingServicesPercentage] WARNING: Query returned no rows.");
                        }
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"[GetCompleteAndPendingServicesPercentage] SQL ERROR: {sqlEx.Message}");
                Console.WriteLine($"[GetCompleteAndPendingServicesPercentage] SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"[GetCompleteAndPendingServicesPercentage] SQL State: {sqlEx.State}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetCompleteAndPendingServicesPercentage] UNEXPECTED ERROR: {ex.Message}");
                Console.WriteLine($"[GetCompleteAndPendingServicesPercentage] StackTrace: {ex.StackTrace}");
                throw;
            }

            Console.WriteLine("[GetCompleteAndPendingServicesPercentage] Returning fallback zeros.");
            return new List<CompleteAndPendingPercentageDto>
            {
                new CompleteAndPendingPercentageDto { name = "Completed", value = 0 },
                new CompleteAndPendingPercentageDto { name = "Started",   value = 0 },
                new CompleteAndPendingPercentageDto { name = "Pending",   value = 0 }
            };
        }

        public List<LastYearJobPerformanceDto> GetLastWeekJobPerformance()
        {
            string query = @"
        SELECT 
            FORMAT(DJ_DATE, 'MM/dd') AS date,
            COUNT(CASE WHEN JOB_STATUS = 'TECH ALLOCATED'          THEN 1 END) AS pending,
            COUNT(CASE WHEN JOB_STATUS IN ('COMPLETED', 'COMPLETE') THEN 1 END) AS completed,
            COUNT(CASE WHEN JOB_STATUS = 'STARTED'                 THEN 1 END) AS started,
            COUNT(CASE WHEN JOB_STATUS = 'CANCELLED'               THEN 1 END) AS cancel,
            COUNT(*) AS total
        FROM TBL_DAILY_JOBS WITH (NOLOCK)
        WHERE DJ_DATE >= DATEADD(DAY, -7, GETDATE())
        GROUP BY CAST(DJ_DATE AS DATE)
        ORDER BY CAST(DJ_DATE AS DATE)";

            Console.WriteLine("[GetLastWeekJobPerformance] Starting...");

            var result = new List<LastYearJobPerformanceDto>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    Console.WriteLine("[GetLastWeekJobPerformance] Opening connection...");
                    connection.Open();
                    Console.WriteLine("[GetLastWeekJobPerformance] Connection opened successfully.");

                    using (var cmd = new SqlCommand(query, connection))
                    using (var reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("[GetLastWeekJobPerformance] Query executed.");

                        int idxDate = reader.GetOrdinal("date");
                        int idxPending = reader.GetOrdinal("pending");
                        int idxCompleted = reader.GetOrdinal("completed");
                        int idxStarted = reader.GetOrdinal("started");
                        int idxCancel = reader.GetOrdinal("cancel");
                        int idxTotal = reader.GetOrdinal("total");

                        while (reader.Read())
                        {
                            var row = new LastYearJobPerformanceDto
                            {
                                date = reader.IsDBNull(idxDate) ? null : reader.GetString(idxDate),
                                pending = reader.IsDBNull(idxPending) ? 0 : reader.GetInt32(idxPending),
                                completed = reader.IsDBNull(idxCompleted) ? 0 : reader.GetInt32(idxCompleted),
                                started = reader.IsDBNull(idxStarted) ? 0 : reader.GetInt32(idxStarted),
                                cancel = reader.IsDBNull(idxCancel) ? 0 : reader.GetInt32(idxCancel),
                                total = reader.IsDBNull(idxTotal) ? 0 : reader.GetInt32(idxTotal)
                            };

                            Console.WriteLine($"[GetLastWeekJobPerformance] Row => Date: {row.date}, Pending: {row.pending}, Completed: {row.completed}, Started: {row.started}, Cancel: {row.cancel}, Total: {row.total}");

                            result.Add(row);
                        }

                        Console.WriteLine($"[GetLastWeekJobPerformance] Total rows fetched: {result.Count}");
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Console.WriteLine($"[GetLastWeekJobPerformance] SQL ERROR: {sqlEx.Message}");
                Console.WriteLine($"[GetLastWeekJobPerformance] SQL Error Number: {sqlEx.Number}");
                Console.WriteLine($"[GetLastWeekJobPerformance] SQL State: {sqlEx.State}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetLastWeekJobPerformance] UNEXPECTED ERROR: {ex.Message}");
                Console.WriteLine($"[GetLastWeekJobPerformance] StackTrace: {ex.StackTrace}");
                throw;
            }

            return result;
        }

        public List<TechnicianPerformenceDto> GetTechniciansPerformence()
        {
            return new List<TechnicianPerformenceDto>
            {
                new TechnicianPerformenceDto
                {
                    tech_id = "0000127",
                    name = "Sudesh",
                    completedJobs = 20,
                    rating = 4.3,
                    services = 10,
                    breakdowns = 5
                },
                new TechnicianPerformenceDto
                {
                    tech_id = "0000128",
                    name = "Nimal",
                    completedJobs = 15,
                    rating = 3.8,
                    services = 8,
                    breakdowns = 7
                },
                new TechnicianPerformenceDto
                {
                    tech_id = "0000129",
                    name = "Kamal",
                    completedJobs = 25,
                    rating = 5.0,
                    services = 12,
                    breakdowns = 3
                },
                new TechnicianPerformenceDto
                {
                    tech_id = "0000125",
                    name = "Sudesh",
                    completedJobs = 20,
                    rating = 4.3,
                    services = 10,
                    breakdowns = 5
                },
                new TechnicianPerformenceDto
                {
                    tech_id = "0000124",
                    name = "Nimal",
                    completedJobs = 15,
                    rating = 3.8,
                    services = 8,
                    breakdowns = 7
                },
                new TechnicianPerformenceDto
                {
                    tech_id = "0000123",
                    name = "Kamal",
                    completedJobs = 25,
                    rating = 5.0,
                    services = 12,
                    breakdowns = 3
                }
            };
        }

        public List<OlderstDueDto> GetOldestDueJobs()
        {
            return new List<OlderstDueDto>
            {
                new OlderstDueDto
                {
                    jobId = "JOB12345",
                    technicianName = "Sudesh",
                    jobType = "Breakdown",
                    location = "Colombo",
                    daysLeft = "2"
                },
                new OlderstDueDto
                {
                    jobId = "JOB12346",
                    technicianName = "Nimal",
                    jobType = "Service",
                    location = "Kandy",
                    daysLeft = "5"
                },
                new OlderstDueDto
                {
                    jobId = "JOB12347",
                    technicianName = "Kamal",
                    jobType = "Breakdown",
                    location = "Galle",
                    daysLeft = "1"
                }
            };
        }

        public List<WarrantyDetailsDto> GetWarrantyDetails()
        {
            return new List<WarrantyDetailsDto>
            {
                new WarrantyDetailsDto
                {
                    erea = "Colombo",
                    ns = 10,
                    fs = 20,
                    ma = 5,
                    ex = 2
                },
                new WarrantyDetailsDto
                {
                    erea = "Kandy",
                    ns = 15,
                    fs = 25,
                    ma = 8,
                    ex = 3
                },
                new WarrantyDetailsDto
                {
                    erea = "Galle",
                    ns = 8,
                    fs = 18,
                    ma = 4,
                    ex = 1
                }
            };


        }
    }
}
