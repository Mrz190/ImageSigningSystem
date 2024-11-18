using API.Data;
using API.Entity;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("Support")]
    [Authorize(AuthenticationSchemes = "Digest", Roles = "Support")]
    public class SupportController : BaseApiController
    {
        private readonly ImageService _imageService;
        private readonly DataContext _context;

        public SupportController(ImageService imageService, DataContext context)
        {
            _imageService = imageService;
            _context = context;
        }

        [HttpPost("request-signature/{imageId}")]
        public async Task<IActionResult> RequestSignature(int imageId)
        {
            var image = await _context.SignedImages.FindAsync(imageId);
            if (image == null || image.Status != ImageStatus.AwaitingSignature)
            {
                return NotFound("Image not found or already signed.");
            }

            image.Status = ImageStatus.PendingAdminSignature;
            await _context.SaveChangesAsync();

            return Ok("Signature request sent to Admin.");
        }
    }
}
