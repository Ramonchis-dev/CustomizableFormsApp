
using CustomizableFormsApp.Models;
using Microsoft.EntityFrameworkCore;

public class FormSubmissionService
{
    private readonly CustomizableFormsAppDbContext _context;

    public FormSubmissionService(CustomizableFormsAppDbContext context)
        => _context = context;

    public async Task CreateSubmissionAsync(FormSubmission submission)
    {
        _context.FormSubmissions.Add(submission);
        await _context.SaveChangesAsync();
    }

    public async Task<List<FormSubmission>> GetSubmissionsAsync(Guid templateId)
    {
        return await _context.FormSubmissions
            .Where(fs => fs.TemplateId == templateId)
            .ToListAsync();
    }
}