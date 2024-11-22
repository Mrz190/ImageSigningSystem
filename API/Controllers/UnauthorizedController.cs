using API.Dto;
using API.Services;
using Microsoft.AspNetCore.Authorization;
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

        [HttpPost("check-metadata")]
        public async Task<ActionResult> CheckMetadata(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is not provided or empty.");

            var fileBytes = await _imageService.ConvertToByteArrayAsync(file);

            //var fileFormat = _imageService.GetFileFormat(fileBytes);

            //if (fileFormat == "Unknown")
            //    return BadRequest("Unsupported file format. Only JPG and PNG are supported.");

            var fileFormat = _imageService.GetFileFormat(fileBytes);

            string signature = fileFormat switch
            {
                "JPG" => _imageService.ExtractSignatureFromJpgMetadata(fileBytes),
                "PNG" => _imageService.ExtractSignatureFromPngMetadata(fileBytes),
                _ => throw new NotSupportedException("Unsupported file format")
            };

            //string signature;

            if (fileFormat == "JPG")
            {
                signature = _imageService.ExtractSignatureFromJpgMetadata(fileBytes);
            }
            else if (fileFormat == "PNG")
            {
                signature = _imageService.ExtractSignatureFromPngMetadata(fileBytes);
            }
            else
            {
                return BadRequest("Unsupported file format.");
            }

            if (string.IsNullOrEmpty(signature)) return BadRequest("Signature not found in metadata.");

            return Ok(signature);
        }

        [HttpPost("verify-file-signature")]
        public async Task<ActionResult> VerifyFileSignature(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is not provided or empty.");

            var fileBytes = await _imageService.ConvertToByteArrayAsync(file);
            var fileFormat = _imageService.GetFileFormat(fileBytes);

            string signature = fileFormat switch
            {
                "JPG" => _imageService.ExtractSignatureFromJpgMetadata(fileBytes),
                "PNG" => _imageService.ExtractSignatureFromPngMetadata(fileBytes),
                _ => throw new NotSupportedException("Unsupported file format")
            };

            if (string.IsNullOrEmpty(signature)) return BadRequest("Signature not found in metadata.");

            var strippedImageData = _imageService.RemoveUserComment(fileBytes);

            bool isValid = _imageService.VerifySignature(strippedImageData, signature);
            return isValid ? Ok("Signature valid.") : BadRequest("Signature invalid.");
        }

    }
}
