using MudBlazor;
using WebUI.Components;

namespace WebUI.Services;

public class SswRulesGptDialogService
{
    private readonly IDialogService _dialogService;

    public SswRulesGptDialogService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<bool> ApiKeyDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            DisableBackdropClick = false,
            NoHeader = false,
            MaxWidth = MaxWidth.ExtraSmall
        };
        var dialog = await _dialogService.ShowAsync<ApiKeyDialog>("API Key", options);
        var result = await dialog.Result;
        var success = dialog.Result.IsCompletedSuccessfully;
        return success;
    }

    public async Task<bool> AboutRulesGptDialog()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            CloseButton = true,
            DisableBackdropClick = false,
            NoHeader = false,
            MaxWidth = MaxWidth.Large
        };
        var dialog = await _dialogService.ShowAsync<AboutAppDialog>(string.Empty, options);
        var result = await dialog.Result;
        var success = dialog.Result.IsCompletedSuccessfully;
        return success;
    }
}
