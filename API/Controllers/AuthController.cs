using API.Data;
using API.Dto;
using API.Entity;
using API.Helpers;
using API.Interfaces;
using API.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;

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
        private readonly IDigestAuthenticationService _digestAuthenticationService;
        private readonly ImageService _imageService;
        public AuthController(UserManager<AppUser> userManager, IDigestAuthenticationService digestAuthenticationService, IHttpContextAccessor httpContextAccessor, IMapper mapper, DataContext context, IConfiguration config, MD5Hash _MD5, ImageService imageService)
        {
            _userManager = userManager;
            _digestAuthenticationService = digestAuthenticationService;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
            _context = context;
            _config = config;
            this._MD5 = _MD5;
            _imageService = imageService;
        }

        [AllowAnonymous]
        [HttpPost("Registration")]
        public async Task<ActionResult<UserDto>> Registragion(RegDto regDto)
        {
            if (await UserExists(regDto.Username)) return BadRequest("User with this name already exists.");
            if (await UserWithEmailExists(regDto.Email)) return BadRequest("User with this email already exists.");
            if (regDto.Username.Length < 3) return BadRequest("Username must be more than 3 symbols.");

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
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            };

            return Ok(resultDto);
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<ActionResult<UserDto>> Login(LogDto logDto)
        {
            var authorizationHeader = Request.Headers["Authorization"].ToString();

            if (string.IsNullOrEmpty(authorizationHeader) || !authorizationHeader.StartsWith("Digest "))
            {
                var nonce = _digestAuthenticationService.GenerateNonce();
                Response.Headers["WWW-Authenticate"] = $"Digest realm=\"{_config["DigestRealm"]}\", qop=\"auth\", nonce=\"{nonce}\", opaque=\"\"";
                return Unauthorized("Authorization header is missing or invalid.");
            }

            var digestValues = ParseDigestHeader(authorizationHeader);

            var username = digestValues["username"];
            var user = await _userManager.Users.SingleOrDefaultAsync(n => n.UserName == logDto.UserName);

            if (user == null)
            {
                var nonce = _digestAuthenticationService.GenerateNonce();
                Response.Headers["WWW-Authenticate"] = $"Digest realm=\"{_config["DigestRealm"]}\", qop=\"auth\", nonce=\"{nonce}\", opaque=\"\"";
                return Unauthorized("Invalid username or password.");
            }

            if (!_digestAuthenticationService.ValidateDigest(digestValues, _digestAuthenticationService.GenerateNonce(), HttpContext))
            {
                var nonce = _digestAuthenticationService.GenerateNonce();
                Response.Headers["WWW-Authenticate"] = $"Digest realm=\"{_config["DigestRealm"]}\", qop=\"auth\", nonce=\"{nonce}\", opaque=\"\"";
                return Unauthorized("Invalid Digest response.");
            }

            var resultDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
            };

            return Ok(resultDto);
        }

        private Dictionary<string, string> ParseDigestHeader(string authorizationHeader)
        {
            var values = new Dictionary<string, string>();
            var digestData = authorizationHeader.Substring("Digest ".Length);
            var parts = digestData.Split(',');

            foreach (var part in parts)
            {
                var kvp = part.Split(new[] { '=' }, 2);
                if (kvp.Length == 2)
                {
                    var key = kvp[0].Trim();
                    var value = kvp[1].Trim(' ', '"');
                    values[key] = value;
                }
            }
            return values;
        }

        [AllowAnonymous]
        [HttpGet("LoginNonce")]
        public IActionResult GetLoginNonce()
        {
            var nonce = _digestAuthenticationService.GenerateNonce();
            return Ok(new
            {
                nonce = nonce,
                realm = _config.GetValue<string>("DigestRealm"),
                opaque = ""
            });
        }

        private string GenerateNonce()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[16];
                rng.GetBytes(randomBytes);
                return Convert.ToBase64String(randomBytes);
            }
        }

        [Authorize(AuthenticationSchemes = "Digest", Roles = "User,Support,Admin")]
        [HttpDelete("delete-user/{userId}")]
        public async Task<ActionResult> DelUser(int userId, [FromBody] PasswordRequest password)
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var user = await _userManager.Users.SingleOrDefaultAsync(n => n.Id == userId);

            if (user.UserName != userName) return StatusCode(500, "Server error.");

            if (user == null) return NotFound("User not found.");

            var realm = _config.GetValue<string>("DigestRealm");
            var ha1 = _MD5.CalculateMd5Hash($"{user.UserName}:{realm}:{password.Password}");

            if (user.PasswordHash != ha1) return BadRequest("Password does not match.");

            var deletingImagesResult = await _imageService.DeleteAllUserImages(user.Id);
            if (deletingImagesResult == false) return BadRequest("Error while deliting images.");

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Any()) await _userManager.RemoveFromRolesAsync(user, roles);

            var result = await _userManager.DeleteAsync(user);

            if(result.Succeeded) return Ok("Account deleted.");
            else return BadRequest("Failed to delete account: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        [Authorize(AuthenticationSchemes = "Digest", Roles = "User,Admin,Support")]
        [HttpPut("change-data")]
        public async Task<ActionResult> EditUserData([FromBody] EditUserDataDto dataDto)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            if (user == null) return Unauthorized("User not found");

            if (string.IsNullOrWhiteSpace(dataDto.username) || string.IsNullOrWhiteSpace(dataDto.email)) return BadRequest("Username and Email cannot be empty.");

            if (!IsValidEmail(dataDto.email)) return BadRequest("Invalid email format.");

            var userWithSameEmail = await _userManager.FindByEmailAsync(dataDto.email);
            if (userWithSameEmail != null && userWithSameEmail.Id != user.Id) return Conflict("Email is already in use.");

            var userWithSameUsername = await _userManager.FindByNameAsync(dataDto.username);
            if (userWithSameUsername != null && userWithSameUsername.Id != user.Id) return Conflict("Username is already in use.");

            user.UserName = dataDto.username;
            user.Email = dataDto.email;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded) return Ok("User data updated successfully.");

            return BadRequest("Failed to update user data.");
        }

        [Authorize(AuthenticationSchemes = "Digest", Roles = "User,Admin,Support")]
        [HttpGet("get-data")]
        public async Task<ActionResult> GetUserData()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);

            if (user == null) return Unauthorized("User not found");

            var result = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email
            };

            return Ok(result);
        }

        [AllowAnonymous]
        [HttpOptions("Login")]
        public IActionResult Options()
        {
            Response.Headers.Add("Access-Control-Allow-Origin", "*");
            Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            Response.Headers.Add("Access-Control-Allow-Headers", "X-Requested-With, Content-Type");
            return Ok();
        }

        private bool IsValidEmail(string email)
        {
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
            return emailRegex.IsMatch(email);
        }

        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(x => x.UserName == username.ToLower());
        }
        
        private async Task<bool> UserWithEmailExists(string email)
        {
            return await _userManager.Users.AnyAsync(x => x.NormalizedEmail == email.ToUpper());
        }

        public class PasswordRequest
        {
            public string Password { get; set; }
        }
    }
}