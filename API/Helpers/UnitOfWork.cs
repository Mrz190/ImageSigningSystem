using API.Data;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Helpers
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DataContext _context;

        public UnitOfWork(DataContext context)
        {
            _context = context;
        }
        public DbContext Context => _context;

        public async Task<int> CompleteAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while saving changes.", ex);
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }

    }
}
