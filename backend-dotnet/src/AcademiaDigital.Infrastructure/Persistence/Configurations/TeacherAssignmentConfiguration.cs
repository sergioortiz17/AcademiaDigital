using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public sealed class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherAssignment> builder)
    {
        builder.ToTable("TeacherAssignments");
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(assignment => assignment.TeachingPositionId).HasColumnName("teaching_position_id");
        builder.Property(assignment => assignment.TeacherId).HasColumnName("teacher_id");
        builder.Property(assignment => assignment.StartedOn).HasColumnName("started_on");
        builder.Property(assignment => assignment.EndedOn).HasColumnName("ended_on");
        builder.Property(assignment => assignment.IsCurrent).HasColumnName("is_current");
        builder.Property(assignment => assignment.AssignmentReason).HasColumnName("assignment_reason").HasMaxLength(500);
        builder.Property(assignment => assignment.EndReason).HasColumnName("end_reason").HasMaxLength(500);
        builder.Property(assignment => assignment.AssignedByUserId).HasColumnName("assigned_by_user_id");
        builder.Property(assignment => assignment.EndedByUserId).HasColumnName("ended_by_user_id");
        builder.Property(assignment => assignment.CreatedAt).HasColumnName("created_at");
        builder.Property(assignment => assignment.EndedAt).HasColumnName("ended_at");

        builder.HasIndex(assignment => assignment.TeachingPositionId)
            .IsUnique()
            .HasFilter("is_current");
        builder.HasIndex(assignment => new { assignment.TeacherId, assignment.IsCurrent });

        builder.HasOne(assignment => assignment.TeachingPosition)
            .WithMany(position => position.Assignments)
            .HasForeignKey(assignment => assignment.TeachingPositionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(assignment => assignment.Teacher)
            .WithMany()
            .HasForeignKey(assignment => assignment.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(assignment => assignment.AssignedByUser)
            .WithMany()
            .HasForeignKey(assignment => assignment.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(assignment => assignment.EndedByUser)
            .WithMany()
            .HasForeignKey(assignment => assignment.EndedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
