using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("auth")]
    public class AuthController : BaseApiController
    {
        [HttpGet("reg")]
        public async Task<ActionResult> Registragion()
        {
            return Ok();
        }
    }
}