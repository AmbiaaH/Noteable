using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NoteableApi.Data;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IO;
using System;

namespace NoteableApi.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserController> _logger;

        public UserController(
            UserManager<ApplicationUser> userManager,
            ILogger<UserController> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        // Retrieves user details by ID and syncs role with ASP.NET Identity if out of sync.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Ensure role is in sync
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "User";
            
            if (user.Role != primaryRole)
            {
                user.Role = primaryRole;
                await _userManager.UpdateAsync(user);
            }

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.DateOfBirth,
                Role = primaryRole, // Return insync role
                user.ProfilePicture,
                user.UserName
            });
        }

        // Retrieves basic user details (ID username profile picture) by username
        [HttpGet("byUsername/{username}")]
        public async Task<IActionResult> GetUserByUsername(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(new
            {
                user.Id,
                user.UserName,
                user.ProfilePicture
            });
        }

        // Returns profile info for the currently authenticated user
        // synchronizes the users role field with their identity role
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try 
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { message = "No user ID claim present." });

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                // Get the users roles from ASP.NET Identity
                var roles = await _userManager.GetRolesAsync(user);
                
                // Get the primary role (or default to User if none exists)
                var primaryRole = roles.FirstOrDefault() ?? "User";
                
                // If the users role property doesnt match the Identity role -> update it
                if (user.Role != primaryRole)
                {
                    _logger.LogInformation($"Synchronizing user {user.UserName} role from '{user.Role}' to '{primaryRole}'");
                    user.Role = primaryRole;
                    await _userManager.UpdateAsync(user);
                }

                return Ok(new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.DateOfBirth,
                    Role = primaryRole, // Use the primary role from ASP.NET Identity
                    user.ProfilePicture,
                    user.UserName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user");
                return StatusCode(500, new { message = "An error occurred while retrieving user data." });
            }
        }

        // Updates the current users email and/or profile picture
        // Accepts form data and supports picture removal
        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateCurrentUser(
            [FromForm] UpdateUserDto model,
            [FromForm] Microsoft.AspNetCore.Http.IFormFile profilePicture)
        {
            try 
            {
                // Get current user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("No user ID claim present.");

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                // Update email 
                if (!string.IsNullOrEmpty(model.Email))
                {
                    user.Email = model.Email;
                }

                // Handle new profile picture 
                if (profilePicture != null)
                {
                    // Convert to base64 for storage in user.ProfilePicture
                    using var ms = new MemoryStream();
                    await profilePicture.CopyToAsync(ms);
                    var fileBytes = ms.ToArray();
                    // For simplicity: store as base64 in the DB
                    user.ProfilePicture = Convert.ToBase64String(fileBytes);
                }
                else if (model.RemoveProfilePicture == true)
                {
                    // If user wants to remove the picture
                    user.ProfilePicture = string.Empty;
                }

                // Save changes
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                // Get the users roles to return in response
                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = roles.FirstOrDefault() ?? "User";

                // Return updated user data
                return Ok(new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.DateOfBirth,
                    Role = primaryRole, // Ensure correct role is returned
                    user.ProfilePicture,
                    user.UserName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new { message = "An error occurred while updating user profile." });
            }
        }
    }

    
    public class UpdateUserDto
    {
        public string Email { get; set; }
        public bool RemoveProfilePicture { get; set; }
    }
}