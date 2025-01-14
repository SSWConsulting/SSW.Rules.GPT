namespace SharedClasses;

public enum AvailableGptModels
{
    Gpt35Turbo = OpenAI.ObjectModels.Models.Model.Gpt_3_5_Turbo,
    Gpt4 = OpenAI.ObjectModels.Models.Model.Gpt_4
}

public static class AvailableGptModelsExtensions
{
    public static string AvailableModelEnumToString(this AvailableGptModels gptModel)
    {
        return gptModel switch
        {
            AvailableGptModels.Gpt35Turbo => "GPT-3.5 Turbo",
            AvailableGptModels.Gpt4 => "GPT-4",
            _ => throw new ArgumentOutOfRangeException(nameof(gptModel), gptModel, null)
        };
    }
}
