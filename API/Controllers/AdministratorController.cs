using API.Data;
using API.Dto;
using API.Entity;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("Admin")]
    [Authorize(AuthenticationSchemes = "Digest", Roles = "Admin")]
    public class AdministratorController : BaseApiController
    {
        private readonly ImageService _imageService;
        private readonly DataContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;

        public AdministratorController(ImageService imageService, DataContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {
            _imageService = imageService;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("get-admin-images")]
        public async Task<ActionResult> GetAdminImages()
        {
            var images = await _imageService.GetAdminImages();
            if (images == null) return NotFound("No images found.");
            return Ok(images);
        }

        // Creating and adding signature into Exif method
        [HttpPost("sign/{imageId}")]
        public async Task<ActionResult> SignImage(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");
            var signatureOperationResult = await _imageService.SignatureOperation(image);

            if (signatureOperationResult == false) return BadRequest("Error while signing image.");

            return Ok("Image was signed, metadata was updated.");
        }

        [HttpPost("reject-signing/{imageId}")]
        public async Task<ActionResult> RejectSigingImage(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");

            var rejectingResult = await _imageService.RejectSigningImage(image);

            if (rejectingResult == false) return BadRequest("Error while rejecting image.");

            return Ok("Signing the image was rejected.");
        }

        [HttpGet("get-users")]
        public async Task<ActionResult> GetUsersList()
        {
            var userRoleUsers = await _userManager.GetUsersInRoleAsync("User");

            var usersResult = userRoleUsers.Select(user => new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }).ToList();

            return Ok(usersResult);
        }

        [HttpGet("get-support")]
        public async Task<ActionResult> GetSupportList()
        {
            var supportUsers = await _userManager.GetUsersInRoleAsync("Support");

            var usersResult = supportUsers.Select(user => new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }).ToList();

            return Ok(usersResult);
        }

        [HttpGet("get-admins")]
        public async Task<ActionResult> GetAdminsList()
        {
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");

            var usersResult = adminUsers.Select(user => new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            }).ToList();

            return Ok(usersResult);
        }

        [HttpGet("view-image/{imageId}")]
        public async Task<IActionResult> ViewImage(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");

            return File(image.ImageData, "image/png");
        }

        [HttpPost("change-role/{userId}")]
        public async Task<IActionResult> ChangeUserRole(int userId, int roleType)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return NotFound($"User with ID {userId} not found.");
            }

            string newRole = roleType switch
            {
                1 => "Admin",
                2 => "Support",
                3 => "User",
                _ => null,
            };

            if (newRole == null)
            {
                return BadRequest("Invalid role type.");
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            foreach (var role in userRoles)
            {
                var removeResult = await _userManager.RemoveFromRoleAsync(user, role);
                if (!removeResult.Succeeded)
                {
                    return BadRequest($"Error while removing role {role} from user.");
                }
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addRoleResult.Succeeded)
            {
                return BadRequest($"Error while adding role {newRole} to user.");
            }

            return Ok($"Role changed successfully to {newRole}.");
        }
    }
}