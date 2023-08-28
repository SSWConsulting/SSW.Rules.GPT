namespace WebUI.Models;

public enum AvailableGptModels
{
    Gpt35Turbo = OpenAI.GPT3.ObjectModels.Models.Model.ChatGpt3_5Turbo,
    Gpt4 = OpenAI.GPT3.ObjectModels.Models.Model.Gpt_4
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
