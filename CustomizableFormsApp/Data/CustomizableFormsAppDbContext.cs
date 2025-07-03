// Add these using directives
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CustomizableFormsApp.Models;

public class CustomizableFormsAppDbContext : IdentityDbContext<ApplicationUser>
{
    public CustomizableFormsAppDbContext(DbContextOptions<CustomizableFormsAppDbContext> options)
        : base(options) { }

    public DbSet<Template> Templates { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<QuestionOption> QuestionOptions { get; set; }
    public DbSet<FormSubmission> FormSubmissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure JSONB columns
        modelBuilder.Entity<Question>()
            .Property(q => q.ValidationRules)
            .HasColumnType("jsonb");

        modelBuilder.Entity<FormSubmission>()
            .Property(f => f.AnswersJson)
            .HasColumnType("jsonb");

        // Configure relationships
        modelBuilder.Entity<Template>()
            .HasMany(t => t.Questions)
            .WithOne(q => q.Template)
            .HasForeignKey(q => q.TemplateId);
    }
}