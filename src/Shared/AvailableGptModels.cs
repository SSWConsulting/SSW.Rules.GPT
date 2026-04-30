namespace SharedClasses;

public enum AvailableGptModels
{
    Gpt54Nano = 1,
    Gpt55 = 2
}

public static class AvailableGptModelsExtensions
{
    public static string AvailableModelEnumToString(this AvailableGptModels gptModel)
    {
        return gptModel switch
        {
            AvailableGptModels.Gpt54Nano => "GPT-5.4 nano",
            AvailableGptModels.Gpt55 => "GPT-5.5",
            _ => throw new ArgumentOutOfRangeException(nameof(gptModel), gptModel, null)
        };
    }

    public static string ToModelId(this AvailableGptModels gptModel)
    {
        return gptModel switch
        {
            AvailableGptModels.Gpt54Nano => "gpt-5.4-nano",
            AvailableGptModels.Gpt55 => "gpt-5.5",
            _ => throw new ArgumentOutOfRangeException(nameof(gptModel), gptModel, null)
        };
    }
}
