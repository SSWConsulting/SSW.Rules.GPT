using SharedClasses;

namespace Application.UnitTests;

public class AvailableGptModelsTests
{
    // Locks in the temperature decision: GPT-5.6 must not receive a custom
    // temperature (the API 400s on it); GPT-5.4 nano may. If a new model is added
    // to the enum, SupportsCustomTemperature throws until a case is added — the
    // allowlist forces that decision rather than silently sending a temperature.
    [Theory]
    [InlineData(AvailableGptModels.Gpt54Nano, true)]
    [InlineData(AvailableGptModels.Gpt56, false)]
    public void SupportsCustomTemperature_ForEachModel_ReturnsWhetherApiAcceptsCustomTemperature(
        AvailableGptModels model,
        bool expected)
    {
        model.SupportsCustomTemperature().Should().Be(expected);
    }
}
