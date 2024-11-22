using API.Dto;
using API.Entity;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [Route("User")]
    [Authorize(AuthenticationSchemes = "Digest", Roles = "User")]
    public class UserController : BaseApiController
    {
        private readonly ImageService _imageService;
        private readonly UserManager<AppUser> _userManager;
        public UserController(ImageService imageService, UserManager<AppUser> userManager)
        {
            _imageService = imageService;
            _userManager = userManager;
        }

        [HttpGet("user-images")]
        public async Task<ActionResult> GetUserImages()
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

        [HttpGet("signed-images")]
        public async Task<ActionResult> GetUserSignedImages()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized("User not found.");
            }

            var images = await _imageService.GetSignedImagesForUser(userId);

            if (images == null)
            {
                return NotFound("No images found for this user.");
            }

            return Ok(images);
        }

        [HttpGet("rejected-images")]
        public async Task<ActionResult> GetUserRejectedImages()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0)
            {
                return Unauthorized("User not found.");
            }

            var images = await _imageService.GetRejectedImagesForUser(userId);

            if (images == null)
            {
                return NotFound("No images found for this user.");
            }

            return Ok(images);
        }

        [HttpPost("upload")]
        public async Task<ActionResult> UploadImage(IFormFile file)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userId == 0) return Unauthorized("User not found.");

            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

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
                        Status = ImageStatus.AwaitingSignature.ToString(),
                        UploadedBy = userName
                    };

                    var signImageChecker = await _imageService.UploadSendImageForSigningToSupport(signedImage);

                    if (signImageChecker != true) return BadRequest("Error while uploading image.");

                    return Ok("Image uploaded.");
                }
            }

            return BadRequest("Image not provided.");
        }

        // Download image method
        [HttpGet("download/{id}")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<ActionResult> DownloadImage(int id)
        {
            var image = await _imageService.GetImageById(id);
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            if (image == null) return NotFound("Image not found.");

            if (image.UploadedBy != userName) return NotFound($"Image not found.");

            var result = image;

            //_imageService.DeleteImage(image);

            return File(image.ImageData, "image/jpeg", "downloaded_image.jpg");
        }

        // Downloading original image method
        [HttpGet("download-without-exif/{id}")]
        public async Task<ActionResult> DownloadImageWithoutExif(int id)
        {
            var image = await _imageService.GetImageById(id);
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            if (image == null) return NotFound("Image not found.");

            if (image.UploadedBy != userName) return NotFound($"Image not found.");

            return File(image.StrippedData, "image/jpeg");
        }

        // Verify signature
        [HttpGet("verify-signature/{imageId}")]
        public async Task<ActionResult> VerifyImageSignature(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");

            // Extract signature from metadata
            var signature = _imageService.ExtractSignatureFromImageMetadata(image.ImageData);
            if (string.IsNullOrEmpty(signature)) return BadRequest("Signature not found in metadata.");

            // Verify original data without Exif
            bool isValid = _imageService.VerifySignature(image.StrippedData, signature);
            return isValid ? Ok("Signature valid.") : BadRequest("Signature invalid.");
        }

        // Extracting metadata from signature
        [HttpGet("get-signature/{imageId}")]
        public async Task<ActionResult> GetSignatureFromImageMetadata(int imageId)
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

        [HttpDelete("delete-image/{id}")]
        public async Task<ActionResult> DeleteImage(int id)
        {
            var resultDeleting = await _imageService.DeleteImage(id);
            if (resultDeleting == false) return BadRequest("Error while deliting image.");
            return Ok("Image deleted.");
        }

        [HttpGet("get-user-data")]
        public async Task<ActionResult> GetUserData()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized("User not found.");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return Unauthorized("User not found.");

            var userResultData = new
            {
                id = user.Id,
                userName = user.UserName,
                email = user.Email
            };

            return Ok(userResultData);
        }
    }
}
