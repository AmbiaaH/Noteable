using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteableApi.Data;
using NoteableApi.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NoteableApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ForumCommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ForumCommentsController> _logger;

        public ForumCommentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ForumCommentsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Get all comments for a specific forum question
        [HttpGet("{questionId}")]
        public async Task<IActionResult> GetComments(int questionId)
        {
            var comments = await _context.ForumQuestionComments
                .Where(c => c.ForumQuestionId == questionId)
                .Include(c => c.Author)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => c.ToResponse())
                .ToListAsync();

            return Ok(comments);
        }

        // Add a new comment to a question
        [HttpPost]
        public async Task<IActionResult> PostComment([FromBody] ForumQuestionCommentDto comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var questionExists = await _context.ForumQuestions.AnyAsync(q => q.Id == comment.ForumQuestionId);
            if (!questionExists)
                return NotFound("Question not found");

            comment.AuthorId = user.Id;
            comment.CreatedAt = DateTime.UtcNow;
            comment.Status = "Active";

            try
            {
                _context.ForumQuestionComments.Add(comment);
                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetComments),
                    new { questionId = comment.ForumQuestionId },
                    comment.ToResponse());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save comment");
                return StatusCode(500, "Server error while saving comment");
            }
        }

        // Delete a comment — allowed if you're the author or an admin
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var comment = await _context.ForumQuestionComments.FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");

            _logger.LogInformation("User: {User} | Roles: {Roles} | IsAdmin: {IsAdmin}", user.UserName, string.Join(", ", roles), isAdmin);

            // Only authors and admins can delete
            if (comment.AuthorId != user.Id && !isAdmin)
            {
                _logger.LogWarning("Forbidden: User {User} tried to delete comment {Id}", user.UserName, id);
                return Forbid("You are not allowed to delete this comment.");
            }

            try
            {
                _context.ForumQuestionComments.Remove(comment);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {Id}", id);
                return StatusCode(500, "Server error while deleting comment");
            }
        }
    }
}
