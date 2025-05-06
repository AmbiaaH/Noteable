public class UserNote // links a note to a user to track notes saved to their journal

{
    public int Id { get; set; } // unique ID for the user-note link
    public string UserId { get; set; }  = string.Empty; // ID of the user who saved the note
    public int NoteId { get; set; } // ID of the note that was saved
    public DateTime AddedAt { get; set; } = DateTime.UtcNow; // when the note was added to the journal

}
