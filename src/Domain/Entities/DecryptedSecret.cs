namespace Domain.Entities;

public partial class DecryptedSecret
{
    public Guid? Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Secret { get; set; }

    public string? DecryptedSecret1 { get; set; }

    public Guid? KeyId { get; set; }

    public byte[]? Nonce { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
