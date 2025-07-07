using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomizableFormsApp.Models
{
    public class Template
    {
        [Key]
        // Use DatabaseGeneratedOption.Identity for Guid PKs with Npgsql's gen_random_uuid()
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid TemplateId { get; set; } // Ensure this is TemplateId

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Foreign Key to ApplicationUser (Author)
        [Required]
        public string AuthorId { get; set; } = string.Empty;
        public ApplicationUser Author { get; set; } = null!; // Navigation property

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<FormSubmission> FormSubmissions { get; set; } = new List<FormSubmission>();
    }
}