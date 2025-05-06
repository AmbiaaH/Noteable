namespace NoteableApi.Models
{
    public class NoteCreateDto // used when creating a new note with optional folder assignment
    {
        public string Title { get; set; } = string.Empty; // title of the note
        public string Preview { get; set; } = string.Empty; // short preview or summary of the note
        public string Author { get; set; } = string.Empty; // name of the note's author
        public string Category { get; set; } = string.Empty; // category the note belongs to
        public int? FolderId { get; set; } // folder ID where the note will be placed
    }

   
    public class MoveFolderDto // used to move a note to a different folder
    {
        public int? FolderId { get; set; }
    }
}

