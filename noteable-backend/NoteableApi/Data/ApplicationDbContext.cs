using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NoteableApi.Models;

namespace NoteableApi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        // Existing DbSets
        public DbSet<Note> Notes { get; set; }
        public DbSet<Folder> Folders { get; set; }
        public DbSet<UserNote> UserNotes { get; set; }
        public DbSet<MentorProfile> MentorProfiles { get; set; }
        
        // Add new DbSets for Forum
        public DbSet<ForumQuestionDto> ForumQuestions { get; set; }
        public DbSet<ForumQuestionCommentDto> ForumQuestionComments { get; set; }
        
        // Add new DbSet for MentorFeedback
        public DbSet<MentorFeedback> MentorFeedback { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Configure ForumQuestions
            builder.Entity<ForumQuestionDto>(entity =>
            {
                entity.ToTable("ForumQuestions");
                
                entity.HasKey(q => q.Id);
                
                entity.Property(q => q.Title)
                    .IsRequired()
                    .HasMaxLength(200);
                
                entity.Property(q => q.Category)
                    .IsRequired()
                    .HasMaxLength(50);
                
                entity.Property(q => q.Description)
                    .IsRequired();
                
                entity.Property(q => q.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("Open");
                
                // Relationship with comments 
                entity.HasMany<ForumQuestionCommentDto>()
                    .WithOne(c => c.ForumQuestion)
                    .HasForeignKey(c => c.ForumQuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
            
            // Configure ForumQuestionComments
            builder.Entity<ForumQuestionCommentDto>(entity =>
            {
                entity.ToTable("ForumQuestionComments");
                
                entity.HasKey(c => c.Id);
                
                entity.Property(c => c.Content)
                    .IsRequired()
                    .HasMaxLength(1000);
                
                entity.Property(c => c.Status)
                    .HasMaxLength(50)
                    .HasDefaultValue("Active");
                
                // Relationship with user
                entity.HasOne(c => c.Author)
                    .WithMany()
                    .HasForeignKey(c => c.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);
                
                // Relationship with question
                entity.HasOne(c => c.ForumQuestion)
                    .WithMany()
                    .HasForeignKey(c => c.ForumQuestionId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Indexes for performance
                entity.HasIndex(c => c.CreatedAt);
                entity.HasIndex(c => c.ForumQuestionId);
            });
            
            // Configure MentorFeedback relationships
            builder.Entity<MentorFeedback>(entity =>
            {
                entity.ToTable("MentorFeedback");
                
                entity.HasKey(mf => mf.Id);
                
                entity.Property(mf => mf.Comment)
                    .IsRequired()
                    .HasMaxLength(1000);
                
                entity.Property(mf => mf.Rating)
                    .IsRequired();
                
                // Relationship with MentorProfile
                entity.HasOne(mf => mf.MentorProfile)
                    .WithMany(mp => mp.Feedback)
                    .HasForeignKey(mf => mf.MentorProfileId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                // Indexes for performance
                entity.HasIndex(mf => mf.CreatedAt);
                entity.HasIndex(mf => mf.MentorProfileId);
            });
        }
    }
}