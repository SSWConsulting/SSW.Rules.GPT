﻿using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels;
using OpenAI.GPT3.ObjectModels.RequestModels;
using WebUI.Models;

namespace WebUI.Services;

public class ApiKeyValidationService
{
    //private readonly OpenAIService _openAiService;

    public async Task<bool> ValidateApiKey(string apiKey, AvailableGptModels gptModel)
    {
        var model = (OpenAI.GPT3.ObjectModels.Models.Model)gptModel;
        var customAiService = new OpenAIService(new OpenAiOptions() { ApiKey = apiKey });
        var completionResult = await customAiService.ChatCompletion.CreateCompletion(
            new ChatCompletionCreateRequest()
            {
                Messages = new List<ChatMessage> { new ChatMessage("user", "a") },
                MaxTokens = 1,
                Temperature = (float)0.5
            },
            model.EnumToString()
        );

        if (completionResult.Successful is false)
        {
            return false;
        }

        return true;
    }
}
