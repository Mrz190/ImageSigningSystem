using Microsoft.EntityFrameworkCore;

namespace API.Intefaces
{
    public interface IUnitOfWork : IDisposable
    {
        public DbContext Context { get; }
        Task<int> CompleteAsync();
    }
}
