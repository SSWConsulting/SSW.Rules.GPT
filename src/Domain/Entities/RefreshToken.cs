namespace Domain.Entities;

/// <summary>
/// Auth: Store of tokens used to refresh JWT tokens once they expire.
/// </summary>
public partial class RefreshToken
{
    public Guid? InstanceId { get; set; }

    public long Id { get; set; }

    public string? Token { get; set; }

    public string? UserId { get; set; }

    public bool? Revoked { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? Parent { get; set; }

    public Guid? SessionId { get; set; }

    public virtual Session? Session { get; set; }
}
