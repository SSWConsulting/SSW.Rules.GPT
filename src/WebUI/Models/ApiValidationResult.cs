namespace WebUI.Classes;

public class ApiValidationResult
{
    public bool Success { get; }
    public string ErrorMessage { get; }

    public ApiValidationResult(bool success, string errorMessage)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }
}