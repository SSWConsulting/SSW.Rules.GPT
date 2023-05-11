using MudBlazor;
using WebUI.Components;

namespace WebUI.Services;

public class StatusAppDialogService
{
    private readonly IDialogService _dialogService;

    public StatusAppDialogService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<bool> LoginDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = false,
            CloseButton = false,
            DisableBackdropClick = true,
            NoHeader = true
        };
        var dialog = await _dialogService.ShowAsync<LoginDialog>(string.Empty, options);
        var result = await dialog.Result;
        var success = dialog.Result.IsCompletedSuccessfully;
        return success;
    }
}
