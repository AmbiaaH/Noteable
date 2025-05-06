using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NoteableApi.Data;
using NoteableApi.Helpers;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddLogging(logging => {
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

// CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials());
});

// EF Core + Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register the email service
builder.Services.AddScoped<IEmailService, EmailService>();

// Configure JWT Authentication
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = JwtHelper.GetValidationParameters();
});

// Add controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 64;
    });

// Set up wwwroot for static files
var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
builder.Environment.WebRootPath = webRootPath;
Directory.CreateDirectory(webRootPath);

// Create uploads folder 
var uploadsPath = Path.Combine(webRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

// Debug middleware for request logging
app.Use(async (context, next) => {
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var path = context.Request.Path;
    var authHeader = context.Request.Headers.Authorization.ToString();
    logger.LogDebug($"Request to {path} with Auth: {(string.IsNullOrEmpty(authHeader) ? "NONE" : "Bearer token present")}");
    
    await next();
    
    logger.LogDebug($"Response: {context.Response.StatusCode} for {path}");
});

// Seed roles and default admin user
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    var programLogger = serviceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        programLogger.LogInformation("Database migrated successfully");
        
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = { "Admin", "Mentor", "Student" };
        
        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
                programLogger.LogInformation($"Created role: {role}");
            }
        }

        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var adminUsername = "adminambia";
        var adminUser = await userManager.FindByNameAsync(adminUsername);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminUsername,
                Email = "admin@noteable.com",
                FirstName = "Admin",
                LastName = "Ambia",
                EmailConfirmed = true
            };
            
            var result = await userManager.CreateAsync(adminUser, "Adminambia123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                programLogger.LogInformation("Default admin user created");
            }
            else
            {
                programLogger.LogError("Failed to create default admin user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
    catch (Exception ex)
    {
        programLogger.LogError(ex, "An error occurred during startup");
    }
}

// Map controllers
app.MapControllers();

// Log that the app is starting
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application starting up");

app.Run();
