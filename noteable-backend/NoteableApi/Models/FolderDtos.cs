using System;

namespace NoteableApi.Models
{
    public class FolderCreateDto // used when creating a new folder
    { 
        public string Name { get; set; } = string.Empty;
    }

    public class FolderUpdateDto // used when updating an existing folder
    {
        public string Name { get; set; } = string.Empty;
    }
}