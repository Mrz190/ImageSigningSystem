using API.Data;
using API.Dto;
using API.Entity;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("Support")]
    [Authorize(AuthenticationSchemes = "Digest", Roles = "Support")]
    public class SupportController : BaseApiController
    {
        private readonly ImageService _imageService;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMailService _mailService;
        private readonly DataContext _context;

        public SupportController(ImageService imageService, DataContext context, UserManager<AppUser> userManager, IMailService mailService)
        {
            _imageService = imageService;
            _context = context;
            _userManager = userManager;
            _mailService = mailService;
        }

        [HttpGet("get-support-images")]
        public async Task<IActionResult> GetSupportImages()
        {
            var images = await _imageService.GetSupportImages();
            if (images == null) return NotFound("No images found for this user.");
            return Ok(images);
        }

        [HttpPost("request-signature/{imageId}")]
        public async Task<IActionResult> RequestSignature(int imageId)
        {
            var image = await _context.SignedImages.FindAsync(imageId);
            if (image == null || image.Status != ImageStatus.AwaitingSignature.ToString())
            {
                return NotFound("Image not found or already signed.");
            }

            image.Status = ImageStatus.PendingAdminSignature.ToString();
            await _context.SaveChangesAsync();

            return Ok("Signature request sent to Admin.");
        }

        [HttpPost("reject-signing/{imageId}")]
        public async Task<IActionResult> RejectSigingImage(int imageId, CommentDto commentDto)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");

            var rejectingResult = await _imageService.RejectSigningImage(image);

            if (rejectingResult == false) return BadRequest("Error while rejecting image.");

            var templateMessage = new Message{ };

            if (commentDto.Comment.Length > 0)
            {
                templateMessage = new Message
                {
                    MessageBody = $"<h1>Hello, {image.UploadedBy} 👋!</h1><br/><p>You've been denied an image signature for your image {image.ImageName}</p><br/><h4>Comment:<br/>{commentDto.Comment}</h4>"
                };
            }
            else
            {
                templateMessage = new Message
                {
                    MessageBody = $"<h1>Hello, {image.UploadedBy} 👋!</h1><br/><p>You've been denied an image signature for your image {image.ImageName}</p>"
                };
            }

            var user = await _userManager.FindByNameAsync(image.UploadedBy);

            var templateMail = new MailRequest
            {
                MailMessage = templateMessage,
                RecipientEmail = user.Email
            };

            var notifyUser = await _mailService.SendMailAsync(templateMail);

            return notifyUser ? Ok("Signing the image was rejected.") : BadRequest("Signing the image was rejected but user was not notified.");
        }

        [HttpGet("view-image/{imageId}")]
        public async Task<IActionResult> ViewImage(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");
            if(image.Status != ImageStatus.AwaitingSignature.ToString())
            {
                return BadRequest("You haven't permission for this.");
            }

            return File(image.ImageData, "image/png");
        }
    }
}
