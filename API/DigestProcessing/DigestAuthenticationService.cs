using API.Helpers;
using API.Interfaces;
using System.Text;

namespace API.DigestProcessing
{
    public class DigestAuthenticationService : IDigestAuthenticationService
    {
        private readonly IConfiguration _config;
        private readonly IUserRepository _userRepository;
        private readonly MD5Hash _MD5;

        public DigestAuthenticationService(IConfiguration config, IUserRepository userRepository, MD5Hash MD5)
        {
            _config = config;
            _userRepository = userRepository;
            _MD5 = MD5;
        }

        public string GenerateNonce()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(DateTime.Now.Ticks.ToString()));
        }

        public bool ValidateDigest(Dictionary<string, string> digestValues, string serverNonce, HttpContext context)
        {
            var username = digestValues["username"];

            var passwordHashFromDb = _userRepository.GetUserPasswordFromDatabaseByName(username).Result;
            var realm = _config.GetValue<string>("DigestRealm");
            var ha1 = passwordHashFromDb;

            var method = context.Request.Method;
            var a2 = $"{method}:{digestValues["uri"]}";
            var ha2 = _MD5.CalculateMd5Hash(a2);

            var expectedResponse = _MD5.CalculateMd5Hash($"{ha1}:{digestValues["nonce"]}:{digestValues["nc"]}:{digestValues["cnonce"]}:{digestValues["qop"]}:{ha2}");
            return expectedResponse == digestValues["response"];
        }
    }
}
