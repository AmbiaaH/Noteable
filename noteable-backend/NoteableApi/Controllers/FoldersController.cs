using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteableApi.Data;
using NoteableApi.Models;
using System.Security.Claims;

namespace NoteableApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    // Controller for managing folders (for specific users)
    // each user can only access and modify their own folders.

    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FoldersController> _logger;

        public FoldersController(ApplicationDbContext context, ILogger<FoldersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        
        [HttpGet("test")] //test endpoint for controller accesebility 

        public IActionResult Test()
        // Initializes the controller with the application DB context and logger
        {
            _logger.LogInformation("Folders test endpoint called");
            return Ok(new { message = "Folders controller is working!" });
        }

    
        // returns a list of all folders owned by the user
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetFolders()
        {
            _logger.LogInformation("GetFolders called");
            
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("GetFolders: User not authenticated");
                return Unauthorized("User not logged in.");
            }

            try
            {
                var folders = await _context.Folders
                    .Where(f => f.UserId == userId)
                    .OrderBy(f => f.Name)
                    .ToListAsync();

                _logger.LogInformation($"GetFolders: Found {folders.Count} folders");
                return Ok(folders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFolders");
                return StatusCode(500, "An error occurred while fetching folders");
            }
        }

        // Returns a specific folder (including its notes) if it belongs to the user
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetFolder(int id)
        {
            _logger.LogInformation($"GetFolder called with id: {id}");
            
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("GetFolder: User not authenticated");
                return Unauthorized("User not logged in.");
            }

            try
            {
                var folder = await _context.Folders
                    .Include(f => f.Notes)
                    .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

                if (folder == null)
                {
                    _logger.LogWarning($"GetFolder: Folder with ID {id} not found");
                    return NotFound("Folder not found.");
                }

                return Ok(folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in GetFolder with ID {id}");
                return StatusCode(500, "An error occurred while fetching the folder");
            }
        }

        
        // Creates a new folder for the authenticated user 
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateFolder([FromBody] FolderCreateDto dto)
        {
            _logger.LogInformation($"CreateFolder called with name: {dto?.Name}");
            
            if (dto == null)
            {
                _logger.LogWarning("CreateFolder: dto is null");
                return BadRequest("Invalid request data");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("CreateFolder: User not authenticated");
                return Unauthorized("User not logged in.");
            }

            // Validate folder name
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                _logger.LogWarning("CreateFolder: Empty folder name");
                return BadRequest("Folder name cannot be empty.");
            }

            try
            {
                // Check if a folder with same name already exists for this user
                bool folderExists = await _context.Folders
                    .AnyAsync(f => f.Name == dto.Name && f.UserId == userId);

                if (folderExists)
                {
                    _logger.LogWarning("CreateFolder: Folder with same name exists");
                    return BadRequest("You already have a folder with this name.");
                }

                var folder = new Folder
                {
                    Name = dto.Name,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Folders.Add(folder);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"CreateFolder: Created folder with ID {folder.Id}");
                return CreatedAtAction(nameof(GetFolder), new { id = folder.Id }, folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateFolder");
                return StatusCode(500, "An error occurred while creating the folder");
            }
        }

        // Updates name of an existing folder 
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateFolder(int id, [FromBody] FolderUpdateDto dto)
        {
            _logger.LogInformation($"UpdateFolder called for id: {id}, name: {dto?.Name}");
            
            if (dto == null)
            {
                _logger.LogWarning("UpdateFolder: dto is null");
                return BadRequest("Invalid request data");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("UpdateFolder: User not authenticated");
                return Unauthorized("User not logged in.");
            }

            try
            {
                var folder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

                if (folder == null)
                {
                    _logger.LogWarning($"UpdateFolder: Folder with ID {id} not found");
                    return NotFound("Folder not found.");
                }

                // Validate folder name
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    _logger.LogWarning("UpdateFolder: Empty folder name");
                    return BadRequest("Folder name cannot be empty.");
                }

                // Check if a folder with same name already exists for this user (excluding current folder)
                bool folderExists = await _context.Folders
                    .AnyAsync(f => f.Name == dto.Name && f.UserId == userId && f.Id != id);

                if (folderExists)
                {
                    _logger.LogWarning("UpdateFolder: Folder with same name exists");
                    return BadRequest("You already have a folder with this name.");
                }

                folder.Name = dto.Name;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"UpdateFolder: Updated folder with ID {id}");
                return Ok(folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in UpdateFolder with ID {id}");
                return StatusCode(500, "An error occurred while updating the folder");
            }
        }

    
        // Deletes a folder owned by the user and removes all notes from it
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            _logger.LogInformation($"DeleteFolder called with id: {id}");
            
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                _logger.LogWarning("DeleteFolder: User not authenticated");
                return Unauthorized("User not logged in.");
            }

            try
            {
                var folder = await _context.Folders
                    .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

                if (folder == null)
                {
                    _logger.LogWarning($"DeleteFolder: Folder with ID {id} not found");
                    return NotFound("Folder not found.");
                }

                // Unset FolderId on notes in this folder
                var notesInFolder = await _context.Notes
                    .Where(n => n.FolderId == id)
                    .ToListAsync();

                foreach (var note in notesInFolder)
                {
                    _logger.LogInformation($"DeleteFolder: Unsetting FolderId for note {note.Id}");
                    note.FolderId = null;
                }

                _context.Folders.Remove(folder);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"DeleteFolder: Deleted folder with ID {id}");
                return Ok("Folder deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in DeleteFolder with ID {id}");
                return StatusCode(500, "An error occurred while deleting the folder");
            }
        }
    }
}