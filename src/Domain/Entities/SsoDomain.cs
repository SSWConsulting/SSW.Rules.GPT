namespace Domain.Entities;

/// <summary>
/// Auth: Manages SSO email address domain mapping to an SSO Identity Provider.
/// </summary>
public partial class SsoDomain
{
    public Guid Id { get; set; }

    public Guid SsoProviderId { get; set; }

    public string Domain { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual SsoProvider SsoProvider { get; set; } = null!;
}
