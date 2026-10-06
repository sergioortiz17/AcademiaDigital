namespace AcademiaDigital.Finance.API.Models;

public sealed record ApiResponse(bool Success, string? Msg)
{
    public static ApiResponse Fail(string msg) => new(false, msg);
    public static ApiResponse Ok(string? msg = null) => new(true, msg);
}
