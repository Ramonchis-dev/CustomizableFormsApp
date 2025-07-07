using System;
using System.Collections.Generic;

namespace CustomizableFormsApp.Models
{
    // This DTO represents a single answer for a question.
    // It's designed to be flexible and hold different types of values.
    // It is NOT an Entity Framework Core entity (i.e., not a database table itself).
    public class Answer
    {
        public Guid QuestionId { get; set; } // The ID of the question this answer is for

        // Use nullable types for values that might not apply to all question types
        public string? TextValue { get; set; } // For Text, Paragraph question types
        public double? NumberValue { get; set; } // For Number question types
        public DateTime? DateValue { get; set; } // For Date question types

        // For single-select (Dropdown, Radio)
        public string? SelectedOptionValue { get; set; } // Stores the 'Value' of the selected option

        // For multi-select (MultiSelect, Checkbox)
        public List<string> SelectedOptionValues { get; set; } = new(); // Stores 'Value's of selected options
    }
}