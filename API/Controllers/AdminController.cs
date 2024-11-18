using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("Admin")]
    [Authorize(AuthenticationSchemes = "Digest", Roles = "Admin")]
    public class AdminController : BaseApiController
    {
        private readonly ImageService _imageService;
        public AdminController(ImageService imageService)
        {
            _imageService = imageService;
        }

        // Creating and adding signature into Exif method
        [Authorize(AuthenticationSchemes = "Digest", Roles = "Admin")]
        [HttpPost("sign/{imageId}")]
        public async Task<IActionResult> SignImage(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null)
            {
                return NotFound("Image not found.");
            }

            var signatureOperationResult = _imageService.SignatureOperation(image);

            return signatureOperationResult == null
                   ? BadRequest("Error while signing image.")
                   : Ok("Image was signed, metadata was updated.");
        }
    }
}
