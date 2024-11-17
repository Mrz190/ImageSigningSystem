using API.Data;
using API.Entity;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("Image")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly ImageService _imageService;

        public ImageController(DataContext context, ImageService imageService)
        {
            _context = context;
            _imageService = imageService;
        }

        [Authorize(AuthenticationSchemes = "Digest")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    var originalImageData = memoryStream.ToArray();

                    // Removing Exif before signing
                    byte[] strippedImageData = _imageService.StripExif(originalImageData);

                    var signedImage = new SignedImage
                    {
                        ImageData = originalImageData,
                        StrippedData = strippedImageData, // Original data without Exif for signing and verifying
                        Signature = null
                    };

                    _context.SignedImages.Add(signedImage);
                    await _context.SaveChangesAsync();

                    return Ok("Изображение успешно загружено.");
                }
            }

            return BadRequest("Изображение не предоставлено.");
        }

        // Download image method
        [Authorize(AuthenticationSchemes = "Digest")]
        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadImage(int id)
        {
            var image = await _context.SignedImages.FindAsync(id);

            if (image == null)
            {
                return NotFound("Изображение не найдено.");
            }

            return File(image.ImageData, "image/png");
        }

        // Downloading original image method
        [Authorize(AuthenticationSchemes = "Digest")]
        [HttpGet("download-without-exif/{id}")]
        public async Task<IActionResult> DownloadImageWithoutExif(int id)
        {
            var image = await _context.SignedImages.FindAsync(id);
            
            if (image == null) return NotFound("Изображение не найдено.");

            return File(image.StrippedData, "image/png");
        }

        // Creating and adding signature into Exif method
        [Authorize(AuthenticationSchemes = "Digest")]
        [HttpPost("sign/{imageId}")]
        public async Task<IActionResult> SignImage(int imageId)
        {
            var image = await _context.SignedImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound("Изображение не найдено.");
            }

            var signature = _imageService.SignImageData(image.StrippedData);

            // Signature in Exif of original data
            byte[] signedImageData = _imageService.AddSignatureToImageMetadata(image.ImageData, signature);

            image.ImageData = signedImageData;
            image.Signature = signature;

            await _context.SaveChangesAsync();

            return Ok("Изображение подписано и метаданные обновлены.");
        }

        // Verify signature
        [Authorize(AuthenticationSchemes = "Digest")]
        [HttpPost("verify-signature/{imageId}")]
        public async Task<IActionResult> VerifyImageSignature(int imageId)
        {
            var image = await _context.SignedImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound("Изображение не найдено.");
            }

            // Extract signature from metadata
            var signature = _imageService.ExtractSignatureFromImageMetadata(image.ImageData);
            if (string.IsNullOrEmpty(signature))
            {
                return BadRequest("Подпись не найдена в метаданных.");
            }

            // Vervify original data without Exif
            bool isValid = _imageService.VerifySignature(image.StrippedData, signature);
            return isValid ? Ok("Подпись верна.") : BadRequest("Подпись неверна.");
        }
        
        // Extracting metadata from signature
        [Authorize(AuthenticationSchemes = "Digest")]
        [HttpGet("get-signature/{imageId}")]
        public async Task<IActionResult> GetSignatureFromImageMetadata(int imageId)
        {
            var image = await _context.SignedImages.FindAsync(imageId);
            if (image == null)
            {
                return NotFound("Изображение не найдено.");
            }

            var signature = _imageService.ExtractSignatureFromImageMetadata(image.ImageData);

            if (string.IsNullOrEmpty(signature))
            {
                return NotFound("Подпись не найдена в метаданных.");
            }

            return Ok(signature);
        }
    }
}
