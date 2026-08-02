using AcademiaDigital.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AcademiaDigital.Infrastructure.Persistence.Configurations;

public class StudentStatusHistoryConfiguration : IEntityTypeConfiguration<StudentStatusHistory>
{
    public void Configure(EntityTypeBuilder<StudentStatusHistory> b)
    {
        b.ToTable("StudentStatusHistory");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        b.Property(x => x.PreviousStatus).HasConversion<int>();
        b.Property(x => x.NewStatus).HasConversion<int>();
        b.HasIndex(x => new { x.StudentId, x.ChangedAt });
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> b)
    {
        b.ToTable("Commissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Shift).HasMaxLength(20).IsRequired();
        b.HasIndex(x => new { x.CareerId, x.AcademicYear, x.Code }).IsUnique();
        b.HasOne(x => x.Career).WithMany().HasForeignKey(x => x.CareerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StudentAcademicAssignmentConfiguration : IEntityTypeConfiguration<StudentAcademicAssignment>
{
    public void Configure(EntityTypeBuilder<StudentAcademicAssignment> b)
    {
        b.ToTable("StudentAcademicAssignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.HasIndex(x => new { x.StudentId, x.AcademicYear });
        b.HasIndex(x => x.StudentCareerId).IsUnique().HasFilter("[IsCurrent] = 1");
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StudentCareer).WithMany(x => x.AcademicAssignments).HasForeignKey(x => x.StudentCareerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Career).WithMany().HasForeignKey(x => x.CareerId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StudyPlan).WithMany().HasForeignKey(x => x.StudyPlanId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Commission).WithMany().HasForeignKey(x => x.CommissionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssignedByUser).WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class DocumentRequirementConfiguration : IEntityTypeConfiguration<DocumentRequirement>
{
    public void Configure(EntityTypeBuilder<DocumentRequirement> b)
    {
        b.ToTable("DocumentRequirements");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => x.Code).IsUnique();
        b.HasOne(x => x.Career).WithMany().HasForeignKey(x => x.CareerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StudentDocumentConfiguration : IEntityTypeConfiguration<StudentDocument>
{
    public void Configure(EntityTypeBuilder<StudentDocument> b)
    {
        b.ToTable("StudentDocuments");
        b.HasKey(x => x.Id);
        b.Property(x => x.FileUrl).HasMaxLength(1000).IsRequired();
        b.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Observation).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => new { x.StudentId, x.DocumentRequirementId, x.SubmittedAt });
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DocumentRequirement).WithMany().HasForeignKey(x => x.DocumentRequirementId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReviewedByUser).WithMany().HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ScholarshipConfiguration : IEntityTypeConfiguration<Scholarship>
{
    public void Configure(EntityTypeBuilder<Scholarship> b)
    {
        b.ToTable("Scholarships");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class StudentScholarshipConfiguration : IEntityTypeConfiguration<StudentScholarship>
{
    public void Configure(EntityTypeBuilder<StudentScholarship> b)
    {
        b.ToTable("StudentScholarships");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Notes).HasMaxLength(500);
        b.HasIndex(x => new { x.StudentId, x.ScholarshipId, x.AcademicYear }).IsUnique();
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Scholarship).WithMany().HasForeignKey(x => x.ScholarshipId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> b)
    {
        b.ToTable("CustomFieldDefinitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(100).IsRequired();
        b.Property(x => x.Label).HasMaxLength(150).IsRequired();
        b.Property(x => x.DataType).HasConversion<int>();
        b.Property(x => x.OptionsJson).HasMaxLength(4000);
        b.HasIndex(x => x.Key).IsUnique();
    }
}

public class StudentCustomFieldValueConfiguration : IEntityTypeConfiguration<StudentCustomFieldValue>
{
    public void Configure(EntityTypeBuilder<StudentCustomFieldValue> b)
    {
        b.ToTable("StudentCustomFieldValues");
        b.HasKey(x => x.Id);
        b.Property(x => x.Value).HasMaxLength(4000);
        b.HasIndex(x => new { x.StudentId, x.CustomFieldDefinitionId }).IsUnique();
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CustomFieldDefinition).WithMany().HasForeignKey(x => x.CustomFieldDefinitionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.UpdatedByUser).WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
