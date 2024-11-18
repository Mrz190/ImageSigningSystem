﻿using API.Data;
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

        [HttpGet("get-support-images")]
        public async Task<IActionResult> GetSupportImages()
        {
            var images = await _imageService.GetSupportImages();
            if (images != null) return Ok(images);
            return NotFound("No images founded.");
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