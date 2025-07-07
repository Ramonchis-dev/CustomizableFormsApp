using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomizableFormsApp.Models
{
    public class FormSubmission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid SubmissionId { get; set; } // Primary key for FormSubmission

        [Required]
        public Guid TemplateId { get; set; } // Foreign key to Template
        public Template Template { get; set; } = null!; // Navigation property

        // Nullable for non-authenticated submissions
        public string? SubmitterUserId { get; set; }
        public ApplicationUser? SubmitterUser { get; set; } // Navigation property

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "jsonb")] // Stored as JSONB in PostgreSQL
        public string AnswersJson { get; set; } = "{}"; // JSON string for dynamic answers
    }
}