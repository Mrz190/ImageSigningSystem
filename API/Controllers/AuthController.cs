using API.Data;
using API.Dto;
using API.Entity;
using API.Helpers;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace API.Controllers
{
    [Route("Account")]
    public class AuthController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;
        private readonly IConfiguration _config;
        private readonly MD5Hash _MD5;

        public AuthController(UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor, IMapper mapper, DataContext context, IConfiguration config, MD5Hash _MD5)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _context = context;
            _config = config;
            this._MD5 = _MD5;
        }

        [AllowAnonymous]
        [HttpPost("Registration")]
        public async Task<ActionResult<UserDto>> Registragion(RegDto regDto)
        {
            if (await UserExists(regDto.Username))
                return BadRequest("User already exists.");

            var user = _mapper.Map<AppUser>(regDto);
            user.UserName = regDto.Username.ToLower();
            user.Email = regDto.Email;

            // Hashing password for digest
            var username = regDto.Username.ToLower();
            var realm = _config.GetValue<string>("DigestRealm");
            var ha1 = _MD5.CalculateMd5Hash($"{username}:{realm}:{regDto.Password}");

            var user_init = await _userManager.CreateAsync(user, regDto.Password);
            if (!user_init.Succeeded)
            {
                var errors = string.Join(", ", user_init.Errors.Select(e => e.Description));
                return BadRequest($"Error while registering new user: {errors}");
            }

            user.PasswordHash = ha1;
            await _userManager.UpdateAsync(user); // Update user with generated password for Digest-auth

            // Add role
            var role_existing = await _userManager.IsInRoleAsync(user, "User");
            if (!role_existing)
            {
                var role_init = await _userManager.AddToRoleAsync(user, "User");
                if (!role_init.Succeeded)
                    return BadRequest("Error while registering new user.");
            }

            var resultDto = new UserDto
            {
                UserName = user.UserName,
                Email = user.Email
            };

            return Ok(resultDto);
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult<UserDto>> Login(LogDto logDto)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(n => n.UserName == logDto.UserName);
            if (user == null)
                return BadRequest("Invalid username or password.");

            var realm = _config.GetValue<string>("DigestRealm");
            var calculatedHash = _MD5.CalculateMd5Hash($"{logDto.UserName.ToLower()}:{realm}:{logDto.Password}");

            if (user.PasswordHash != calculatedHash)
                return BadRequest("Invalid username or password.");

            var resultDto = new UserDto
            {
                UserName = user.UserName,
                Email = user.Email
            };

            return Ok(resultDto);
        }

        [AllowAnonymous]
        [HttpDelete("del/{id}")]
        public async Task<IActionResult> DelUser(int id)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(n => n.Id == id);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, roles);
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                return Ok("User deleted.");
            }
            else
            {
                return BadRequest("Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }

        [Authorize(AuthenticationSchemes = "Digest")]
        [HttpGet("test-auth")]
        public async Task<ActionResult> TestAuth()
        {
            return Ok("Test passed.");
        }

        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
    }
}