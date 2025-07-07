using CustomizableFormsApp.Data;
using CustomizableFormsApp.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CustomizableFormsApp.Services
{
    public class FormSubmissionService
    {
        private readonly ApplicationDbContext _context;

        public FormSubmissionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<FormSubmission>> GetAllSubmissionsAsync()
        {
            return await _context.FormSubmissions
                .Include(fs => fs.Template)
                .Include(fs => fs.SubmitterUser)
                .OrderByDescending(fs => fs.SubmittedAt)
                .ToListAsync();
        }

        public async Task<FormSubmission?> GetSubmissionByIdAsync(Guid submissionId)
        {
            return await _context.FormSubmissions
                .Include(fs => fs.Template)
                .Include(fs => fs.SubmitterUser)
                .FirstOrDefaultAsync(fs => fs.SubmissionId == submissionId);
        }

        public async Task CreateSubmissionAsync(FormSubmission submission)
        {
            submission.SubmittedAt = DateTime.UtcNow;
            _context.FormSubmissions.Add(submission);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSubmissionAsync(Guid submissionId)
        {
            var submission = await _context.FormSubmissions.FindAsync(submissionId);
            if (submission != null)
            {
                _context.FormSubmissions.Remove(submission);
                await _context.SaveChangesAsync();
            }
        }
    }
}