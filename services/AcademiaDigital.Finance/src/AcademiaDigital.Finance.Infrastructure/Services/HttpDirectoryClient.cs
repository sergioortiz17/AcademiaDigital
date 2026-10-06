using System.Net.Http.Json;
using AcademiaDigital.Finance.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AcademiaDigital.Finance.Infrastructure.Services;

// HTTP client for display-name lookups against the monolito. It caches successful lookups
// in memory (TTL 5 min) and, if the monolito is unreachable or returns an error, DEGRADES
// to returning the id rendered as text. It never throws for a failed lookup — a missing
// display name must never break a Finance request (ADR 0001 / README).
//
// Endpoints consumed:
//   GET {monolith}/api/v1/careers/{id}                 -> exists today.
//   GET {monolith}/api/v1/users/{id}/display-name       -> may not exist yet; degrades to id.
//   GET {monolith}/api/v1/students/{id}/display          -> may not exist yet; degrades to id.
public sealed class HttpDirectoryClient(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<HttpDirectoryClient> logger) : IDirectoryClient
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public Task<CareerInfo> GetCareerAsync(int careerId, CancellationToken ct = default)
        => GetOrDegradeAsync(
            $"career:{careerId}",
            async token =>
            {
                var dto = await httpClient.GetFromJsonAsync<CareerResponse>($"api/v1/careers/{careerId}", token);
                return dto is null
                    ? null
                    : new CareerInfo(careerId, string.IsNullOrWhiteSpace(dto.Name) ? $"Carrera {careerId}" : dto.Name, dto.Code);
            },
            () => new CareerInfo(careerId, $"Carrera {careerId}", null),
            ct);

    public Task<UserInfo> GetUserAsync(long userId, CancellationToken ct = default)
        => GetOrDegradeAsync(
            $"user:{userId}",
            async token =>
            {
                var dto = await httpClient.GetFromJsonAsync<UserResponse>($"api/v1/users/{userId}/display-name", token);
                return dto is null || string.IsNullOrWhiteSpace(dto.FullName)
                    ? null
                    : new UserInfo(userId, dto.FullName);
            },
            () => new UserInfo(userId, $"Usuario {userId}"),
            ct);

    public Task<StudentInfo> GetStudentAsync(long studentId, CancellationToken ct = default)
        => GetOrDegradeAsync(
            $"student:{studentId}",
            async token =>
            {
                var dto = await httpClient.GetFromJsonAsync<StudentResponse>($"api/v1/students/{studentId}/display", token);
                return dto is null || string.IsNullOrWhiteSpace(dto.FullName)
                    ? null
                    : new StudentInfo(studentId, dto.FullName, dto.Legajo);
            },
            () => new StudentInfo(studentId, $"Alumno {studentId}", null),
            ct);

    private async Task<T> GetOrDegradeAsync<T>(
        string cacheKey,
        Func<CancellationToken, Task<T?>> fetch,
        Func<T> degrade,
        CancellationToken ct)
        where T : class
    {
        if (cache.TryGetValue(cacheKey, out T? cached) && cached is not null)
            return cached;
        try
        {
            var value = await fetch(ct);
            if (value is not null)
            {
                cache.Set(cacheKey, value, CacheTtl);
                return value;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Directory lookup failed for {CacheKey}; degrading to id.", cacheKey);
        }
        return degrade();
    }

    private sealed record CareerResponse(int CareerId, string Name, string? Code);
    private sealed record UserResponse(long UserId, string FullName);
    private sealed record StudentResponse(long StudentId, string FullName, string? Legajo);
}
