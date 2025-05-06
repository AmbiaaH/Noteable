using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NoteableApi.Data;
using NoteableApi.Helpers;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Net;

// Controller responsible for user authentication, registration, and role management
namespace NoteableApi.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AuthController> _logger;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AuthController> logger,
            IEmailService emailService,
            IConfiguration config)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailService = emailService;
            _config = config;
        }

        [HttpGet("ping")]
        // check endpoint to verify the Auth API is running
        public IActionResult Ping()
        {
            return Ok("Auth API is working!");
        }

        [HttpPost("register")]
        // Registers a new user with default Student role and returns a confirmation token
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            try
            {
                var user = new ApplicationUser 
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth,
                    Role = "Student"
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                await _userManager.AddToRoleAsync(user, "Student");

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                return Ok(new
                {
                    Message = "User registered successfully, please confirm your email to continue.",
                    UserId = user.Id,
                    Token = token
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { message = "Registration failed due to an internal error" });
            }
        }

        [HttpPost("login")]
        // Authenticates a user and returns a JWT token along with user info
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                _logger.LogInformation($"Login attempt for username: {model.Username}");

                var user = await _userManager.FindByNameAsync(model.Username);
                if (user == null)
                {
                    _logger.LogWarning($"Login failed: User not found - {model.Username}");
                    return Unauthorized("User not found.");
                }

                var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

                if (!passwordCheck.Succeeded)
                {
                    _logger.LogWarning($"Login failed for user {model.Username}: {passwordCheck.ToString()}");

                    if (passwordCheck.IsLockedOut)
                        return Unauthorized("Account is locked out.");
                    if (passwordCheck.IsNotAllowed)
                        return Unauthorized("Login not allowed.");

                    return Unauthorized("Invalid login credentials.");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.FirstOrDefault() ?? "Student";

                var token = JwtHelper.GenerateToken(user.Id, user.UserName, user.Email, role);

                _logger.LogInformation($"Successful login for user: {model.Username}");

                return Ok(new {
                    token = token,
                    user = new {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        role = role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during login for user {model.Username}");
                return StatusCode(500, new { message = "An unexpected error occurred during login" });
            }
        }

        [HttpGet("currentuser")] 
        [Authorize]
        // Returns the currently authenticated users profile
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound("User not found");

                var roles = await _userManager.GetRolesAsync(user);

                return Ok(new {
                    id = user.Id,
                    username = user.UserName,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    profilePicture = user.ProfilePicture,
                    dateOfBirth = user.DateOfBirth,
                    roles = roles,
                    role = roles.FirstOrDefault() ?? "Student"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user");
                return StatusCode(500, new { message = "An error occurred while retrieving user information" });
            }
        }

        [HttpPost("forgotpassword")]
        // Generates and emails a password reset link to the user's registered email

        
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return Ok(new { message = "If the email exists, a reset link has been sent." });
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedToken = WebUtility.UrlEncode(token);
                var frontendBaseUrl = _config["Frontend:BaseUrl"];
                var resetUrl = $"{frontendBaseUrl}/reset-password?email={user.Email}&token={encodedToken}";

                await _emailService.SendEmailAsync(
                    user.Email,
                    "Reset Your Password",
                    $"<p>Click <a href='{resetUrl}'>here</a> to reset your password.</p>");

                return Ok(new { message = "If the email exists, a reset link has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing forgot password request");
                return StatusCode(500, new { message = "Forgot password request failed due to an internal error" });
            }
        }

        [HttpPost("resetpassword")]
        // Resets the users password using the provided token

        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Token) || string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    return BadRequest(new { message = "Email, token, and new password are required." });
                }

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return Ok(new { message = "Password reset process completed." });
                }

                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new { errors = result.Errors });
                }

                return Ok(new { message = "Your password has been reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new { message = "Password reset failed due to an internal error." });
            }
        }

        [HttpPost("assignadmin")]
        [Authorize(Roles = "Admin")]
        // Assigns the 'admin' role to a user and requires admin authorization.

        public async Task<IActionResult> AssignAdminRole([FromBody] AssignRoleModel model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user == null)
                {
                    return BadRequest("User not found.");
                }

                var result = await _userManager.AddToRoleAsync(user, "Admin");
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                user.Role = "Admin";
                await _userManager.UpdateAsync(user);

                return Ok("Admin role assigned successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning admin role");
                return StatusCode(500, new { message = "Failed to assign admin role due to an internal error" });
            }
        }

        [HttpPost("assignrole")]
        [Authorize(Roles = "Admin")]
        // Assigns a specified role Admin / Student to a user and requires admin authorization
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleWithTypeModel model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.Username);
                if (user == null)
                {
                    return BadRequest("User not found.");
                }

                if (!new[] { "Admin", "Mentor", "Student" }.Contains(model.Role))
                {
                    return BadRequest("Invalid role specified.");
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, userRoles);
                var result = await _userManager.AddToRoleAsync(user, model.Role);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                user.Role = model.Role;
                await _userManager.UpdateAsync(user);

                return Ok($"{model.Role} role assigned successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role");
                return StatusCode(500, new { message = "Failed to assign role due to an internal error" });
            }
        }

        //Models


        public class AssignRoleModel // Model for admin assignment
        {
            public string Username { get; set; } = string.Empty;
        }

        public class AssignRoleWithTypeModel // Model for role assigment
        {
            public string Username { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }
    }

    public class RegisterModel //Model for registering a new user
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;  
        public DateTime DateOfBirth { get; set; }
    }

    public class LoginModel // Model for user login credentials
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ConfirmEmailModel // Model used for confirming user email
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class ForgotPasswordModel // Model used for requesting a password reset
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordModel // Model for resetting a users password with token
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
