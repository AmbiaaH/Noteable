using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteableApi.Models
{
    public class MentorFeedback // feedback left by a user for a mentor profile

    {
        [Key] // unique ID for the feedback entry
        public int Id { get; set; }
        
        [Required] // ID of the mentor profile being reviewed
        public int MentorProfileId { get; set; }
        
        [ForeignKey("MentorProfileId")] // navigation property for the related mentor profile
        public MentorProfile MentorProfile { get; set; } = null!;
        
        [Required] // ID of the user who gave the feedback
        public string UserId { get; set; } = string.Empty;
         
        [Required] // rating score between 1 and 5
        [Range(1, 5)]
        public int Rating { get; set; }
        
        [Required] // written feedback from the user
        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // date and time when the feedback was submitted
    }
}