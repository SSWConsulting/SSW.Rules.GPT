namespace Infrastructure.Options;

public class AzureOpenAiOptions
{
    public const string Section = "AzureOpenAi";

    public required string ApiKey { get; set; }
    public required string Endpoint { get; set; }
}