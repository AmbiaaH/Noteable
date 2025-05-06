using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace NoteableApi.Data
{
    // Custom user class inheriting from IdentityUser
    public class ApplicationUser : IdentityUser
    {
        [Required]
        // First name of the user  for registration and display
        public string FirstName { get; set; } = string.Empty;
        
        [Required]
        // Last name of the user  for registration and display
        public string LastName { get; set; } = string.Empty;

        // Optional date of birth
        public DateTime? DateOfBirth { get; set; } // Nullable to avoid errors

        
        // Application-specific role field 
        // Used in addition to ASP.NET Identity roles for convenience or syncing
        public string Role { get; set; } = "User";  // Default value if needed
        
        // Base64-encoded profile picture
        public string ProfilePicture { get; set; } = string.Empty;

    }
}

