using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;
using AcademiaDigital.Application.Interfaces;
using AcademiaDigital.Application.Services;
using AcademiaDigital.Infrastructure.Persistence;
using AcademiaDigital.Infrastructure.Persistence.Repositories;
using AcademiaDigital.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AcademiaDigital.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // EF Core + PostgreSQL
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositorios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<ICertificateRequestRepository, CertificateRequestRepository>();
        services.AddScoped<ICareerRepository, CareerRepository>();
        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<ICourseTypeRepository, CourseTypeRepository>();
        services.AddScoped<IStudyPlanRepository, StudyPlanRepository>();
        services.AddScoped<IStudyPlanCourseRepository, StudyPlanCourseRepository>();
        services.AddScoped<ICoursePrerequisiteRepository, CoursePrerequisiteRepository>();
        services.AddScoped<IStudentAcademicRepository, StudentAcademicRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IStudentCareerRepository, StudentCareerRepository>();
        services.AddScoped<IRematriculationRepository, RematriculationRepository>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<ITeacherDocumentRepository, TeacherDocumentRepository>();
        services.AddScoped<ITeacherAssignmentRepository, TeacherAssignmentRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IGradebookRepository, GradebookRepository>();
        services.AddScoped<IExamTableRepository, ExamTableRepository>();
        services.AddScoped<IAdministrativeRepository, AdministrativeRepository>();
        services.AddScoped<ICooperativeEntityRepository, CooperativeEntityRepository>();
        services.AddScoped<ICommunicationRepository, CommunicationRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IEnrollmentPeriodRepository, EnrollmentPeriodRepository>();
        services.AddScoped<IAdmissionRepository, AdmissionRepository>();
        services.AddScoped<ICommissionRepository, CommissionRepository>();
        services.AddScoped<ITeacherContestRepository, TeacherContestRepository>();
        services.AddScoped<IContestApplicationRepository, ContestApplicationRepository>();
        services.AddScoped<ITeachingPositionRepository, TeachingPositionRepository>();
        services.AddScoped<IFinanceRepository, FinanceRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Servicios
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IStudentManagementService, StudentManagementService>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IAdmissionAgreementPdfGenerator, SimpleAdmissionAgreementPdfGenerator>();
        services.AddSingleton<IAdmissionNotificationSender, LocalAdmissionNotificationSender>();
        services.AddSingleton<IAdmissionChallengeVerifier, ConfigurableAdmissionChallengeVerifier>();
        services.AddSingleton<IAttendanceReportGenerator, SimpleAttendanceReportGenerator>();
        services.AddSingleton<ICertificatePdfGenerator, SimpleCertificatePdfGenerator>();
        services.AddSingleton<IReceiptPdfGenerator, SimpleReceiptPdfGenerator>();

        return services;
    }
}
