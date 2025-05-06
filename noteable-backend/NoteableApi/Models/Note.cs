namespace NoteableApi.Models
{
    public class Note // represents a note uploaded by a user including file and metadata

    {
        public int Id { get; set; } // unique ID for the note
        public string Title { get; set; } = string.Empty; // title of the note
        public string Preview { get; set; } = string.Empty; // text preview or summary of the note
        public string Author { get; set; } = string.Empty;// name of the person who created the note
        public string Category { get; set; } = string.Empty; // category the note belongs to like Math or Science
        public DateTime CreatedAt { get; set; } // when the note was created
        public string? FilePath { get; set; } // path to the uploaded file on the server
        public int? FolderId { get; set; } // folder ID if the note is assigned to a folder
        public Folder? Folder { get; set; } // navigation property for the folder

       
        public string? OwnerId { get; set; }// ID of the user who owns the note
    }
}


