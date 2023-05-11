namespace Domain.Entities;

/// <summary>
/// auth: stores metadata about factors
/// </summary>
public partial class MfaFactor
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? FriendlyName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Secret { get; set; }

    public virtual ICollection<MfaChallenge> MfaChallenges { get; } = new List<MfaChallenge>();

    public virtual User User { get; set; } = null!;
}
