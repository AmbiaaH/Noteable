using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    // Controller for managing forum questions


    public class ForumQuestionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ForumQuestionsController> _logger;

        // Initializes the controller with the database context, user manager, and logger.
        public ForumQuestionsController( 

            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ForumQuestionsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // Returns a paginated list of forum questions including comment counts
        // Defaults to 10 items per page and  max 50
        [HttpGet]
        public async Task<IActionResult> GetQuestions([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 50) pageSize = 50;

                // Get total count for pagination metadata
                var totalCount = await _context.ForumQuestions.CountAsync();

                // Get paginated questions and include comment counts for each question
                var questions = await _context.ForumQuestions
                    .OrderByDescending(q => q.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(q => new
                    {
                        q.Id,
                        q.Title,
                        q.Category,
                        q.Description,
                        q.AuthorId,
                        q.AuthorName,
                        q.CreatedAt,
                        q.Status,
                        CommentsCount = _context.ForumQuestionComments.Count(c => c.ForumQuestionId == q.Id)
                    })
                    .ToListAsync();

                // Return with pagination metadata
                return Ok(new
                {
                    TotalCount = totalCount,
                    PageSize = pageSize,
                    CurrentPage = page,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Questions = questions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving forum questions");
                return StatusCode(500, "An error occurred while retrieving forum questions.");
            }
        }

        // Retrieves a single question by its ID. Returns 404 if not found.
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestion(int id)
        {
            var question = await _context.ForumQuestions.FindAsync(id);

            if (question == null)
            {
                return NotFound();
            }

            return Ok(question);
        }

        // Allows an authenticated user to create a new forum question.
        [HttpPost]
        public async Task<IActionResult> CreateQuestion([FromBody] ForumQuestionDto questionDto)
        {
            // Get current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Validate input
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Set author information
            questionDto.AuthorId = user.Id;
            questionDto.AuthorName = user.UserName;
            questionDto.CreatedAt = DateTime.UtcNow;
            questionDto.Status = "Open";

            try
            {
                _context.ForumQuestions.Add(questionDto);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetQuestion), new { id = questionDto.Id }, questionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating forum question");
                return StatusCode(500, "An error occurred while creating the forum question.");
            }
        }

        
        //Allows the author of a question to update it
        //Validates ID match and ownership
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] ForumQuestionDto questionDto)
        {
            if (id != questionDto.Id)
            {
                return BadRequest("ID mismatch");
            }

            // Get current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Validate input
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find the question
            var question = await _context.ForumQuestions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            // Only allow updates by the author
            if (question.AuthorId != user.Id)
            {
                return Forbid();
            }

            // Update question properties
            question.Title = questionDto.Title;
            question.Category = questionDto.Category;
            question.Description = questionDto.Description;
            question.Status = questionDto.Status;

            try
            {
                _context.Entry(question).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating forum question");
                return StatusCode(500, "An error occurred while updating the forum question.");
            }
        }

       
        // Allows the author or an admin to delete a forum question
        // Also deletes all related comments for cleanup

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            // Get current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Find the question
            var question = await _context.ForumQuestions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            // Check if user is admin (checking username directly for simplicity)
            bool isAdmin = user.UserName == "adminambia";
            
            // Only allow the author or an admin to delete
            if (question.AuthorId != user.Id && !isAdmin)
            {
                return Forbid();
            }

            try
            {
                // Delete related comments first
                var comments = await _context.ForumQuestionComments
                    .Where(c => c.ForumQuestionId == id)
                    .ToListAsync();
                
                if (comments.Any())
                {
                    _context.ForumQuestionComments.RemoveRange(comments);
                }

                // Delete the question
                _context.ForumQuestions.Remove(question);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting forum question");
                return StatusCode(500, "Error deleting question");
            }
        }
        
        // Public endpoint to check if the current user is authenticated and has the Admin role
        [HttpGet("TestAdmin")]
        [AllowAnonymous]
        public async Task<IActionResult> TestAdminAccess()
        {
            
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Ok(new { status = "Not authenticated" });
            }
            
            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");
            
            return Ok(new { 
                username = user.UserName,
                userId = user.Id,
                isAuthenticated = User.Identity.IsAuthenticated,
                roles = roles,
                isAdmin = isAdmin
            });
        }
        
        // POST alternative for delete functionality
        [HttpPost("remove/{id}")]
        public async Task<IActionResult> RemoveQuestion(int id)
        {
            // Get current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Find the question
            var question = await _context.ForumQuestions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            // Check if user is admin (checking username directly for simplicity)
            bool isAdmin = user.UserName == "adminambia";
            
            // Only allow the author or an admin to delete
            if (question.AuthorId != user.Id && !isAdmin)
            {
                return Forbid();
            }

            try
            {
                // Delete related comments first
                var comments = await _context.ForumQuestionComments
                    .Where(c => c.ForumQuestionId == id)
                    .ToListAsync();
                
                if (comments.Any())
                {
                    _context.ForumQuestionComments.RemoveRange(comments);
                }

                // Delete the question
                _context.ForumQuestions.Remove(question);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting forum question");
                return StatusCode(500, "Error deleting question");
            }
        }
    }
}