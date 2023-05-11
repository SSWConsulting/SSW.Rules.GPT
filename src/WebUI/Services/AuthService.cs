namespace WebUI.Services;

public class AuthService
{
    private readonly StatusAppClient _statusAppClient;

    public AuthService(StatusAppClient statusAppClient)
    {
        _statusAppClient = statusAppClient;
    }

    public async Task<bool> CheckAuthAsync()
    {
        try
        {
            var response = await _statusAppClient.CheckAuthAsync();
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
            //await _statusAppClient.SignOutAsync();
            return true;
        }
        catch (ApiException e)
        {
            var statusCode = e.StatusCode;
            return false;
        }
    }
}
