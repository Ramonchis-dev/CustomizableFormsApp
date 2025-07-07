using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomizableFormsApp.Models
{
    public class QuestionOption
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; } // Primary key for QuestionOption

        [Required]
        public Guid QuestionId { get; set; } // Foreign key to Question
        public Question Question { get; set; } = null!; // Navigation property

        [Required]
        [MaxLength(255)]
        public string Text { get; set; } = string.Empty; // Display text for the option

        [Required]
        [MaxLength(255)]
        public string Value { get; set; } = string.Empty; // Stored value for the option

        [Required]
        public int OrderIndex { get; set; } // Order of option within question
    }
}