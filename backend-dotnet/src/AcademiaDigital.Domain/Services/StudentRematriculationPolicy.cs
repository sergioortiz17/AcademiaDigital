using AcademiaDigital.Domain.Entities;
using AcademiaDigital.Domain.Enums;

namespace AcademiaDigital.Domain.Services;

public sealed class StudentRematriculationPolicy
{
    public void ValidateStudent(Student student)
    {
        if (student.Status is StudentStatus.Graduated or StudentStatus.Withdrawn)
            throw new InvalidOperationException("Graduated or withdrawn students cannot be rematriculated.");
        if (!student.User.IsActive)
            throw new InvalidOperationException("Inactive users cannot be rematriculated.");
    }

    public void ValidateTarget(
        StudentCareer studentCareer,
        StudyPlan studyPlan,
        Commission commission,
        int academicYear,
        int yearNumber)
    {
        if (!studentCareer.IsActive)
            throw new InvalidOperationException("Student career membership is inactive.");
        if (!studyPlan.IsActive || studyPlan.Status != StudyPlanStatus.Active)
            throw new InvalidOperationException("Rematriculation requires an active study plan.");
        if (studyPlan.CareerId != studentCareer.CareerId)
            throw new InvalidOperationException("Study plan and student career are incompatible.");
        if (!commission.IsActive)
            throw new InvalidOperationException("Rematriculation requires an active commission.");
        if (commission.CareerId != studentCareer.CareerId
            || commission.AcademicYear != academicYear
            || commission.YearNumber != yearNumber)
            throw new InvalidOperationException("Commission, career and academic cycle are incompatible.");
    }

    public void ValidateNextCycle(int? latestAcademicYear, int academicYear)
    {
        if (!latestAcademicYear.HasValue)
            throw new InvalidOperationException("Student has no prior academic assignment to rematriculate.");
        if (academicYear != latestAcademicYear.Value + 1)
            throw new InvalidOperationException("Rematriculation must target the next academic year.");
    }
}
