// Models/Answer.cs
public class Answer
{
    public Guid QuestionId { get; set; }
    public string? TextValue { get; set; }
    public int? NumberValue { get; set; }
    public DateTime? DateValue { get; set; }
    public string? SelectedOption { get; set; }
    public Dictionary<string, bool> SelectedOptions { get; set; } = new();
}