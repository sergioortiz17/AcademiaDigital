using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Services;

public sealed class StudentRematriculationPolicy
{
    public void ValidateStudent(Student student)
    {
        if (student.Status is StudentStatus.Graduated or StudentStatus.Withdrawn)
            throw new InvalidOperationException("Los estudiantes egresados o dados de baja no pueden rematricularse.");
        if (!student.User.IsActive)
            throw new InvalidOperationException("Los usuarios inactivos no pueden rematricularse.");
    }

    public void ValidateTarget(
        StudentCareer studentCareer,
        StudyPlan studyPlan,
        Commission commission,
        int academicYear,
        int yearNumber)
    {
        if (!studentCareer.IsActive)
            throw new InvalidOperationException("La membresía de la carrera del estudiante está inactiva.");
        if (!studyPlan.IsActive || studyPlan.Status != StudyPlanStatus.Active)
            throw new InvalidOperationException("La rematriculación requiere un plan de estudios activo.");
        if (studyPlan.CareerId != studentCareer.CareerId)
            throw new InvalidOperationException("El plan de estudios y la carrera del estudiante son incompatibles.");
        if (!commission.IsActive)
            throw new InvalidOperationException("La rematriculación requiere una comisión activa.");
        if (commission.CareerId != studentCareer.CareerId
            || commission.AcademicYear != academicYear
            || commission.YearNumber != yearNumber)
            throw new InvalidOperationException("La comisión, la carrera y el ciclo académico son incompatibles.");
    }

    public void ValidateNextCycle(int? latestAcademicYear, int academicYear)
    {
        if (!latestAcademicYear.HasValue)
            throw new InvalidOperationException("El estudiante no tiene una asignación académica previa para rematricular.");
        if (academicYear != latestAcademicYear.Value + 1)
            throw new InvalidOperationException("La rematriculación debe apuntar al siguiente año académico.");
    }
}
