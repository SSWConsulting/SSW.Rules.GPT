namespace Domain.Entities;

/// <summary>
/// Auth: Manages SSO identity provider information; see saml_providers for SAML.
/// </summary>
public partial class SsoProvider
{
    public Guid Id { get; set; }

    /// <summary>
    /// Auth: Uniquely identifies a SSO provider according to a user-chosen resource ID (case insensitive), useful in infrastructure as code.
    /// </summary>
    public string? ResourceId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<SamlProvider> SamlProviders { get; } = new List<SamlProvider>();

    public virtual ICollection<SamlRelayState> SamlRelayStates { get; } = new List<SamlRelayState>();

    public virtual ICollection<SsoDomain> SsoDomains { get; } = new List<SsoDomain>();
}
