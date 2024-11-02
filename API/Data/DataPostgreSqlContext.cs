using API.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class DataPostgreSqlContext : IdentityDbContext<AppUser,
                                                 AppRole,
                                                 int,
                                                 IdentityUserClaim<int>,
                                                 AppUserRole,
                                                 IdentityUserLogin<int>,
                                                 IdentityRoleClaim<int>,
                                                 IdentityUserToken<int>>
    {
        public DataPostgreSqlContext(DbContextOptions<DataPostgreSqlContext> options) : base(options)
        {

        }

        public DbSet<ImageHash> ImageHashes { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

        }
    }
}
