using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AcademiaDigital.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();
    public DbSet<ActiveSession> ActiveSessions => Set<ActiveSession>();
    public DbSet<Career> Careers => Set<Career>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<SubjectPrerequisite> SubjectPrerequisites => Set<SubjectPrerequisite>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<Administrative> Administratives => Set<Administrative>();
    public DbSet<CooperativeEntity> CooperativeEntities => Set<CooperativeEntity>();
    public DbSet<Communication> Communications => Set<Communication>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<TeacherContest> TeacherContests => Set<TeacherContest>();
    public DbSet<ContestApplication> ContestApplications => Set<ContestApplication>();
    public DbSet<TeachingPosition> TeachingPositions => Set<TeachingPosition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new AdminAuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new ActiveSessionConfiguration());
        modelBuilder.ApplyConfiguration(new CareerConfiguration());
        modelBuilder.ApplyConfiguration(new SubjectConfiguration());
        modelBuilder.ApplyConfiguration(new SubjectPrerequisiteConfiguration());
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
        modelBuilder.ApplyConfiguration(new TeacherConfiguration());
        modelBuilder.ApplyConfiguration(new AdministrativeConfiguration());
        modelBuilder.ApplyConfiguration(new CooperativeEntityConfiguration());
        modelBuilder.ApplyConfiguration(new CommunicationConfiguration());
        modelBuilder.ApplyConfiguration(new EnrollmentConfiguration());
        modelBuilder.ApplyConfiguration(new TeacherContestConfiguration());
        modelBuilder.ApplyConfiguration(new ContestApplicationConfiguration());
        modelBuilder.ApplyConfiguration(new TeachingPositionConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
