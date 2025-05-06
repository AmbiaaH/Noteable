using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using NoteableApi.Data;

namespace NoteableApi.Models
{
    public class ForumQuestionCommentDto // represents a comment made on a forum question
    {
        [Key] // unique ID for the comment
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Question ID is required")] // ID of the related forum question
        public int ForumQuestionId { get; set; }

        [ForeignKey("ForumQuestionId")] // navigation property for the forum question
        public ForumQuestionDto? ForumQuestion { get; set; }

        [Required(ErrorMessage = "Comment content is required")] // the comment text
        [StringLength(1000, 
            MinimumLength = 1, 
            ErrorMessage = "Comment must be between 1 and 1000 characters")]
        public string Content { get; set; } = string.Empty;

        public string AuthorId { get; set; } = string.Empty; // ID of the user who wrote the comment

        [ForeignKey("AuthorId")] // navigation property for the author
        public ApplicationUser? Author { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // when the comment was created

        [StringLength(50)]
        public string Status { get; set; } = "Active";

        public object ToResponse() // Method to convert to a response object
        {
            return new 
            {
                Id,
                ForumQuestionId,
                Content,
                AuthorId,
                AuthorName = Author?.UserName ?? "Unknown",
                CreatedAt,
                Status
            };
        }
    }
}