using System.Security.Cryptography;
using System.Text;

namespace API.Middleware
{
    public class DigestAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _realm = "MyAppRealm";

        public DigestAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (authHeader == null || !authHeader.StartsWith("Digest", StringComparison.OrdinalIgnoreCase))
            {
                // No header or not Digest
                context.Response.StatusCode = 401;
                context.Response.Headers.Add("WWW-Authenticate", "Digest realm=\"example.com\", qop=\"auth\", algorithm=\"MD5\", nonce=\"randomNonce\", opaque=\"randomOpaque\"");
                return;
            }

            var digestParams = ParseDigestHeader(authHeader);

            if (digestParams == null || !ValidateDigest(context, digestParams))
            {
                // Challenge client with Digest
                var nonce = GenerateNonce();
                context.Response.Headers["WWW-Authenticate"] = $"Digest realm=\"{_realm}\", nonce=\"{nonce}\", algorithm=MD5";
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await _next(context);
        }

        private static string GenerateNonce()
        {
            var timeStamp = DateTime.UtcNow.ToString("o");
            var nonceData = $"{Guid.NewGuid()}:{timeStamp}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(nonceData));
        }

        private bool ValidateDigest(HttpContext context, DigestParams digestParams)
        {
            // TODO: get from database
            var passwordHash = "5f4dcc3b5aa765d61d8327deb882cf99"; // "password" hash

            var ha1 = MD5Hash($"{digestParams.Username}:{digestParams.Realm}:{passwordHash}");
            var ha2 = MD5Hash($"GET:{digestParams.Uri}");
            var expectedResponse = MD5Hash($"{ha1}:{digestParams.Nonce}:{digestParams.Nc}:{digestParams.Cnonce}:{digestParams.Qop}:{ha2}");

            return expectedResponse.Equals(digestParams.Response, StringComparison.OrdinalIgnoreCase);
        }

        private DigestParams ParseDigestHeader(string authHeader)
        {
            var parameters = authHeader.Substring(7).Trim(); // Remove "Digest: "
            var digestParams = new DigestParams();

            foreach (var param in parameters.Split(','))
            {
                var keyValue = param.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim(' ', '"');

                    switch (key.ToLower())
                    {
                        case "username":
                            digestParams.Username = value;
                            break;
                        case "realm":
                            digestParams.Realm = value;
                            break;
                        case "nonce":
                            digestParams.Nonce = value;
                            break;
                        case "uri":
                            digestParams.Uri = value;
                            break;
                        case "response":
                            digestParams.Response = value;
                            break;
                        case "qop":
                            digestParams.Qop = value;
                            break;
                        case "nc":
                            digestParams.Nc = value;
                            break;
                        case "cnonce":
                            digestParams.Cnonce = value;
                            break;
                        case "opaque":
                            digestParams.Opaque = value;
                            break;
                    }
                }
            }

            return digestParams;
        }

        private string MD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(bytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    public class DigestParams
    {
        public string Username { get; set; }
        public string Realm { get; set; }
        public string Nonce { get; set; }
        public string Uri { get; set; }
        public string Response { get; set; }
        public string Qop { get; set; }
        public string Nc { get; set; }
        public string Cnonce { get; set; }
        public string Opaque { get; set; }
    }

    public static class DigestAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseDigestAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DigestAuthenticationMiddleware>();
        }
    }
}
