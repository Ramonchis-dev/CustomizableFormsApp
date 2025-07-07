// This is an example of a DTO (Data Transfer Object)
// if you intend to deserialize individual answers from the AnswersJson JSONB column.
// It is NOT an Entity Framework Core entity (i.e., not a database table).

namespace CustomizableFormsApp.Models
{
    public class Answer
    {
        public string QuestionId { get; set; } = string.Empty; // Or Guid if you store Guid strings
        public string Value { get; set; } = string.Empty;
        // Add other properties as needed, e.g., for specific types of answers
    }
}