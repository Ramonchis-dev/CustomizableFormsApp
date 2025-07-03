using System.ComponentModel.DataAnnotations;

namespace CustomizableFormsApp.Models;

public class Question
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public int OrderIndex { get; set; }
    public bool IsRequired { get; set; }
    public string ValidationRules { get; set; } = "{}"; // JSONB

    public Template? Template { get; set; }
    public List<QuestionOption> Options { get; set; } = new();
}