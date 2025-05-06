using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteableApi.Data;
using NoteableApi.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace NoteableApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MentorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MentorController> _logger;

        public MentorController(ApplicationDbContext context, ILogger<MentorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Retrieves a mentor profile by mentorId.
        [HttpGet("{mentorId}")]
        public async Task<IActionResult> GetMentorProfile(string mentorId)
        {
            var profile = await _context.MentorProfiles
                .Include(mp => mp.Feedback)
                .FirstOrDefaultAsync(mp => mp.MentorId == mentorId);

            if (profile == null)
            {
                return NotFound("Mentor profile not found.");
            }

            // Get the user information for this mentor
            var mentorUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == mentorId);

            // Get feedback details with user information
            var feedbackList = await _context.MentorFeedback
                .Where(f => f.MentorProfileId == profile.Id)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new
                {
                    id = f.Id,
                    rating = f.Rating,
                    comment = f.Comment,
                    // Get the username from the user table
                    userName = _context.Users.FirstOrDefault(u => u.Id == f.UserId).UserName,
                    createdAt = f.CreatedAt
                })
                .ToListAsync();

            // Process specialties client-side to avoid LINQ translation issues
            var specialties = string.IsNullOrEmpty(profile.Skills) 
                ? new List<string>() 
                : profile.Skills
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToList();

            return Ok(new
            {
                profile.Id,
                profile.MentorId,
                name = mentorUser?.UserName ?? "Unknown",
                title = profile.Title,
                bio = profile.Bio,
                avatarUrl = profile.AvatarUrl,
                specialties = specialties,
                averageRating = profile.Feedback.Any() ? profile.Feedback.Average(f => f.Rating) : 0,
                ratingCount = profile.Feedback.Count,
                feedback = feedbackList
            });
        }


        // Get all mentors with pagination and filtering
        [HttpGet]
        public async Task<IActionResult> GetAllMentors(
            [FromQuery] string search = null,
            [FromQuery] string specialty = null,
            [FromQuery] int? minRating = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 8)
        {
            try
            {
                IQueryable<MentorProfile> query = _context.MentorProfiles
                    .Include(mp => mp.Feedback)
                    .AsNoTracking(); // Improve performance

                // Simplify filtering
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    query = query.Where(mp =>
                        mp.Title.ToLower().Contains(search) ||
                        mp.Bio.ToLower().Contains(search) ||
                        mp.Skills.ToLower().Contains(search));
                }

                if (!string.IsNullOrEmpty(specialty))
                {
                    query = query.Where(mp => mp.Skills.ToLower().Contains(specialty.ToLower()));
                }

                if (minRating.HasValue)
                {
                    query = query.Where(mp =>
                        mp.Feedback.Any() &&
                        mp.Feedback.Average(f => f.Rating) >= minRating.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination and project to DTO
                // But fetch the raw data first, without complex string operations
                var mentorsRaw = await query
                    .OrderBy(mp => mp.Id) // Ensure consistent ordering
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(mp => new
                    {
                        id = mp.Id,
                        mentorId = mp.MentorId,
                        name = _context.Users.FirstOrDefault(u => u.Id == mp.MentorId).UserName,
                        title = mp.Title,
                        bio = mp.Bio,
                        avatarUrl = mp.AvatarUrl,
                        skills = mp.Skills, // Just get the raw skills string
                        averageRating = mp.Feedback.Any() ? mp.Feedback.Average(f => f.Rating) : 0,
                        ratingCount = mp.Feedback.Count
                    })
                    .ToListAsync();
                
                // Now process the string operations in memory
                var mentors = mentorsRaw.Select(m => new
                {
                    id = m.id,
                    mentorId = m.mentorId,
                    name = m.name,
                    title = m.title,
                    bio = m.bio,
                    avatarUrl = m.avatarUrl,
                    specialties = string.IsNullOrEmpty(m.skills)
                        ? new List<string>()
                        : m.skills.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .ToList(),
                    averageRating = m.averageRating,
                    ratingCount = m.ratingCount
                }).ToList();

                return Ok(new
                {
                    totalCount,
                    page,
                    pageSize,
                    items = mentors
                });
            }
            catch (Exception ex)
            {
                // Log the full exception details
                _logger.LogError(ex, "Error fetching mentors");
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching mentors",
                    details = ex.Message
                });
            }
        }


        // Create or update the current mentors profile.
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrUpdateProfile([FromBody] MentorProfileDto dto)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(mentorId))
            {
                return Unauthorized();
            }

            var profile = await _context.MentorProfiles.FirstOrDefaultAsync(mp => mp.MentorId == mentorId);

            if (profile == null)
            {
                // Create a new profile 
                profile = new MentorProfile
                {
                    MentorId = mentorId,
                    Bio = dto.Bio,
                    Skills = dto.Skills,
                    Title = dto.Title,
                    AvatarUrl = dto.AvatarUrl,
                    CreatedAt = DateTime.UtcNow
                };
                _context.MentorProfiles.Add(profile);
            }
            else
            {
                // Update the existing profile.
                profile.Bio = dto.Bio;
                profile.Skills = dto.Skills;
                profile.Title = dto.Title;
                profile.AvatarUrl = dto.AvatarUrl;
            }

            await _context.SaveChangesAsync();
            return Ok("Mentor profile updated successfully.");
        }

       
        // Add feedback for a mentor
        [HttpPost("{mentorProfileId}/feedback")]
        [Authorize]
        public async Task<IActionResult> AddFeedback(int mentorProfileId, [FromBody] FeedbackDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var mentorProfile = await _context.MentorProfiles.FindAsync(mentorProfileId);
            if (mentorProfile == null)
            {
                return NotFound("Mentor profile not found.");
            }

            // Check if user has already provided feedback for this mentor
            var existingFeedback = await _context.MentorFeedback
                .FirstOrDefaultAsync(f => f.MentorProfileId == mentorProfileId && f.UserId == userId);

            if (existingFeedback != null)
            {
                // Update existing feedback
                existingFeedback.Rating = dto.Rating;
                existingFeedback.Comment = dto.Comment;
                existingFeedback.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new feedback
                var feedback = new MentorFeedback
                {
                    MentorProfileId = mentorProfileId,
                    UserId = userId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MentorFeedback.Add(feedback);
            }

            await _context.SaveChangesAsync();

            // Get username for response
            var username = (await _context.Users.FirstOrDefaultAsync(u => u.Id == userId))?.UserName ?? "Anonymous";

            return Ok(new
            {
                rating = dto.Rating,
                comment = dto.Comment,
                userName = username,
                createdAt = DateTime.UtcNow
            });
        }

 
        // Get all unique specialties
        [HttpGet("specialties")]
        public async Task<IActionResult> GetSpecialties()
        {
            //  fetch the skills strings
            var skillsStrings = await _context.MentorProfiles
                .Where(mp => !string.IsNullOrEmpty(mp.Skills))
                .Select(mp => mp.Skills)
                .ToListAsync();
            
            // rocess them in memory
            var specialties = skillsStrings
                .SelectMany(skills => skills.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            return Ok(specialties);
        }
        

        [HttpDelete("feedback/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var feedback = await _context.MentorFeedback.FindAsync(id);
            if (feedback == null)
            {
                return NotFound("Feedback not found.");
            }

            // Check if user is admin or the feedback author
            bool isAdmin = User.IsInRole("Admin");
            if (feedback.UserId != userId && !isAdmin)
            {
                return Forbid();
            }

            _context.MentorFeedback.Remove(feedback);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        
        // Alternative to DELETE
        [HttpPost("feedback/remove/{id}")]
        [Authorize]
        public async Task<IActionResult> RemoveFeedback(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var feedback = await _context.MentorFeedback.FindAsync(id);
            if (feedback == null)
            {
                return NotFound("Feedback not found.");
            }

            // Check if user is admin by username (simple approach)
            bool isAdmin = User.Identity.Name == "adminambia";
            
            // Only allow the feedback author or an admin to delete
            if (feedback.UserId != userId && !isAdmin)
            {
                return Forbid();
            }

            _context.MentorFeedback.Remove(feedback);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/mentor/{id}
        // Delete a mentor profile (admin only)
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteMentor(int id)
        {
            // Get the current user ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check if user is admin
            bool isAdmin = User.Identity.Name == "adminambia";
            if (!isAdmin)
            {
                return Forbid("Only administrators can delete mentor profiles.");
            }

            // Find the mentor profile
            var mentorProfile = await _context.MentorProfiles.FindAsync(id);
            if (mentorProfile == null)
            {
                return NotFound("Mentor profile not found.");
            }

            // Delete the mentor profile
            _context.MentorProfiles.Remove(mentorProfile);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mentor profile deleted successfully." });
        }

  
        // Alternative endpoint for deleting a mentor profile (admin only)
        [HttpPost("delete/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteMentorPost(int id)
        {
            // Get the current user ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Check if user is admin
            bool isAdmin = User.Identity.Name == "adminambia";
            if (!isAdmin)
            {
                return Forbid("Only administrators can delete mentor profiles.");
            }

            // Find the mentor profile
            var mentorProfile = await _context.MentorProfiles.FindAsync(id);
            if (mentorProfile == null)
            {
                return NotFound("Mentor profile not found.");
            }

            // Delete the mentor profile
            _context.MentorProfiles.Remove(mentorProfile);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Mentor profile deleted successfully." });
        }
    }

    public class MentorProfileDto
    {
        [Required]
        public string Bio { get; set; } = string.Empty;

        [Required]
        public string Skills { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string AvatarUrl { get; set; } = string.Empty;
    }

    public class FeedbackDto
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
    }
}