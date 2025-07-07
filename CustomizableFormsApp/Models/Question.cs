using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomizableFormsApp.Models
{
    public class Question
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } // Primary key for Question

        [Required]
        public Guid TemplateId { get; set; } // Foreign key to Template
        public Template Template { get; set; } = null!; // Navigation property

        [Required]
        public string Text { get; set; } = string.Empty; // Question text

        public string? Description { get; set; } // Optional description

        [Required]
        public QuestionType Type { get; set; } // Enum for question type

        [Required]
        public int OrderIndex { get; set; } // Order of question within template

        public bool IsRequired { get; set; } = false;

        [Column(TypeName = "jsonb")] // Stored as JSONB in PostgreSQL
        public string ValidationRules { get; set; } = "{}"; // JSON string for validation rules

        public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>(); // For dropdown/multiselect
    }
}