using AcademiaDigital.Application.Dtos;

namespace AcademiaDigital.Application.UseCases.StudyPlanDiff;

/// <summary>
/// Pure diff of two course lists identified by course_code, git-diff style. No DB access, no
/// ASP.NET types — reused by both the persisted-plan-vs-plan diff and the CSV preview diff.
/// </summary>
public static class StudyPlanDiffCalculator
{
    public static StudyPlanDiffDto Compute(
        IReadOnlyList<PlanCourseSnapshot> planA,
        IReadOnlyList<PlanCourseSnapshot> planB)
    {
        var byCodeA = planA.ToDictionary(c => c.CourseCode);
        var byCodeB = planB.ToDictionary(c => c.CourseCode);

        var added = planB
            .Where(b => !byCodeA.ContainsKey(b.CourseCode))
            .Select(ToItem)
            .ToList();

        var removed = planA
            .Where(a => !byCodeB.ContainsKey(a.CourseCode))
            .Select(ToItem)
            .ToList();

        var modified = new List<ModifiedCourseDiffDto>();
        var unchangedCount = 0;

        foreach (var a in planA)
        {
            if (!byCodeB.TryGetValue(a.CourseCode, out var b)) continue;

            var fieldChanges = new List<FieldChangeDto>();
            AddIfChanged(fieldChanges, "year_number", a.YearNumber.ToString(), b.YearNumber.ToString());
            AddIfChanged(fieldChanges, "semester", a.Semester.ToString(), b.Semester.ToString());
            AddIfChanged(fieldChanges, "course_type_code", a.CourseTypeCode, b.CourseTypeCode);
            AddIfChanged(fieldChanges, "workload_hours", a.WorkloadHours?.ToString(), b.WorkloadHours?.ToString());
            AddIfChanged(fieldChanges, "is_mandatory", a.IsMandatory.ToString(), b.IsMandatory.ToString());
            AddIfChanged(fieldChanges, "name", a.Name, b.Name);

            var prereqAdded = b.Prerequisites.Except(a.Prerequisites).OrderBy(x => x).ToList();
            var prereqRemoved = a.Prerequisites.Except(b.Prerequisites).OrderBy(x => x).ToList();
            var prerequisiteChanges = prereqAdded.Count > 0 || prereqRemoved.Count > 0
                ? new PrerequisiteChangesDto { Added = prereqAdded, Removed = prereqRemoved }
                : null;

            if (fieldChanges.Count == 0 && prerequisiteChanges is null)
            {
                unchangedCount++;
                continue;
            }

            modified.Add(new ModifiedCourseDiffDto
            {
                CourseCode = a.CourseCode,
                Name = b.Name,
                FieldChanges = fieldChanges,
                PrerequisiteChanges = prerequisiteChanges
            });
        }

        return new StudyPlanDiffDto
        {
            AddedCourses = added,
            RemovedCourses = removed,
            ModifiedCourses = modified,
            UnchangedCourseCount = unchangedCount
        };
    }

    private static void AddIfChanged(List<FieldChangeDto> changes, string field, string? oldValue, string? newValue)
    {
        if (oldValue == newValue) return;
        changes.Add(new FieldChangeDto { Field = field, OldValue = oldValue, NewValue = newValue });
    }

    private static CourseDiffItemDto ToItem(PlanCourseSnapshot c) => new()
    {
        CourseCode = c.CourseCode,
        Name = c.Name,
        YearNumber = c.YearNumber,
        Semester = c.Semester,
        CourseTypeCode = c.CourseTypeCode,
        WorkloadHours = c.WorkloadHours,
        IsMandatory = c.IsMandatory,
        Prerequisites = c.Prerequisites
    };
}
