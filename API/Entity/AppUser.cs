using Microsoft.AspNetCore.Identity;

namespace API.Entity
{
    public class AppUser : IdentityUser<int>
    {
        public ICollection<AppUserRole> UserRoles { get; set; }
    }
}
