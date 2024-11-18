using API.Entity;

namespace API.Intefaces
{
    public interface IUserRepository
    {
        Task<string> GetUserPasswordFromDatabaseByName(string username);
        Task<AppUser> GetUserByUsernameAsync(string username);
        Task<List<string>> GetUserRolesAsync(AppUser user);
    }
}
