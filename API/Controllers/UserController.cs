using API.Data;
using API.Dto;
using API.Entity;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("User")]
    [Authorize(AuthenticationSchemes = "Digest", Roles = "User")]
    public class UserController : BaseApiController
    {
        private readonly ImageService _imageService;
        public UserController(ImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpGet("user-images")]
        public async Task<IActionResult> GetUserImages()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized("User not found.");
            }

            var images = await _imageService.GetUserImages(userId);

            if (images == null)
            {
                return NotFound("No images found for this user.");
            }

            return Ok(images);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized("User not found.");
            }

            if (file.Length > 0)
            {
                var fileName = Path.GetFileName(file.FileName);

                var fileExtension = Path.GetExtension(fileName);

                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var originalImageData = memoryStream.ToArray();
                    byte[] strippedImageData = _imageService.StripExif(originalImageData);

                    var signedImage = new SignedImage
                    {
                        ImageData = originalImageData,
                        StrippedData = strippedImageData,
                        Signature = null,
                        UserId = userId,
                        ImageName = fileName, 
                        Status = ImageStatus.AwaitingSignature
                    };

                    var signImageChecker = await _imageService.UploadSendImageForSigningToSupport(signedImage);

                    if (signImageChecker != true) return BadRequest("Error while uploading image.");

                    return Ok("Image uploaded.");
                }
            }

            return BadRequest("Image not provided.");
        }

        // Download image method
        [Authorize(AuthenticationSchemes = "Digest")]
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadImage(int id)
        {
            var image = await _imageService.GetImageById(id);

            if (image == null) return NotFound("Image not found.");

            return File(image.ImageData, "image/png");
        }

        // Downloading original image method
        [Authorize(AuthenticationSchemes = "Digest")]
        [HttpGet("download-without-exif/{id}")]
        public async Task<IActionResult> DownloadImageWithoutExif(int id)
        {
            var image = await _imageService.GetImageById(id);

            if (image == null) return NotFound("Image not found.");

            return File(image.StrippedData, "image/png");
        }

        // Verify signature
        [HttpPost("verify-signature/{imageId}")]
        public async Task<IActionResult> VerifyImageSignature(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");

            // Extract signature from metadata
            var signature = _imageService.ExtractSignatureFromImageMetadata(image.ImageData);
            if (string.IsNullOrEmpty(signature)) return BadRequest("Signature not found in metadata.");

            // Vervify original data without Exif
            bool isValid = _imageService.VerifySignature(image.StrippedData, signature);
            return isValid ? Ok("Signature valid.") : BadRequest("Signature invalid.");
        }

        // Extracting metadata from signature
        [HttpGet("get-signature/{imageId}")]
        public async Task<IActionResult> GetSignatureFromImageMetadata(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");

            var signature = _imageService.ExtractSignatureFromImageMetadata(image.ImageData);

            if (string.IsNullOrEmpty(signature))
            {
                return NotFound("Signature not found in metadata.");
            }

            return Ok(signature);
        }
    }
}
