using API.Data;
using API.Entity;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();


// Настройка базы данных SQL Server
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnection")));

// Настройка Identity
builder.Services.AddIdentity<AppUser, AppRole>()
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();

// Настройка IdentityServer без аутентификации
builder.Services.AddIdentityServer()
    .AddDeveloperSigningCredential()
    .AddInMemoryApiResources(new List<ApiResource>()) // Пустой список ресурсов
    .AddInMemoryClients(new List<Client>()) // Пустой список клиентов
    .AddInMemoryApiScopes(new List<ApiScope>()) // Пустой список скоупов
    .AddAspNetIdentity<AppUser>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddLogging();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v2");
    });
}

app.UseRouting();

app.UseHttpsRedirection();

// IdentityServer
app.UseIdentityServer();

app.UseAuthorization();
app.MapControllers();

using(var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<DataContext>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var roleManager = services.GetRequiredService<RoleManager<AppRole>>();

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
        var logger = services.GetService<ILogger<Program>>();
        Console.BackgroundColor = ConsoleColor.Red;
        Console.ForegroundColor = ConsoleColor.White;
        logger.LogError(ex, "An error occurred while seeding role in the database.");
        Console.ResetColor();
    }
}

app.Run();
