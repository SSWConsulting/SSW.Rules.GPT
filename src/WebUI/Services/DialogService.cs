using MudBlazor;
using WebUI.Components;

namespace WebUI.Services;

public class DialogService
{
    private readonly IDialogService _dialogService;

    public DialogService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<bool> ApiKeyDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            NoHeader = false,
            MaxWidth = MaxWidth.ExtraSmall
        };
        var dialog = await _dialogService.ShowAsync<ApiKeyDialog>("API Key", options);
        var success = dialog.Result.IsCompletedSuccessfully;
        return success;
    }

    public async Task<bool> AboutRulesGptDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            NoHeader = false,
            MaxWidth = MaxWidth.Small,
            
        };
        var dialog = await _dialogService.ShowAsync<AboutAppDialog>(string.Empty, options);
        var success = dialog.Result.IsCompletedSuccessfully;
        return success;
    }

    public async Task<bool> InstallInstructionsDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            NoHeader = false,
            MaxWidth = MaxWidth.Large
        };
        var dialog = await _dialogService.ShowAsync<InstallInstructionDialog>(string.Empty, options);
        var success = dialog.Result.IsCompletedSuccessfully;
        return success;
    }
}
