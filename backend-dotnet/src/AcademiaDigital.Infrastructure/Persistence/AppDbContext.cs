using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ActiveSession> ActiveSessions => Set<ActiveSession>();
    public DbSet<Career> Careers => Set<Career>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<StudyPlan> StudyPlans => Set<StudyPlan>();
    public DbSet<StudyPlanCourse> StudyPlanCourses => Set<StudyPlanCourse>();
    public DbSet<CourseType> CourseTypes => Set<CourseType>();
    public DbSet<CourseApprovalRule> CourseApprovalRules => Set<CourseApprovalRule>();
    public DbSet<CoursePrerequisite> CoursePrerequisites => Set<CoursePrerequisite>();
    public DbSet<StudentStudyPlan> StudentStudyPlans => Set<StudentStudyPlan>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentCareer> StudentCareers => Set<StudentCareer>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Administrative> Administratives => Set<Administrative>();
    public DbSet<CooperativeEntity> CooperativeEntities => Set<CooperativeEntity>();
    public DbSet<Communication> Communications => Set<Communication>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<TeacherContest> TeacherContests => Set<TeacherContest>();
    public DbSet<ContestApplication> ContestApplications => Set<ContestApplication>();
    public DbSet<TeachingPosition> TeachingPositions => Set<TeachingPosition>();
    public DbSet<CertificateRequest> CertificateRequests => Set<CertificateRequest>();
    public DbSet<EnrollmentPeriod> EnrollmentPeriods => Set<EnrollmentPeriod>();
    public DbSet<AcademicEvent> AcademicEvents => Set<AcademicEvent>();
    public DbSet<StudentStatusHistory> StudentStatusHistory => Set<StudentStatusHistory>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<StudentAcademicAssignment> StudentAcademicAssignments => Set<StudentAcademicAssignment>();
    public DbSet<DocumentRequirement> DocumentRequirements => Set<DocumentRequirement>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<StudentScholarship> StudentScholarships => Set<StudentScholarship>();
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<StudentCustomFieldValue> StudentCustomFieldValues => Set<StudentCustomFieldValue>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ActiveSessionConfiguration());
        modelBuilder.ApplyConfiguration(new CertificateRequestConfiguration());
        modelBuilder.ApplyConfiguration(new CareerConfiguration());
        modelBuilder.ApplyConfiguration(new CourseConfiguration());
        modelBuilder.ApplyConfiguration(new StudyPlanConfiguration());
        modelBuilder.ApplyConfiguration(new StudyPlanCourseConfiguration());
        modelBuilder.ApplyConfiguration(new CourseTypeConfiguration());
        modelBuilder.ApplyConfiguration(new CourseApprovalRuleConfiguration());
        modelBuilder.ApplyConfiguration(new CoursePrerequisiteConfiguration());
        modelBuilder.ApplyConfiguration(new StudentStudyPlanConfiguration());
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new StudentCareerConfiguration());
        modelBuilder.ApplyConfiguration(new TeacherConfiguration());
        modelBuilder.ApplyConfiguration(new AdministrativeConfiguration());
        modelBuilder.ApplyConfiguration(new CooperativeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CommunicationConfiguration());
        modelBuilder.ApplyConfiguration(new EnrollmentConfiguration());
        modelBuilder.ApplyConfiguration(new TeacherContestConfiguration());
        modelBuilder.ApplyConfiguration(new ContestApplicationConfiguration());
        modelBuilder.ApplyConfiguration(new TeachingPositionConfiguration());
        modelBuilder.ApplyConfiguration(new EnrollmentPeriodConfiguration());
        modelBuilder.ApplyConfiguration(new StudentStatusHistoryConfiguration());
        modelBuilder.ApplyConfiguration(new CommissionConfiguration());
        modelBuilder.ApplyConfiguration(new StudentAcademicAssignmentConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentRequirementConfiguration());
        modelBuilder.ApplyConfiguration(new StudentDocumentConfiguration());
        modelBuilder.ApplyConfiguration(new ScholarshipConfiguration());
        modelBuilder.ApplyConfiguration(new StudentScholarshipConfiguration());
        modelBuilder.ApplyConfiguration(new CustomFieldDefinitionConfiguration());
        modelBuilder.ApplyConfiguration(new StudentCustomFieldValueConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
