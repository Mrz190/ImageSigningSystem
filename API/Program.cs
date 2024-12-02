using API.Data;
using API.Entity;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using API.Middleware;
using API.Repositories;
using API.Services;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure logging with Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// Configure SQL Server database connection
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IDigestAuthenticationService, DigestAuthenticationService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<IMailService, MailService>();
builder.Services.AddScoped<MD5Hash>();

// Configure Identity
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();


// Configure IdentityServer (without authentication)
builder.Services.AddIdentityServer()
    .AddDeveloperSigningCredential() // Developer signing credential for signing tokens
    .AddInMemoryApiResources(new List<ApiResource>()) // Empty list of API resources
    .AddInMemoryClients(new List<Client>()) // Empty list of clients
    .AddInMemoryApiScopes(new List<ApiScope>()) // Empty list of API scopes
    .AddAspNetIdentity<AppUser>(); // Link IdentityServer with ASP.NET Identity

builder.Services.AddAuthentication("Digest") // Устанавливаем Digest как стандартную схему
    .AddScheme<DigestAuthenticationOptions, DigestAuthenticationHandler>("Digest", options =>
    {
        options.Realm = builder.Configuration.GetValue<string>("DigestRealm");
    });

builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod()
               .WithExposedHeaders("*");
    });
});


// Configure AutoMapper and other services
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    });
}

app.UseCors("AllowAllOrigins");

app.UseRouting();

app.UseHttpsRedirection();

// Enable IdentityServer middleware
app.UseIdentityServer();

// Enable ASP.NET Core Identity Authentication middleware
app.UseAuthentication();

// Enable custom Digest Authentication middleware
app.UseDigestAuthentication();

// Enable Authorization middleware
app.UseAuthorization();

// Map attribute-routed API controllers
app.MapControllers();

// Seed database with roles during application startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<DataContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();

        // Create roles if they don't exist
        if (!await roleManager.RoleExistsAsync("Admin")) await roleManager.CreateAsync(new AppRole { Name = "Admin" });
        if (!await roleManager.RoleExistsAsync("Support")) await roleManager.CreateAsync(new AppRole { Name = "Support" });
        if (!await roleManager.RoleExistsAsync("User")) await roleManager.CreateAsync(new AppRole { Name = "User" });

        Console.BackgroundColor = ConsoleColor.DarkGreen;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(" Application started. ");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        // Log errors during role creation
        var logger = services.GetService<ILogger<Program>>();
        Console.BackgroundColor = ConsoleColor.Red;
        Console.ForegroundColor = ConsoleColor.White;
        logger.LogError(ex, "An error occurred while seeding roles in the database.");
        Console.ResetColor();
    }
}

app.Run();
