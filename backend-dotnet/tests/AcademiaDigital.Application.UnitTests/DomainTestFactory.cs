using System.Reflection;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Application.UnitTests;

/// <summary>
/// Helpers de test para fabricar entidades de dominio RICAS (setters privados) en estados
/// arbitrarios (incluido fijar Id o forzar Status) sin exponer setters en el dominio de
/// producción. Solo en el proyecto de tests. Usa las factories del dominio + reflexión únicamente
/// para el Id (que en producción asigna EF/DB).
/// </summary>
internal static class DomainTestFactory
{
    public static Career Career(int id = 0, string name = "Career", string code = "CAR", int durationYears = 3, bool isActive = true)
    {
        var career = AcademiaDigital.Domain.Entities.Career.Create(name, code, null, durationYears);
        if (!isActive) career.Deactivate();
        if (id != 0) SetId(career, id);
        return career;
    }

    public static StudyPlan StudyPlan(int id = 0, int careerId = 0, string code = "PLAN", string name = "Plan",
        int versionNumber = 1, StudyPlanStatus status = StudyPlanStatus.Draft)
    {
        var plan = AcademiaDigital.Domain.Entities.StudyPlan.Create(careerId, code, name, versionNumber);
        if (status == StudyPlanStatus.Active) plan.Activate();
        else if (status == StudyPlanStatus.Archived) plan.Archive();
        if (id != 0) SetId(plan, id);
        return plan;
    }

    public static CoursePrerequisite Prerequisite(int courseId, int prerequisiteCourseId,
        int studyPlanId = 7, PrerequisiteType type = PrerequisiteType.Strict,
        MinimumRequiredStatus requiredStatus = MinimumRequiredStatus.Approved, bool isActive = true)
    {
        var p = CoursePrerequisite.Create(studyPlanId, courseId, prerequisiteCourseId, type, requiredStatus);
        if (!isActive) p.Deactivate();
        return p;
    }

    private static void SetId<T>(T entity, int id)
    {
        var prop = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!;
        prop.SetValue(entity, id);
    }
}
