namespace AcademiaDigital.Domain.Entities;

public class Communication
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public CommunicationType Type { get; set; } = CommunicationType.General;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public long AuthorId { get; set; }
    public User Author { get; set; } = null!;
}

public enum CommunicationType
{
    General = 0,
    Students = 1,
    Teachers = 2,
    Administrative = 3
}
