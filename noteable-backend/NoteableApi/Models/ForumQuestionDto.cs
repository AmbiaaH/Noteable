using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteableApi.Models
{
    public class ForumQuestionDto // represents a forum question posted by a user
    {
        [Key] // unique ID for the forum question
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")] // title of the question
        [StringLength(200, ErrorMessage = "Title must not exceed 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")] // category the question belongs to
        [StringLength(50, ErrorMessage = "Category must not exceed 50 characters")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")] // full description or content of the question
        public string Description { get; set; } = string.Empty;

        public string? AuthorId { get; set; } // ID of the user who asked the question

        public string? AuthorName { get; set; } // username of the author

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // date and time the question was created

        [StringLength(50)] // status of the question like Open or Closed
        public string Status { get; set; } = "Open";
    }
}