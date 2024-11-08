using API.Data;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class SupportController : BaseApiController
    {

        //public SupportController(IImageRepository imageRepository)
        //{
        //    _imageRepository = imageRepository;
        //}

        [HttpGet("test")]
        public async Task<ActionResult> Test()
        {
            return Ok("");
        }
    }
}
