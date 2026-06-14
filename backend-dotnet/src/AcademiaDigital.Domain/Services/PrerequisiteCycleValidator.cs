using AcademiaDigital.Domain.Entities;

namespace AcademiaDigital.Domain.Services;

public class PrerequisiteCycleValidator
{
    public bool WouldCreateCycle(
        IEnumerable<CoursePrerequisite> existingPrerequisites,
        int courseId,
        int prerequisiteCourseId)
    {
        var graph = existingPrerequisites
            .Where(p => p.IsActive)
            .GroupBy(p => p.CourseId)
            .ToDictionary(g => g.Key, g => g.Select(p => p.PrerequisiteCourseId).ToList());

        if (!graph.TryGetValue(courseId, out var currentPrerequisites))
        {
            currentPrerequisites = [];
            graph[courseId] = currentPrerequisites;
        }

        currentPrerequisites.Add(prerequisiteCourseId);

        var visited = new HashSet<int>();
        var stack = new Stack<int>([prerequisiteCourseId]);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (current == courseId) return true;
            if (!visited.Add(current)) continue;

            if (!graph.TryGetValue(current, out var next)) continue;
            foreach (var node in next) stack.Push(node);
        }

        return false;
    }
}
