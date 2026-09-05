using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> builder)
    {
        builder.ToTable("AttendanceSessions", table =>
        {
            table.HasCheckConstraint("CK_AttendanceSessions_Units", "[units] >= 1 AND [units] <= 12");
            table.HasCheckConstraint("CK_AttendanceSessions_TimeRange", "([scope] = 0 AND [start_time] IS NOT NULL AND [end_time] IS NOT NULL AND [end_time] > [start_time]) OR ([scope] = 1 AND [start_time] IS NULL AND [end_time] IS NULL AND [units] = 1)");
        });
        builder.HasKey(session => session.Id);
        builder.Property(session => session.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(session => session.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(100).IsRequired();
        builder.Property(session => session.TeachingPositionId).HasColumnName("teaching_position_id");
        builder.Property(session => session.CourseId).HasColumnName("course_id");
        builder.Property(session => session.CommissionId).HasColumnName("commission_id");
        builder.Property(session => session.AcademicYear).HasColumnName("academic_year");
        builder.Property(session => session.Semester).HasColumnName("semester");
        builder.Property(session => session.SessionDate).HasColumnName("session_date");
        builder.Property(session => session.StartTime).HasColumnName("start_time");
        builder.Property(session => session.EndTime).HasColumnName("end_time");
        builder.Property(session => session.Scope).HasColumnName("scope").HasConversion<int>();
        builder.Property(session => session.Units).HasColumnName("units");
        builder.Property(session => session.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(session => session.EditDeadlineUtc).HasColumnName("edit_deadline_utc");
        builder.Property(session => session.IsAdministrativelyReopened).HasColumnName("is_administratively_reopened");
        builder.Property(session => session.CreatedAt).HasColumnName("created_at");
        builder.Property(session => session.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(session => session.ClosedAt).HasColumnName("closed_at");
        builder.Property(session => session.ClosedByUserId).HasColumnName("closed_by_user_id");
        builder.HasIndex(session => session.IdempotencyKey).IsUnique();
        builder.HasIndex(session => new
        {
            session.CourseId,
            session.CommissionId,
            session.AcademicYear,
            session.Semester,
            session.SessionDate,
            session.StartTime,
            session.Scope
        }).IsUnique().HasFilter(null);
        builder.HasOne(session => session.TeachingPosition).WithMany().HasForeignKey(session => session.TeachingPositionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(session => session.Course).WithMany().HasForeignKey(session => session.CourseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(session => session.Commission).WithMany().HasForeignKey(session => session.CommissionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(session => session.CreatedByUser).WithMany().HasForeignKey(session => session.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(session => session.ClosedByUser).WithMany().HasForeignKey(session => session.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(record => record.AttendanceSessionId).HasColumnName("attendance_session_id");
        builder.Property(record => record.EnrollmentId).HasColumnName("enrollment_id");
        builder.Property(record => record.StudentId).HasColumnName("student_id");
        builder.Property(record => record.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(record => record.Notes).HasColumnName("notes").HasMaxLength(500);
        builder.Property(record => record.UpdatedAt).HasColumnName("updated_at");
        builder.Property(record => record.UpdatedByUserId).HasColumnName("updated_by_user_id");
        builder.HasIndex(record => new { record.AttendanceSessionId, record.EnrollmentId }).IsUnique();
        builder.HasIndex(record => new { record.StudentId, record.AttendanceSessionId });
        builder.HasOne(record => record.AttendanceSession).WithMany(session => session.Records).HasForeignKey(record => record.AttendanceSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(record => record.Enrollment).WithMany().HasForeignKey(record => record.EnrollmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(record => record.Student).WithMany().HasForeignKey(record => record.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(record => record.UpdatedByUser).WithMany().HasForeignKey(record => record.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceJustificationConfiguration : IEntityTypeConfiguration<AttendanceJustification>
{
    public void Configure(EntityTypeBuilder<AttendanceJustification> builder)
    {
        builder.ToTable("AttendanceJustifications");
        builder.HasKey(justification => justification.Id);
        builder.Property(justification => justification.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(justification => justification.AttendanceRecordId).HasColumnName("attendance_record_id");
        builder.Property(justification => justification.PreviousStatus).HasColumnName("previous_status").HasConversion<int>();
        builder.Property(justification => justification.Category).HasColumnName("category").HasMaxLength(100).IsRequired();
        builder.Property(justification => justification.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
        builder.Property(justification => justification.EvidenceUrl).HasColumnName("evidence_url").HasMaxLength(1000);
        builder.Property(justification => justification.IsCurrent).HasColumnName("is_current");
        builder.Property(justification => justification.CreatedAt).HasColumnName("created_at");
        builder.Property(justification => justification.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.HasIndex(justification => justification.AttendanceRecordId).IsUnique().HasFilter("is_current");
        builder.HasOne(justification => justification.AttendanceRecord).WithMany(record => record.Justifications).HasForeignKey(justification => justification.AttendanceRecordId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(justification => justification.CreatedByUser).WithMany().HasForeignKey(justification => justification.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AttendanceSessionReopeningConfiguration : IEntityTypeConfiguration<AttendanceSessionReopening>
{
    public void Configure(EntityTypeBuilder<AttendanceSessionReopening> builder)
    {
        builder.ToTable("AttendanceSessionReopenings");
        builder.HasKey(reopening => reopening.Id);
        builder.Property(reopening => reopening.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(reopening => reopening.AttendanceSessionId).HasColumnName("attendance_session_id");
        builder.Property(reopening => reopening.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
        builder.Property(reopening => reopening.ReopenedAt).HasColumnName("reopened_at");
        builder.Property(reopening => reopening.ReopenedByUserId).HasColumnName("reopened_by_user_id");
        builder.HasIndex(reopening => new { reopening.AttendanceSessionId, reopening.ReopenedAt });
        builder.HasOne(reopening => reopening.AttendanceSession).WithMany(session => session.Reopenings).HasForeignKey(reopening => reopening.AttendanceSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(reopening => reopening.ReopenedByUser).WithMany().HasForeignKey(reopening => reopening.ReopenedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
