using API.Data;
using API.Entity;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly DataContext _context;
        public UserRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<string> GetUserPasswordFromDatabaseByName(string username)
        {
            var password = await _context.Users.AsNoTracking()
                                               .Where(x => x.UserName == username)
                                               .Select(x => x.PasswordHash).FirstOrDefaultAsync();

            return password;
        }

        public async Task<AppUser> GetUserByUsernameAsync(string username)
        {
            var user = await _context.Users.AsNoTracking()
                                           .Where(u => u.UserName == username)
                                           .FirstOrDefaultAsync();
            return user;
        }
        public async Task<List<string>> GetUserRolesAsync(AppUser user)
        {
            // Используя UserManager или напрямую через таблицу UserRoles, получаем роли пользователя
            var roles = await _context.UserRoles
                                       .Where(ur => ur.UserId == user.Id)
                                       .Select(ur => ur.Role.Name)
                                       .ToListAsync();
            return roles;
        }
    }
}
