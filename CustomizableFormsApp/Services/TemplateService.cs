// Services/TemplateService.cs
using CustomizableFormsApp.Models;
using Microsoft.EntityFrameworkCore;

public async Task CreateTemplateAsync(Template template)
{
    _context.Templates.Add(template);
    await _context.SaveChangesAsync();
}

public async Task UpdateTemplateAsync(Template template)
{
    var existing = await _context.Templates
        .Include(t => t.Questions)
        .ThenInclude(q => q.Options)
        .FirstOrDefaultAsync(t => t.Id == template.Id);

    if (existing == null) return;

    // Update template properties
    existing.Title = template.Title;
    existing.Description = template.Description;
    existing.UpdatedAt = DateTime.UtcNow;

    // Update questions
    foreach (var question in template.Questions)
    {
        var existingQuestion = existing.Questions
            .FirstOrDefault(q => q.Id == question.Id);

        if (existingQuestion == null)
        {
            existing.Questions.Add(question);
        }
        else
        {
            // Update question properties
            existingQuestion.Text = question.Text;
            existingQuestion.Description = question.Description;
            existingQuestion.Type = question.Type;
            existingQuestion.OrderIndex = question.OrderIndex;
            existingQuestion.IsRequired = question.IsRequired;

            // Update options
            foreach (var option in question.Options)
            {
                var existingOption = existingQuestion.Options
                    .FirstOrDefault(o => o.Id == option.Id);

                if (existingOption == null)
                {
                    existingQuestion.Options.Add(option);
                }
                else
                {
                    existingOption.Text = option.Text;
                    existingOption.Value = option.Value;
                    existingOption.OrderIndex = option.OrderIndex;
                }
            }

            // Remove deleted options
            var optionsToRemove = existingQuestion.Options
                .Where(o => !question.Options.Any(qo => qo.Id == o.Id))
                .ToList();

            foreach (var option in optionsToRemove)
            {
                existingQuestion.Options.Remove(option);
            }
        }
    }

    // Remove deleted questions
    var questionsToRemove = existing.Questions
        .Where(q => !template.Questions.Any(tq => tq.Id == q.Id))
        .ToList();

    foreach (var question in questionsToRemove)
    {
        existing.Questions.Remove(question);
    }

    await _context.SaveChangesAsync();
}