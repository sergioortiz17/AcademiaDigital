namespace AcademiaDigital.Domain.Services;

/// <summary>Materia a programar: año de la carrera, orden en el plan y sus correlativas previas.</summary>
public sealed record SchedulableCourse(int CourseId, int YearNumber, int SortOrder, IReadOnlyCollection<int> PrerequisiteCourseIds);

/// <summary>Fecha propuesta para la mesa de una materia (null si no entró en el rango, con el motivo).</summary>
public sealed record ScheduledExam(int CourseId, DateOnly? Date, string? Reason);

/// <summary>
/// Propone fechas de mesas de examen dentro de un rango de días hábiles:
///  - una mesa por materia;
///  - dos materias del mismo año de la carrera nunca el mismo día (los alumnos pueden rendir ambas);
///  - una materia va siempre después de sus correlativas previas (si están en el mismo llamado);
///  - se reparte de forma pareja: cada materia toma el primer día libre a partir de su turno.
/// </summary>
public static class ExamScheduler
{
    public static IReadOnlyList<ScheduledExam> Propose(IReadOnlyList<SchedulableCourse> courses, IReadOnlyList<DateOnly> availableDays)
    {
        var days = availableDays.Distinct().OrderBy(d => d).ToArray();
        var byId = courses.ToDictionary(c => c.CourseId);
        var order = TopologicalOrder(courses, byId);
        var dayIndex = new Dictionary<int, int>();          // materia -> índice del día asignado
        var takenByYear = new Dictionary<int, HashSet<int>>(); // año -> días ocupados
        var result = new Dictionary<int, ScheduledExam>();

        foreach (var course in order)
        {
            // Debe ir después de todas sus correlativas que estén en este llamado.
            var earliest = 0;
            var blocked = false;
            foreach (var pre in course.PrerequisiteCourseIds.Where(byId.ContainsKey))
            {
                if (!dayIndex.TryGetValue(pre, out var preDay)) { blocked = true; break; }
                earliest = Math.Max(earliest, preDay + 1);
            }
            if (blocked)
            {
                result[course.CourseId] = new(course.CourseId, null, "Su correlativa previa no entró en el rango de fechas.");
                continue;
            }
            var taken = takenByYear.TryGetValue(course.YearNumber, out var set) ? set : takenByYear[course.YearNumber] = [];
            var chosen = Enumerable.Range(earliest, Math.Max(0, days.Length - earliest)).Cast<int?>()
                .FirstOrDefault(i => !taken.Contains(i!.Value));
            if (chosen is null)
            {
                result[course.CourseId] = new(course.CourseId, null, "No quedan días libres para su año en el rango elegido.");
                continue;
            }
            taken.Add(chosen.Value);
            dayIndex[course.CourseId] = chosen.Value;
            result[course.CourseId] = new(course.CourseId, days[chosen.Value], null);
        }
        return courses.Select(c => result[c.CourseId]).ToArray();
    }

    /// <summary>Año, orden del plan y correlativas primero (orden topológico estable).</summary>
    private static List<SchedulableCourse> TopologicalOrder(IReadOnlyList<SchedulableCourse> courses, Dictionary<int, SchedulableCourse> byId)
    {
        var pending = courses.OrderBy(c => c.YearNumber).ThenBy(c => c.SortOrder).ThenBy(c => c.CourseId).ToList();
        var placed = new HashSet<int>();
        var ordered = new List<SchedulableCourse>();
        while (pending.Count > 0)
        {
            var next = pending.FirstOrDefault(c => c.PrerequisiteCourseIds.Where(byId.ContainsKey).All(placed.Contains))
                ?? pending[0]; // ciclo en las correlativas: se rompe por el orden del plan
            pending.Remove(next);
            placed.Add(next.CourseId);
            ordered.Add(next);
        }
        return ordered;
    }

    /// <summary>Días hábiles (lunes a viernes) del rango, sin los días bloqueados (feriados).</summary>
    public static IReadOnlyList<DateOnly> BusinessDays(DateOnly from, DateOnly to, IEnumerable<DateOnly> blocked)
    {
        if (to < from) throw new ArgumentException("La fecha final no puede ser anterior a la inicial.");
        if (to.DayNumber - from.DayNumber > 120) throw new ArgumentException("El rango de fechas puede ser de hasta 120 días.");
        var blockedSet = blocked.ToHashSet();
        var days = new List<DateOnly>();
        for (var d = from; d <= to; d = d.AddDays(1))
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !blockedSet.Contains(d)) days.Add(d);
        return days;
    }
}
