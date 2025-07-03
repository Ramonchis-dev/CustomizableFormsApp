namespace CustomizableFormsApp.Models;

public class QuestionOption
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int OrderIndex { get; set; }

    public Question? Question { get; set; }
}