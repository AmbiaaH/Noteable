using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace NoteableApi.Models
{
    public class MentorProfile
    {
        [Key]
        public int Id { get; set; }
        
        // Stores the unique ID of the mentor (linked to ApplicationUser)
        [Required]
        public string MentorId { get; set; } = string.Empty;
        
        // A brief bio or description for the mentor
        [Required]
        public string Bio { get; set; } = string.Empty;
        
        // Skills or specialties of the mentor
        [Required]
        public string Skills { get; set; } = string.Empty;
        
        // Academic title or professional position
        public string Title { get; set; } = string.Empty;
        
        // Profile image URL
        public string AvatarUrl { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property for feedback
        public virtual ICollection<MentorFeedback> Feedback { get; set; } = new List<MentorFeedback>();
        
        // Computed properties (not stored in database)
        [NotMapped]
        public double AverageRating => Feedback.Count > 0 ? Feedback.Average(f => f.Rating) : 0;
        
        [NotMapped]
        public int RatingCount => Feedback.Count;
        
        [NotMapped]
        public List<string> SpecialtyList => Skills?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToList() ?? new List<string>();
    }
}