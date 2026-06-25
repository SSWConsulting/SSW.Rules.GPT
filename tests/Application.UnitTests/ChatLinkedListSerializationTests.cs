using Newtonsoft.Json;
using SharedClasses;

namespace Application.UnitTests;

// Regression coverage for conversation-history persistence. The WebUI serializes a
// ChatLinkedList with PreserveReferencesHandling.All before POSTing it, and the server
// (ChatHistoryService.ValidateConversation) round-trips it with JsonConvert.DeserializeObject.
// ChatLinkedListItem previously had only parameterized constructors, so Newtonsoft threw
// "Unable to find a constructor to use", which broke save/load for signed-in users.
public class ChatLinkedListSerializationTests
{
    // Mirrors the client's persistence settings (WebUI MessagingService).
    private static readonly JsonSerializerSettings ClientSettings =
        new() { PreserveReferencesHandling = PreserveReferencesHandling.All };

    private static ChatLinkedList BuildConversation()
    {
        var list = new ChatLinkedList();
        var question = list.Add(
            new ChatMessage("user", "How do I write a good commit message?"),
            AvailableGptModels.Gpt55);
        var answer = list.AddAfter(
            new ChatMessage("assistant", "Use the imperative mood 🙂"),
            question,
            AvailableGptModels.Gpt55);
        // Exercises the Left/Right/Previous/Next reference cycle that requires $ref handling.
        list.AddRight(
            new ChatMessage("assistant", "Or describe the why, not the what"),
            answer,
            AvailableGptModels.Gpt55);
        return list;
    }

    [Fact]
    public void DeserializeObject_WhenConversationSerializedWithPreserveReferences_DoesNotThrow()
    {
        var serialized = JsonConvert.SerializeObject(BuildConversation(), ClientSettings);

        var deserialize = () => JsonConvert.DeserializeObject<ChatLinkedList>(serialized);

        deserialize.Should().NotThrow();
    }

    [Fact]
    public void DeserializeObject_RoundTrippingConversation_PreservesMessagesAndModel()
    {
        var serialized = JsonConvert.SerializeObject(BuildConversation(), ClientSettings);

        var result = JsonConvert.DeserializeObject<ChatLinkedList>(serialized);

        result.Should().NotBeNull();
        result!.Should().HaveCount(3);
        result[0].Message.Role.Should().Be("user");
        result[0].Message.Content.Should().Be("How do I write a good commit message?");
        result[0].GptModel.Should().Be(AvailableGptModels.Gpt55);
        result[0].Next.Should().NotBeNull();
    }
}
