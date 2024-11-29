namespace API.Interfaces
{
    public interface IDigestAuthenticationService
    {
        string GenerateNonce();
        bool ValidateDigest(Dictionary<string, string> digestValues, string serverNonce, HttpContext context);
    }
}
