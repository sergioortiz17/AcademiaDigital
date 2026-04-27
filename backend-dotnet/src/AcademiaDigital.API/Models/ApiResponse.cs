namespace AcademiaDigital.API.Models;

public record ApiResponse<T>(bool Success, T? Data, string? Msg = null);

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data) => new(true, data);
    public static ApiResponse<object> Fail(string msg) => new(false, null, msg);
}
