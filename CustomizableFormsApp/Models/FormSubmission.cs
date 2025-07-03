namespace CustomizableFormsApp.Models;

public class FormSubmission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TemplateId { get; set; }
    public Guid? SubmitterUserId { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string AnswersJson { get; set; } = "{}"; // JSONB

    public Template? Template { get; set; }
}