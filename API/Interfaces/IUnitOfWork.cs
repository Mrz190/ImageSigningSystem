using Microsoft.EntityFrameworkCore;

namespace API.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        public DbContext Context { get; }
        Task<int> CompleteAsync();
    }
}
