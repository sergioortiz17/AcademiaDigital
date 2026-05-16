using AcademiaDigital.Domain.Interfaces.Repositories;
using AcademiaDigital.Domain.Interfaces.Services;
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
        // EF Core + SQL Server
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Repositorios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<ICareerRepository, CareerRepository>();
        services.AddScoped<ISubjectRepository, SubjectRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<ITeacherRepository, TeacherRepository>();
        services.AddScoped<IAdministrativeRepository, AdministrativeRepository>();
        services.AddScoped<ICooperativeEntityRepository, CooperativeEntityRepository>();
        services.AddScoped<ICommunicationRepository, CommunicationRepository>();
        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<ITeacherContestRepository, TeacherContestRepository>();
        services.AddScoped<IContestApplicationRepository, ContestApplicationRepository>();
        services.AddScoped<ITeachingPositionRepository, TeachingPositionRepository>();
        services.AddScoped<IMateriaRepository, MateriaRepository>();

        // Servicios
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
