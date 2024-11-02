using API.Data;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions
{
    public static class DatabaseExtension
    {
        public static IServiceCollection ConnectPostgreSQL(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DataPostgreSqlContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("DbConnection"));
            });

            return services;
        }
    }
}
