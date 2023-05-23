namespace WebUI.Services;

public class AuthService
{
    private readonly RulesGptClient _rulesGptClient;

    public AuthService(RulesGptClient rulesGptClient)
    {
        _rulesGptClient = rulesGptClient;
    }

    public async Task<bool> CheckAuthAsync()
    {
        try
        {
            var response = await _rulesGptClient.CheckAuthAsync();
            return true;
        }
        catch (ApiException e)
        {
            var statusCode = e.StatusCode;
            return false;
        }
    }

    public async Task<bool> SignOutAsync()
    {
        try
        {
            //await _rulesGptClient.SignOutAsync();
            return true;
        }
        catch (ApiException e)
        {
            var statusCode = e.StatusCode;
            return false;
        }
    }
}
