namespace Application.Contracts;

public interface ISemanticKernelService
{
    public Task<string> GetConversationTitle(string question);
}