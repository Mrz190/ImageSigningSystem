using API.Data;
using API.Intefaces;
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
    }
}
