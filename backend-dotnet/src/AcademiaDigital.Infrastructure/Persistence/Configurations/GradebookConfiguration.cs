using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class GradebookConfiguration : IEntityTypeConfiguration<Gradebook>
{
    public void Configure(EntityTypeBuilder<Gradebook> builder)
    {
        builder.ToTable("Gradebooks");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100).IsRequired();
        builder.Property(item => item.TeachingPositionId).HasColumnName("teaching_position_id");
        builder.Property(item => item.CourseId).HasColumnName("course_id");
        builder.Property(item => item.CommissionId).HasColumnName("commission_id");
        builder.Property(item => item.AcademicYear).HasColumnName("academic_year");
        builder.Property(item => item.Semester).HasColumnName("semester");
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(item => item.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(item => item.SubmittedByUserId).HasColumnName("submitted_by_user_id");
        builder.Property(item => item.ApprovedAt).HasColumnName("approved_at");
        builder.Property(item => item.ApprovedByUserId).HasColumnName("approved_by_user_id");
        builder.Property(item => item.PublishedAt).HasColumnName("published_at");
        builder.Property(item => item.PublishedByUserId).HasColumnName("published_by_user_id");
        builder.Property(item => item.ClosedAt).HasColumnName("closed_at");
        builder.Property(item => item.ClosedByUserId).HasColumnName("closed_by_user_id");
        builder.HasIndex(item => item.IdempotencyKey).IsUnique();
        builder.HasIndex(item => new { item.CourseId, item.CommissionId, item.AcademicYear, item.Semester }).IsUnique();
        builder.HasOne(item => item.TeachingPosition).WithMany().HasForeignKey(item => item.TeachingPositionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Course).WithMany().HasForeignKey(item => item.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Commission).WithMany().HasForeignKey(item => item.CommissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.SubmittedByUser).WithMany().HasForeignKey(item => item.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ApprovedByUser).WithMany().HasForeignKey(item => item.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PublishedByUser).WithMany().HasForeignKey(item => item.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.ClosedByUser).WithMany().HasForeignKey(item => item.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GradebookEvaluationConfiguration : IEntityTypeConfiguration<GradebookEvaluation>
{
    public void Configure(EntityTypeBuilder<GradebookEvaluation> builder)
    {
        builder.ToTable("GradebookEvaluations", table =>
        {
            table.HasCheckConstraint("CK_GradebookEvaluations_Weight", "[weight_percentage] > 0 AND [weight_percentage] <= 100");
            table.HasCheckConstraint("CK_GradebookEvaluations_Maximum", "[maximum_score] > 0 AND [maximum_score] <= 100");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.GradebookId).HasColumnName("gradebook_id");
        builder.Property(item => item.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(item => item.WeightPercentage).HasColumnName("weight_percentage").HasPrecision(5, 2);
        builder.Property(item => item.MaximumScore).HasColumnName("maximum_score").HasPrecision(5, 2);
        builder.Property(item => item.DisplayOrder).HasColumnName("display_order");
        builder.HasIndex(item => new { item.GradebookId, item.Name }).IsUnique();
        builder.HasIndex(item => new { item.GradebookId, item.DisplayOrder }).IsUnique();
        builder.HasOne(item => item.Gradebook).WithMany(item => item.Evaluations).HasForeignKey(item => item.GradebookId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class GradeEntryRevisionConfiguration : IEntityTypeConfiguration<GradeEntryRevision>
{
    public void Configure(EntityTypeBuilder<GradeEntryRevision> builder)
    {
        builder.ToTable("GradeEntryRevisions", table =>
            table.HasCheckConstraint("CK_GradeEntryRevisions_Score", "[score] >= 0 AND [score] <= 100"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.GradebookId).HasColumnName("gradebook_id");
        builder.Property(item => item.EvaluationId).HasColumnName("evaluation_id");
        builder.Property(item => item.EnrollmentId).HasColumnName("enrollment_id");
        builder.Property(item => item.StudentId).HasColumnName("student_id");
        builder.Property(item => item.Version).HasColumnName("version");
        builder.Property(item => item.IsCurrent).HasColumnName("is_current");
        builder.Property(item => item.Score).HasColumnName("score").HasPrecision(5, 2);
        builder.Property(item => item.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.HasIndex(item => new { item.EvaluationId, item.EnrollmentId, item.Version }).IsUnique();
        builder.HasIndex(item => new { item.EvaluationId, item.EnrollmentId }).IsUnique().HasFilter("is_current");
        builder.HasIndex(item => new { item.GradebookId, item.StudentId });
        builder.HasOne(item => item.Gradebook).WithMany(item => item.GradeRevisions).HasForeignKey(item => item.GradebookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Evaluation).WithMany(item => item.GradeRevisions).HasForeignKey(item => item.EvaluationId).OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(item => item.Enrollment).WithMany().HasForeignKey(item => item.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Student).WithMany().HasForeignKey(item => item.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GradebookReopeningConfiguration : IEntityTypeConfiguration<GradebookReopening>
{
    public void Configure(EntityTypeBuilder<GradebookReopening> builder)
    {
        builder.ToTable("GradebookReopenings");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.GradebookId).HasColumnName("gradebook_id");
        builder.Property(item => item.PreviousStatus).HasColumnName("previous_status").HasConversion<int>();
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
        builder.Property(item => item.ReopenedAt).HasColumnName("reopened_at");
        builder.Property(item => item.ReopenedByUserId).HasColumnName("reopened_by_user_id");
        builder.HasIndex(item => new { item.GradebookId, item.ReopenedAt });
        builder.HasOne(item => item.Gradebook).WithMany(item => item.Reopenings).HasForeignKey(item => item.GradebookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.ReopenedByUser).WithMany().HasForeignKey(item => item.ReopenedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
