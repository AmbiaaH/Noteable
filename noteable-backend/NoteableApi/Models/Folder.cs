using System;
using System.Collections.Generic;

namespace NoteableApi.Models 
{
    public class Folder // represents a folder that contains notes and belongs to a specific user

    {
        public int Id { get; set; } // unique ID for the folder
        public string Name { get; set; } = string.Empty; // name of the folder
        public string UserId { get; set; } = string.Empty;  // ID of the user who owns the folder
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // when the folder was created
        public ICollection<Note> Notes { get; set; } = new List<Note>(); // list of notes inside the folder
    }
}

