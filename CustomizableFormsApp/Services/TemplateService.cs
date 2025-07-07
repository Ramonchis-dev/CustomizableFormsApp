using CustomizableFormsApp.Data;
using CustomizableFormsApp.Models;
using Microsoft.EntityFrameworkCore;


namespace CustomizableFormsApp.Services
{
    public class TemplateService
    {
        private readonly ApplicationDbContext _context;

        public TemplateService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Template>> GetTemplatesAsync()
        {
            return await _context.Templates
                .Include(t => t.Author)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<Template?> GetTemplateByIdAsync(Guid templateId)
        {
            return await _context.Templates
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.TemplateId == templateId);
        }

        public async Task CreateTemplateAsync(Template template)
        {
            template.CreatedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            _context.Templates.Add(template);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTemplateAsync(Template template)
        {
            var existing = await _context.Templates
                .Include(t => t.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(t => t.TemplateId == template.TemplateId);

            if (existing == null) return;

            existing.Title = template.Title;
            existing.Description = template.Description;
            existing.UpdatedAt = DateTime.UtcNow;

            // Handle questions (add, update, remove)
            var questionsToRemove = existing.Questions
                .Where(q => !template.Questions.Any(tq => tq.Id == q.Id))
                .ToList();
            foreach (var question in questionsToRemove)
            {
                _context.Questions.Remove(question);
            }

            foreach (var question in template.Questions)
            {
                var existingQuestion = existing.Questions
                    .FirstOrDefault(q => q.Id == question.Id);

                if (existingQuestion == null)
                {
                    question.TemplateId = existing.TemplateId;
                    existing.Questions.Add(question);
                }
                else
                {
                    existingQuestion.Text = question.Text;
                    existingQuestion.Description = question.Description;
                    existingQuestion.Type = question.Type;
                    existingQuestion.OrderIndex = question.OrderIndex;
                    existingQuestion.IsRequired = question.IsRequired;
                    existingQuestion.ValidationRules = question.ValidationRules;

                    // Handle options for this question (add, update, remove)
                    var optionsToRemove = existingQuestion.Options
                        .Where(o => !question.Options.Any(qo => qo.Id == o.Id))
                        .ToList();
                    foreach (var option in optionsToRemove)
                    {
                        _context.QuestionOptions.Remove(option);
                    }

                    foreach (var option in question.Options)
                    {
                        var existingOption = existingQuestion.Options
                            .FirstOrDefault(o => o.Id == option.Id);

                        if (existingOption == null)
                        {
                            option.QuestionId = existingQuestion.Id;
                            existingQuestion.Options.Add(option);
                        }
                        else
                        {
                            existingOption.Text = option.Text;
                            existingOption.Value = option.Value;
                            existingOption.OrderIndex = option.OrderIndex;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteTemplateAsync(Guid templateId)
        {
            var template = await _context.Templates.FindAsync(templateId);
            if (template != null)
            {
                _context.Templates.Remove(template);
                await _context.SaveChangesAsync();
            }
        }

        // Ensure ONLY ONE of this method exists in the file
        public async Task<object> GetTemplateAnalyticsAsync(Guid templateId)
        {
            var submissionsCount = await _context.FormSubmissions
                                                .Where(fs => fs.TemplateId == templateId)
                                                .CountAsync();
            return new { TemplateId = templateId, TotalSubmissions = submissionsCount, Message = "Analytics feature coming soon!" };
        }
    }
}