using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("Account")]
    public class AuthController : BaseApiController
    {
        [HttpGet("reg")]
        public async Task<ActionResult> Registragion()
        {
            return Ok("Test passed.");
        }

        [Authorize]
        [HttpGet("test-auth")]
        public async Task<ActionResult> TestAuth()
        {
            return Ok("Test passed.");
        }
    }
}