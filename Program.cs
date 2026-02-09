using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ServvistaWebAppAPI.Classes;
using ServvistaWebAppAPI.Services;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --------------------
// CORS (React + SignalR)
// --------------------
builder.Services.AddCors(options =>
{
    //this is something
    options.AddPolicy("AllowOrigin", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)  // ⚠️ Only for testing!
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
    //options.AddPolicy("AllowOrigin", policy =>
    //{
    //    policy
    //        .WithOrigins(
    //            "http://localhost:5173",  // Vite dev server
    //            "http://localhost:3000",  // React dev server
    //            "https://gestetner-service-schedule-4cse.vercel.app",  // ✅ ADD YOUR VERCEL DOMAIN
    //            "https://gestetner-service-schedule-git-227f58-chamodsathsaras-projects.vercel.app"  // ✅ ADD if you have preview deployments
    //        )
    //        .AllowAnyHeader()
    //        .AllowAnyMethod()
    //        .AllowCredentials();  // Required for SignalR
    //});
});

// --------------------
// SignalR
// --------------------
builder.Services.AddSignalR();
builder.Services.AddHostedService<TechNotificationBackgroundService>();

// --------------------
// Controllers & Services
// --------------------
builder.Services.AddControllers();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<ITechnicianPerformanceService, TechnicianPerformanceService>();
builder.Services.AddScoped<IBreakdownServices, BreakdownServices>();
builder.Services.AddScoped<IServiceSchedule, ServiceScheduleService>();

// --------------------
// Authentication (JWT + SignalR)
// --------------------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            ),
            NameClaimType = ClaimTypes.Name
        };

        // 🔥 REQUIRED for SignalR over WebSockets
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/notificationhub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// --------------------
// Swagger
// --------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --------------------
// Middleware pipeline (ORDER MATTERS)
// --------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ✅ IMPORTANT: UseCors MUST be here, BEFORE UseAuthentication
app.UseCors("AllowOrigin");

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// --------------------
// Endpoints
// --------------------
app.MapHub<NotificationHub>("/notificationhub");
app.MapControllers();

app.Run();