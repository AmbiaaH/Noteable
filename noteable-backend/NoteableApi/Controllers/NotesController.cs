using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NoteableApi.Data;
using NoteableApi.Models;
using System.IO;
using System.Security.Claims;
using ImageMagick; 

namespace NoteableApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<NotesController> _logger;

        public NotesController(ApplicationDbContext context, IWebHostEnvironment environment, ILogger<NotesController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET /api/notes
        // gets all notes in the system and  their folders

        [HttpGet]
        public async Task<IActionResult> GetNotes()
        {
            try
            {
                _logger.LogInformation("Fetching all notes");
                
                // Test database connection first
                if (!_context.Database.CanConnect())
                {
                    _logger.LogError("Database connection failed");
                    return StatusCode(500, new { Message = "Database connection failed" });
                }
                
                var notes = await _context.Notes.Include(n => n.Folder).ToListAsync();
                _logger.LogInformation($"Successfully retrieved {notes.Count} notes");
                return Ok(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching notes");
                return StatusCode(500, new { Message = "Failed to retrieve notes", Error = ex.Message });
            }
        }

        // Test endpoint to verify database connection
        [HttpGet("test-connection")]
        public IActionResult TestConnection()
        {
            try
            {
                bool canConnect = _context.Database.CanConnect();
                return Ok(new { Connected = canConnect, Message = canConnect ? "Database connection successful" : "Cannot connect to database" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return StatusCode(500, new { Message = "Database connection test failed", Error = ex.Message });
            }
        }
        
        // Gets notes saved to the users personal journal
        [HttpGet("my-journal")]
        [Authorize]
        public async Task<IActionResult> GetMyJournal()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized("User not logged in.");

                var noteIds = await _context.UserNotes
                    .Where(un => un.UserId == userId)
                    .Select(un => un.NoteId)
                    .ToListAsync();

                var journalNotes = await _context.Notes
                    .Where(n => noteIds.Contains(n.Id))
                    .ToListAsync();

                return Ok(journalNotes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching journal notes");
                return StatusCode(500, new { Message = "Failed to retrieve journal notes", Error = ex.Message });
            }
        }

       
        // Reads only the first page of the PDF for a quick preview
        [HttpGet("preview/{id}")]
        public async Task<IActionResult> GetPdfPreview(int id)
        {
            try
            {
                var note = await _context.Notes.FindAsync(id);
                if (note == null || note.FilePath == null)
                    return NotFound("PDF not found.");

                var filePath = Path.Combine(_environment.WebRootPath, note.FilePath);
                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found on server.");

                try
                {
                    var settings = new MagickReadSettings { Density = new Density(150) };
                    // Append [0] to read only the first page of the PDF
                    using (var image = new MagickImage(filePath + "[0]", settings))
                    {
                        image.BackgroundColor = MagickColors.White;
                        image.Alpha(AlphaOption.Remove);
                        image.Resize(new MagickGeometry("50%"));
                        byte[] imageBytes = image.ToByteArray(MagickFormat.Jpeg);
                        return File(imageBytes, "image/jpeg");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate PDF preview for note ID: {Id}", id);
                    return StatusCode(500, new { Message = "Failed to generate PDF preview", Error = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing PDF preview request for note ID: {Id}", id);
                return StatusCode(500, new { Message = "Failed to process PDF preview request", Error = ex.Message });
            }
        }

        // Full PDF access which authenticated users can access
        [HttpGet("full/{id}")]
        [Authorize]
        public async Task<IActionResult> GetFullPdf(int id)
        {
            try
            {
                var note = await _context.Notes.FindAsync(id);
                if (note == null || note.FilePath == null)
                    return NotFound("PDF not found.");

                var filePath = Path.Combine(_environment.WebRootPath, note.FilePath);
                if (!System.IO.File.Exists(filePath))
                    return NotFound("File not found on server.");

                var pdfBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching full PDF for note ID: {Id}", id);
                return StatusCode(500, new { Message = "Failed to retrieve full PDF", Error = ex.Message });
            }
        }

        // Uploads a new note along with optional file info.
        [HttpPost]
        [Consumes("multipart/form-data")]
        [Authorize] // require login to upload
        public async Task<IActionResult> CreateNote([FromForm] NoteCreateDto dto, IFormFile file)
        {
            try
            {
                // Get current user id from the token/claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized("You must be logged in to upload notes.");

                string? filePath = null;
                if (file != null && file.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var fileName = file.FileName;
                    var fullPath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    filePath = Path.Combine("uploads", fileName);
                }

                int? folderIdToUse = dto.FolderId;
                if (folderIdToUse.HasValue)
                {
                    bool exists = await _context.Folders.AnyAsync(f => f.Id == folderIdToUse.Value);
                    if (!exists) folderIdToUse = null;
                }

                var note = new Note
                {
                    Title = dto.Title,
                    Preview = dto.Preview,
                    Author = dto.Author,
                    Category = dto.Category,
                    FolderId = folderIdToUse,
                    CreatedAt = DateTime.UtcNow,
                    FilePath = filePath,
                    OwnerId = userId // set the uploader as the owner
                };

                _context.Notes.Add(note);
                await _context.SaveChangesAsync();
                return Ok(note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating note");
                return StatusCode(500, new { Message = "Failed to create note", Error = ex.Message });
            }
        }

        // Only allow deletion if the user is the owner or an Admin.
        [HttpDelete("{id}")]
        [Authorize] // must be logged in
        public async Task<IActionResult> DeleteNote(int id)
        {
            try
            {
                var note = await _context.Notes.FindAsync(id);
                if (note == null)
                    return NotFound("Note not found.");

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized();

                // Check if the current user is an Admin or the owner of the note
                bool isAdmin = User.IsInRole("Admin");
                bool isOwner = note.OwnerId == userId;

                if (!isAdmin && !isOwner)
                {
                    return Forbid("You do not have permission to delete this note.");
                }

                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
                return Ok("Note deleted successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting note ID: {Id}", id);
                return StatusCode(500, new { Message = "Failed to delete note", Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(int id, [FromBody] Note updatedNote)
        {
            try
            {
                if (id != updatedNote.Id)
                    return BadRequest("ID mismatch.");

                _context.Entry(updatedNote).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                return Ok(updatedNote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating note ID: {Id}", id);
                return StatusCode(500, new { Message = "Failed to update note", Error = ex.Message });
            }
        }

        [HttpPost("add-to-journal/{noteId}")]
        [Authorize]
        // Adds the chosen note to the current user's journal

        public async Task<IActionResult> AddToJournal(int noteId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized("User not logged in.");

                bool noteExists = await _context.Notes.AnyAsync(n => n.Id == noteId);
                if (!noteExists)
                    return NotFound("Note not found.");

                // Check if already in journal
                bool alreadyInJournal = await _context.UserNotes
                    .AnyAsync(un => un.UserId == userId && un.NoteId == noteId);
                    
                if (alreadyInJournal)
                    return Ok("Note is already in your journal.");

                var userNote = new UserNote
                {
                    UserId = userId,
                    NoteId = noteId,
                    AddedAt = DateTime.UtcNow
                };

                _context.UserNotes.Add(userNote);
                await _context.SaveChangesAsync();
                return Ok("Added to journal.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding note ID: {NoteId} to journal", noteId);
                return StatusCode(500, new { Message = "Failed to add note to journal", Error = ex.Message });
            }
        }

        // Removes a note from the current users journal
        [HttpDelete("remove-from-journal/{noteId}")]
        [Authorize]
        public async Task<IActionResult> RemoveFromJournal(int noteId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                    return Unauthorized("User not logged in.");

                var userNote = await _context.UserNotes
                    .FirstOrDefaultAsync(un => un.NoteId == noteId && un.UserId == userId);

                if (userNote == null)
                    return NotFound("Note not found in your journal.");

                _context.UserNotes.Remove(userNote);
                await _context.SaveChangesAsync();
                return Ok("Removed from journal.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing note ID: {NoteId} from journal", noteId);
                return StatusCode(500, new { Message = "Failed to remove note from journal", Error = ex.Message });
            }
        }
        
        // fixed Update a note's folder without circular references
        // Updates the folder assignment for a note
        // Validates ownership and folder access
        [HttpPut("{id}/folder")]
        [Authorize]
        public async Task<IActionResult> UpdateNoteFolder(int id, [FromBody] UpdateNoteFolderDto dto)
        {
            try
            {
                _logger.LogInformation($"UpdateNoteFolder called for note ID: {id}, folder ID: {dto?.FolderId}");
                
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    _logger.LogWarning("UpdateNoteFolder: User not authenticated");
                    return Unauthorized("User not logged in.");
                }

                var note = await _context.Notes.FindAsync(id);
                if (note == null)
                {
                    _logger.LogWarning($"UpdateNoteFolder: Note with ID {id} not found");
                    return NotFound("Note not found.");
                }

                // If the dto.FolderId is null,  move to All Notes (no folder)
                // Or ensure the folder exists and belongs to the user
                if (dto.FolderId.HasValue)
                {
                    var folder = await _context.Folders.FirstOrDefaultAsync(f => f.Id == dto.FolderId.Value && f.UserId == userId);
                    if (folder == null)
                    {
                        _logger.LogWarning($"UpdateNoteFolder: Folder with ID {dto.FolderId} not found or does not belong to user");
                        return NotFound("Folder not found or you don't have access to it.");
                    }
                }

                // Update the notes folder
                note.FolderId = dto.FolderId;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Note {id} moved to folder {dto.FolderId}");
                
                // Return a simplified response to avoid circular references
                return Ok(new { 
                    message = "Note folder updated successfully", 
                    noteId = note.Id,
                    folderId = note.FolderId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating note folder for ID: {Id}", id);
                return StatusCode(500, new { Message = "Failed to update note folder", Error = ex.Message });
            }
        }
    }

    // Add this DTO class at the bottom of the file or in your DTOs folder
    public class UpdateNoteFolderDto
    {
        public int? FolderId { get; set; }
    }
}