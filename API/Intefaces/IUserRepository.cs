namespace API.Intefaces
{
    public interface IUserRepository
    {
        Task<string> GetUserPasswordFromDatabaseByName(string username);
    }
}
