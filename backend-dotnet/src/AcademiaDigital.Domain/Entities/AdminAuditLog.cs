namespace AcademiaDigital.Domain.Entities;

public class AdminAuditLog
{
    public long Id { get; set; }
    public long ActorUserId { get; set; }
    public long? TargetUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User ActorUser { get; set; } = null!;
    public User? TargetUser { get; set; }
}
