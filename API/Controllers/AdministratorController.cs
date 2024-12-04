using API.Data;
using API.Dto;
using API.Entity;
using API.Interfaces;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMailService _mailService;

        public AdministratorController(ImageService imageService, DataContext context, UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IMailService mailService, IUnitOfWork unitOfWork)
        {
            _imageService = imageService;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _mailService = mailService;
            _unitOfWork = unitOfWork;

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

            var templateMessage = new Message
            {
                MessageBody = $"<h1>Hello, {image.UploadedBy} 👋!</h1><br/><h4>Your image: '{image.ImageName}' was signed by administration. Now you can download it!</h4>"
            };

            var user = await _userManager.FindByNameAsync(image.UploadedBy);

            var templateMail = new MailRequest
            {
                MailMessage = templateMessage,
                RecipientEmail = user.Email
            };

            var notifyUser = await _mailService.SendMailAsync(templateMail);

            return notifyUser ? Ok("Image was signed, metadata was updated.") : BadRequest("Image was signed but user was'nt notified.");
        }

        [HttpPost("reject-signing/{imageId}")]
        public async Task<ActionResult> RejectSigingImage(int imageId, CommentDto commentDto)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");

            var rejectingResult = await _imageService.RejectSigningImage(image);

            if (rejectingResult == false) return BadRequest("Error while rejecting image.");

            var templateMessage = new Message { };

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
        public async Task<ActionResult> ViewImage(int imageId)
        {
            var image = await _imageService.GetImageById(imageId);
            if (image == null) return NotFound("Image not found.");

            return File(image.ImageData, "image/png");
        }

        [HttpPost("change-role/{userId}")]
        public async Task<ActionResult> ChangeUserRole(int userId, int roleType)
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

        [HttpPost("send-email")]
        public async Task<ActionResult> SendEmail(MailRequest mailRequest)
        {
            var message = await _mailService.SendMailAsync(mailRequest);
            return message ? Ok() : BadRequest();
        }

        [HttpPost("change-support-mail")]
        public async Task<ActionResult> ChangeSupportMail([FromBody] SupportEmailDto emailDto)
        {
            var dbEmail = _context.EmailSettings.FirstOrDefault();

            if (dbEmail == null)
            {
                var email = new EmailSettings
                {
                    SupportEmail = emailDto.supportEmail
                };
                _context.EmailSettings.Add(email);
            }
            else dbEmail.SupportEmail = emailDto.supportEmail;

            var changes = _unitOfWork.Context.ChangeTracker.Entries()
           .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
           .ToList();

            if (!changes.Any())
                return StatusCode(500, "Internal server error.");

            await _unitOfWork.CompleteAsync();

            return Ok("Support email was changed.");
        }

        [HttpDelete("remove-user/{userId}")]
        public async Task<ActionResult> RemoveUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return NotFound("User not found");

            var roles = await _userManager.GetRolesAsync(user);

            if(roles.Contains("Admin")) return BadRequest("You can't delete admin.");

            if(roles.Contains("User"))
            {
                var deletingImagesResult = await _imageService.DeleteAllUserImages(user.Id);
                if (deletingImagesResult == false) return BadRequest("Error while deliting images.");
            }

            if (roles.Any()) await _userManager.RemoveFromRolesAsync(user, roles);

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                var templateMessage = new Message
                {
                    Subject = "Account specifics",
                    MessageBody = $"<h1>Hello, {user.UserName} 👋!</h1><br/><p>Your account was deleted from our platform.</p>"
                };

                var templateMail = new MailRequest
                {
                    MailMessage = templateMessage,
                    RecipientEmail = user.Email
                };

                var notifyUser = await _mailService.SendMailAsync(templateMail);

                return Ok("Account deleted.");
            }
            else return BadRequest("Failed to delete account: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}