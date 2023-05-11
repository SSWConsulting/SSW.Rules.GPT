namespace Domain.Entities;

/// <summary>
/// stores metadata for pkce logins
/// </summary>
public partial class FlowState
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string AuthCode { get; set; } = null!;

    public string CodeChallenge { get; set; } = null!;

    public string ProviderType { get; set; } = null!;

    public string? ProviderAccessToken { get; set; }

    public string? ProviderRefreshToken { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string AuthenticationMethod { get; set; } = null!;
}
