using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace API.Middleware
{
    // Middleware class to handle Digest Authentication
    public class DigestAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IConfiguration _config;
        public DigestAuthenticationMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory, IConfiguration config)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
            _config = config;
        }

        public async Task Invoke(HttpContext context)
        {
            var endpoint = context.GetEndpoint();

            // Check [AllowAnonymous]
            if (endpoint?.Metadata?.GetMetadata<AllowAnonymousAttribute>() != null)
            {
                await _next(context);
                return;
            }

            // Read the Authorization header from the request
            var authorizationHeader = context.Request.Headers["Authorization"].ToString();

            // If no Authorization header is present or it's not a Digest auth, challenge the request
            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Digest "))
            {
                await ChallengeAsync(context); // Send a 401 Unauthorized response
                return;
            }

            // Parse the Digest Authentication header
            var digestValues = ParseDigestHeader(authorizationHeader);
            if (digestValues == null)
            {
                await ChallengeAsync(context); // Send a 401 Unauthorized response
                return;
            }

            // Generate a server nonce
            var serverNonce = GenerateNonce();

            // Validate the Digest header by comparing hashes
            if (!ValidateDigest(digestValues, serverNonce, context))
            {
                await ChallengeAsync(context); // Send a 401 Unauthorized response
                return;
            }

            await _next(context);
        }

        private async Task ChallengeAsync(HttpContext context)
        {
            var nonce = GenerateNonce();
            context.Response.Headers["WWW-Authenticate"] = $"Digest realm=\"{_config["DigestRealm"]}\", qop=\"auth\", nonce=\"{nonce}\", opaque=\"\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Authentication required");
        }

        private string GenerateNonce()
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(DateTime.Now.Ticks.ToString()));
        }

        private bool ValidateDigest(Dictionary<string, string> digestValues, string serverNonce, HttpContext context)
        {
            var username = digestValues["username"];

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var _userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var passwordHashFromDb = _userRepository.GetUserPasswordFromDatabaseByName(username).Result;
                var _MD5 = scope.ServiceProvider.GetRequiredService<MD5Hash>();

                var realm = _config.GetValue<string>("DigestRealm");
                var ha1 = passwordHashFromDb;

                var method = context.Request.Method;
                var a2 = $"{method}:{digestValues["uri"]}";
                var ha2 = _MD5.CalculateMd5Hash(a2);

                var expectedResponse = _MD5.CalculateMd5Hash($"{ha1}:{digestValues["nonce"]}:{digestValues["nc"]}:{digestValues["cnonce"]}:{digestValues["qop"]}:{ha2}");
                return expectedResponse == digestValues["response"];
            }
        }

        private Dictionary<string, string> ParseDigestHeader(string authorizationHeader)
        {
            var values = new Dictionary<string, string>();
            var digestData = authorizationHeader.Substring("Digest ".Length);
            var parts = digestData.Split(',');

            foreach (var part in parts)
            {
                var kvp = part.Split(new[] { '=' }, 2);
                if (kvp.Length == 2)
                {
                    var key = kvp[0].Trim();
                    var value = kvp[1].Trim(' ', '"');
                    values[key] = value;
                }
            }
            return values;
        }
    }

    // Class to hold the configuration options for Digest Authentication
    public class DigestAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string Realm { get; set; } = "MyRealm";
    }

    // Handler class to process Digest Authentication requests
    public class DigestAuthenticationHandler : AuthenticationHandler<DigestAuthenticationOptions>
    {
        private readonly IUserRepository _userRepository;

        public DigestAuthenticationHandler(
            IOptionsMonitor<DigestAuthenticationOptions> options,
            ILoggerFactory logger,
            System.Text.Encodings.Web.UrlEncoder encoder,
            ISystemClock clock,
            IUserRepository userRepository) : base(options, logger, encoder, clock)
        {
            _userRepository = userRepository;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.NoResult();
            }

            var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);

            if (authHeader.Scheme != "Digest")
            {
                return AuthenticateResult.Fail("Invalid authentication scheme");
            }

            var digestValues = ParseDigestHeader(Request.Headers["Authorization"]);

            var username = digestValues["username"];
            var user = await _userRepository.GetUserByUsernameAsync(username);

            if (user == null)
            {
                return AuthenticateResult.Fail("Invalid username or password.");
            }

            var roles = await _userRepository.GetUserRolesAsync(user);

            var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName)
        };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Headers["WWW-Authenticate"] = $"Digest realm=\"{Options.Realm}\", qop=\"auth\"";
            Response.StatusCode = 401;
        }

        private Dictionary<string, string> ParseDigestHeader(string authorizationHeader)
        {
            var values = new Dictionary<string, string>();
            var digestData = authorizationHeader.Substring("Digest ".Length);
            var parts = digestData.Split(',');

            foreach (var part in parts)
            {
                var kvp = part.Split(new[] { '=' }, 2);
                if (kvp.Length == 2)
                {
                    var key = kvp[0].Trim();
                    var value = kvp[1].Trim(' ', '"');
                    values[key] = value;
                }
            }
            return values;
        }
    }
}
