using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [AllowAnonymous]
    [Route("Unauthorized")]
    public class UnauthorizedController : BaseApiController
    {
        private readonly ImageService _imageService;

        public UnauthorizedController(ImageService imageService)
        {
            _imageService = imageService;
        }

        [HttpPost("find-signature")]
        public async Task<ActionResult> CheckMetadata(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is not provided or empty.");

            var fileBytes = await _imageService.ConvertToByteArrayAsync(file);

            var fileFormat = _imageService.GetFileFormat(fileBytes);
            if (fileFormat != "PNG")
                return BadRequest("Unsupported file format. Only PNG files are supported.");

            var signature = _imageService.ExtractSignatureFromPngMetadata(fileBytes);
            if (string.IsNullOrEmpty(signature))
                return BadRequest("Signature not found in metadata.");

            if (string.IsNullOrEmpty(signature))
                return BadRequest("Signature not found in metadata.");

            return Ok(signature);
        }

        [HttpPost("verify-file-signature")]
        public async Task<ActionResult> VerifyFileSignature(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is not provided or empty.");

            var fileBytes = await _imageService.ConvertToByteArrayAsync(file);

            var fileFormat = _imageService.GetFileFormat(fileBytes);
            if (fileFormat != "PNG")
                return BadRequest("Unsupported file format. Only PNG files are supported.");

            var signature = _imageService.ExtractSignatureFromPngMetadata(fileBytes);
            if (string.IsNullOrEmpty(signature))
                return BadRequest("Signature not found in metadata.");

            // Removing signature for verification
            var strippedImageData = _imageService.RemoveMetadata(fileBytes);

            bool isValid = _imageService.VerifySignature(strippedImageData, signature);
            return isValid ? Ok("Signature valid.") : BadRequest("Signature invalid.");
        }
    }
}
