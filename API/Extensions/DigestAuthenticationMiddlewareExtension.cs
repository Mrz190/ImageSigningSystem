using API.Middleware;

namespace API.Extensions
{
    public static class DigestAuthenticationMiddlewareExtension
    {
        public static IApplicationBuilder UseDigestAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DigestAuthenticationMiddleware>();
        }
    }
}
