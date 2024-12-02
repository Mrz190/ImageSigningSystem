using API.Data;

namespace API.Services
{
    public class EmailService
    {
        private readonly DataContext _context;

        public EmailService(DataContext context)
        {
            _context = context;
        }

        public string GetSupportEmail()
        {
            var emailSettings = _context.EmailSettings.FirstOrDefault();
            return emailSettings?.SupportEmail ?? "default@example.com";
        }
    }
}
