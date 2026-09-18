using System.Reflection;
using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.UnitTests;

/// <summary>
/// Helpers de test para fabricar entidades de dominio RICAS (setters privados) en estados
/// arbitrarios que los tests necesitan (incluido fijar Id o forzar Status), sin exponer esos
/// setters en el dominio de producción. Vive SOLO en el proyecto de tests.
///
/// Usa las factories/métodos del dominio para respetar las invariantes, y un pequeño helper por
/// reflexión únicamente para asignar el Id (que en producción lo pone EF/DB).
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

    /// <summary>Asigna el Id por reflexión (solo para tests; en producción lo asigna la DB).</summary>
    private static void SetId<T>(T entity, int id)
    {
        var prop = typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!;
        prop.SetValue(entity, id);
    }
}
