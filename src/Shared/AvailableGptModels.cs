namespace SharedClasses;

public enum AvailableGptModels
{
    Gpt54Nano = 1,
    Gpt56 = 2
}

public static class AvailableGptModelsExtensions
{
    public static string AvailableModelEnumToString(this AvailableGptModels gptModel)
    {
        return gptModel switch
        {
            AvailableGptModels.Gpt54Nano => "GPT-5.4 nano",
            AvailableGptModels.Gpt56 => "GPT-5.6",
            _ => throw new ArgumentOutOfRangeException(nameof(gptModel), gptModel, null)
        };
    }

    public static string ToModelId(this AvailableGptModels gptModel)
    {
        return gptModel switch
        {
            AvailableGptModels.Gpt54Nano => "gpt-5.4-nano",
            AvailableGptModels.Gpt56 => "gpt-5.6",
            _ => throw new ArgumentOutOfRangeException(nameof(gptModel), gptModel, null)
        };
    }

    /// <summary>
    /// Whether the model accepts a custom sampling temperature. GPT-5.6 only
    /// supports the API default (1) and returns HTTP 400 (unsupported_value) for
    /// any other value; GPT-5.4 nano accepts a custom temperature. Exhaustive by
    /// design (allowlist), so a new model forces a decision here rather than
    /// silently defaulting to "send temperature".
    /// Caveat: this keys off the enum. A <c>GPT_Model</c> / Azure-deployment config
    /// override that points at a different underlying model is not accounted for.
    /// </summary>
    public static bool SupportsCustomTemperature(this AvailableGptModels gptModel)
    {
        return gptModel switch
        {
            AvailableGptModels.Gpt54Nano => true,
            AvailableGptModels.Gpt56 => false,
            _ => throw new ArgumentOutOfRangeException(nameof(gptModel), gptModel, null)
        };
    }
}
