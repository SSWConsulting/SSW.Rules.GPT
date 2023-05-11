using System.Net;

namespace Domain.Entities;

/// <summary>
/// Auth: Contains SAML Relay State information for each Service Provider initiated login.
/// </summary>
public partial class SamlRelayState
{
    public Guid Id { get; set; }

    public Guid SsoProviderId { get; set; }

    public string RequestId { get; set; } = null!;

    public string? ForEmail { get; set; }

    public string? RedirectTo { get; set; }

    public IPAddress? FromIpAddress { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual SsoProvider SsoProvider { get; set; } = null!;
}
