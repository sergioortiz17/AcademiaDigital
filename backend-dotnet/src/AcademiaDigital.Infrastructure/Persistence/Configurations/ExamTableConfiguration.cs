using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class ExamTableConfiguration : IEntityTypeConfiguration<ExamTable>
{
    public void Configure(EntityTypeBuilder<ExamTable> builder)
    {
        builder.ToTable("ExamTables", table =>
        {
            table.HasCheckConstraint("CK_ExamTables_CallNumber", "[call_number] >= 1 AND [call_number] <= 10");
            table.HasCheckConstraint("CK_ExamTables_Deadline", "[registration_deadline_utc] <= [exam_date_utc]");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100).IsRequired();
        builder.Property(item => item.CourseId).HasColumnName("course_id");
        builder.Property(item => item.AcademicYear).HasColumnName("academic_year");
        builder.Property(item => item.CallNumber).HasColumnName("call_number");
        builder.Property(item => item.ExamDateUtc).HasColumnName("exam_date_utc");
        builder.Property(item => item.RegistrationDeadlineUtc).HasColumnName("registration_deadline_utc");
        builder.Property(item => item.Location).HasColumnName("location").HasMaxLength(200).IsRequired();
        builder.Property(item => item.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(item => item.GradingStartedAt).HasColumnName("grading_started_at");
        builder.Property(item => item.GradingStartedByUserId).HasColumnName("grading_started_by_user_id");
        builder.Property(item => item.PublishedAt).HasColumnName("published_at");
        builder.Property(item => item.PublishedByUserId).HasColumnName("published_by_user_id");
        builder.HasIndex(item => item.IdempotencyKey).IsUnique();
        builder.HasIndex(item => new { item.CourseId, item.ExamDateUtc, item.CallNumber }).IsUnique();
        builder.HasOne(item => item.Course).WithMany().HasForeignKey(item => item.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.GradingStartedByUser).WithMany().HasForeignKey(item => item.GradingStartedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.PublishedByUser).WithMany().HasForeignKey(item => item.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ExamTribunalMemberConfiguration : IEntityTypeConfiguration<ExamTribunalMember>
{
    public void Configure(EntityTypeBuilder<ExamTribunalMember> builder)
    {
        builder.ToTable("ExamTribunalMembers");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.ExamTableId).HasColumnName("exam_table_id");
        builder.Property(item => item.TeacherId).HasColumnName("teacher_id");
        builder.Property(item => item.Role).HasColumnName("role").HasConversion<int>();
        builder.HasIndex(item => new { item.ExamTableId, item.TeacherId }).IsUnique();
        builder.HasIndex(item => new { item.ExamTableId, item.Role }).IsUnique().HasFilter("[role] = 0");
        builder.HasOne(item => item.ExamTable).WithMany(item => item.TribunalMembers).HasForeignKey(item => item.ExamTableId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Teacher).WithMany().HasForeignKey(item => item.TeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ExamRegistrationConfiguration : IEntityTypeConfiguration<ExamRegistration>
{
    public void Configure(EntityTypeBuilder<ExamRegistration> builder)
    {
        builder.ToTable("ExamRegistrations", table =>
            table.HasCheckConstraint("CK_ExamRegistrations_Attempt", "[attempt_number] >= 1"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.ExamTableId).HasColumnName("exam_table_id");
        builder.Property(item => item.EnrollmentId).HasColumnName("enrollment_id");
        builder.Property(item => item.StudentId).HasColumnName("student_id");
        builder.Property(item => item.AttemptNumber).HasColumnName("attempt_number");
        builder.Property(item => item.RegisteredAt).HasColumnName("registered_at");
        builder.Property(item => item.RegisteredByUserId).HasColumnName("registered_by_user_id");
        builder.Property(item => item.PreviousEnrollmentStatus).HasColumnName("previous_enrollment_status").HasConversion<int?>();
        builder.Property(item => item.PreviousFinalGrade).HasColumnName("previous_final_grade").HasPrecision(4, 2);
        builder.Property(item => item.ResultAppliedAt).HasColumnName("result_applied_at");
        builder.HasIndex(item => new { item.ExamTableId, item.EnrollmentId }).IsUnique();
        builder.HasIndex(item => new { item.EnrollmentId, item.AttemptNumber }).IsUnique();
        builder.HasOne(item => item.ExamTable).WithMany(item => item.Registrations).HasForeignKey(item => item.ExamTableId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.Enrollment).WithMany().HasForeignKey(item => item.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.Student).WithMany().HasForeignKey(item => item.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(item => item.RegisteredByUser).WithMany().HasForeignKey(item => item.RegisteredByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ExamGradeRevisionConfiguration : IEntityTypeConfiguration<ExamGradeRevision>
{
    public void Configure(EntityTypeBuilder<ExamGradeRevision> builder)
    {
        builder.ToTable("ExamGradeRevisions", table =>
            table.HasCheckConstraint("CK_ExamGradeRevisions_Grade", "[grade] IS NULL OR ([grade] >= 0 AND [grade] <= 10)"));
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.ExamRegistrationId).HasColumnName("exam_registration_id");
        builder.Property(item => item.Version).HasColumnName("version");
        builder.Property(item => item.IsCurrent).HasColumnName("is_current");
        builder.Property(item => item.Outcome).HasColumnName("outcome").HasConversion<int>();
        builder.Property(item => item.Grade).HasColumnName("grade").HasPrecision(4, 2);
        builder.Property(item => item.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(item => item.CreatedAt).HasColumnName("created_at");
        builder.Property(item => item.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.HasIndex(item => new { item.ExamRegistrationId, item.Version }).IsUnique();
        builder.HasIndex(item => item.ExamRegistrationId).IsUnique().HasFilter("[is_current] = 1");
        builder.HasOne(item => item.ExamRegistration).WithMany(item => item.GradeRevisions).HasForeignKey(item => item.ExamRegistrationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.CreatedByUser).WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ExamTableReopeningConfiguration : IEntityTypeConfiguration<ExamTableReopening>
{
    public void Configure(EntityTypeBuilder<ExamTableReopening> builder)
    {
        builder.ToTable("ExamTableReopenings");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(item => item.ExamTableId).HasColumnName("exam_table_id");
        builder.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
        builder.Property(item => item.ReopenedAt).HasColumnName("reopened_at");
        builder.Property(item => item.ReopenedByUserId).HasColumnName("reopened_by_user_id");
        builder.HasIndex(item => new { item.ExamTableId, item.ReopenedAt });
        builder.HasOne(item => item.ExamTable).WithMany(item => item.Reopenings).HasForeignKey(item => item.ExamTableId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(item => item.ReopenedByUser).WithMany().HasForeignKey(item => item.ReopenedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
