using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [AllowAnonymous]
    public class UnauthorizedController : BaseApiController
    {
        public UnauthorizedController()
        {
            
        }


    }
}
