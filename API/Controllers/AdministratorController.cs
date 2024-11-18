using API.Data;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("Administrator")]
    [Authorize(AuthenticationSchemes = "Digest", Roles = "Admin")]
    public class AdministratorController : BaseApiController
    {
        private readonly ImageService _imageService;
        private readonly DataContext _context;

        public AdministratorController(ImageService imageService, DataContext context)
        {
            _imageService = imageService;
            _context = context;
        }

        [HttpGet("get-admin-images")]
        public async Task<IActionResult> GetAdminImages()
        {
            var images = await _imageService.GetAdminImages();
            if (images != null) return Ok(images);
            return NotFound("No images founded.");
        }

        // Creating and adding signature into Exif method
        [HttpPost("sign/{imageId}")]
        public async Task<IActionResult> SignImage(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");
            var signatureOperationResult = await _imageService.SignatureOperation(image);

            if(signatureOperationResult == false) return BadRequest("Error while signing image.");
            
            return Ok("Image was signed, metadata was updated.");
        }

        [HttpPost("reject-signing/{id}")]
        public async Task<IActionResult> RejectSigingImage(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");

            var signatureOperationResult = await _imageService.SignatureOperation(image);

            if (signatureOperationResult == false) return BadRequest("Error while rejecting signing the image.");

            return Ok("Image was signed, metadata was updated.");
        }
    }
}
