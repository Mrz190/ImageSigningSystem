using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions
{
    public static class DatabaseExtension
    {
        public static IServiceCollection ConnectSqlServer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DataContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DbConnection"));
            });

            return services;
        }
    }
}
